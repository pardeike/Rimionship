using HarmonyLib;
using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Steam;
using static HarmonyLib.Code;

namespace Rimionship
{
	// replace New Game with Rimionship button in the main menu
	//
	[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
	class OptionListingUtility_DrawOptionListing_Patch
	{
		public static void Prefix(List<ListableOption> optList)
		{
			var label = "NewColony".Translate();
			var option = optList.FirstOrDefault(opt => opt.label == label);
			if (option == null) return;
			option.label = "Rimionship";
			option.action = () =>
			{
				MainMenuDrawer.CloseMainTab();
				GameDataSaveLoader.LoadGame(Path.Combine(RimionshipMod.rootDir, "Resources", "rimionship"));
			};
		}
	}

	// open colonist configuration page after map is loaded
	//
	[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
	class Game_FinalizeInit_Patch
	{
		public static void Postfix()
		{
			if (Find.TickManager.TicksGame == 0)
			{
				Find.GameEnder.gameEnding = false;
				Find.GameEnder.ticksToGameOver = -1;
				Stats.ResetAll();
				Find.WindowStack.Add(new ConfigurePawns());
			}
		}
	}

	// update stuff when ui scale changes
	//
	[HarmonyPatch(typeof(Prefs), nameof(Prefs.UIScale), MethodType.Setter)]
	class Prefs_UIScale_Setter_Patch
	{
		public static void Postfix() => Assets.UIScaleChanged();
	}

	// replace Auto-sort mods button in mod configuration dialog with Load-default-rimionship
	//
	[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
	class Page_ModsConfig_DoWindowContents_Patch
	{
		static void LoadRimionshipMods()
		{
			var modList = new ulong[] { 2009463077, 0, 818773962, 761421485, 867467808, 839005762, 2773821103, 2790250834 };

			foreach (var id in modList)
			{
				if (ModLister.AllInstalledMods.Any(mod => mod.GetPublishedFileId().m_PublishedFileId == id))
					continue;
				_ = SteamUGC.SubscribeItem(new PublishedFileId_t(id));
			}
			WorkshopItems.RebuildItemsList();

			var activeMods = modList
				.Select(id => ModLister.AllInstalledMods.FirstOrDefault(mod => mod.GetPublishedFileId().m_PublishedFileId == id))
				.OfType<ModMetaData>()
				.ToList();

			ModLister.AllInstalledMods.Do(mod => mod.Active = false);
			activeMods.Do(mod => mod.Active = true);

			ModsConfig.Save();
			ModsConfig.RecacheActiveMods();
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instruction)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ModsConfig.TrySortMods());
			var to = SymbolExtensions.GetMethodInfo(() => LoadRimionshipMods());
			var list = Transpilers.MethodReplacer(instruction, from, to).ToList();

			var ldstr = list.FirstOrDefault(code => code.operand is string s && s == "ResolveModOrder");
			ldstr.operand = "LoadRimionshipMods";

			return list.AsEnumerable();
		}
	}

	// add fully custom weighted traits to new pawns
	//
	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateTraits))]
	class PawnGenerator_GenerateTraits_Patch
	{
		static bool IsDisplayClassConstructor(CodeInstruction c) => c.opcode == Newobj.opcode && c.operand is ConstructorInfo constructor && constructor.DeclaringType.Name.StartsWith("<>c__DisplayClass");

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
		{
			var matcher = new CodeMatcher(instructions, gen)
				.MatchStartForward(
					new CodeMatch(IsDisplayClassConstructor),
					Stloc[name: "displayclass"]
				);

			var local_displayclass = matcher.NamedMatch("displayclass").operand as LocalBuilder;

			matcher = matcher.MatchEndForward(
					new CodeMatch(c => c.IsLdloc(), "weightSelector"),
					new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => GenCollection.RandomElementByWeight<TraitDef>(default, default))),
					Stfld[name: "newTraitDef"] // we will start inserting just before this code
				);

			var local_weightSelector = matcher.NamedMatch("weightSelector").operand as LocalBuilder;
			var field_newTraitDef = matcher.NamedMatch("newTraitDef").operand as FieldInfo;

			var local_TraitScore = gen.DeclareLocal(typeof(TraitScore));

			matcher = matcher
				.Insert(
					Ldloc[operand: local_weightSelector],
					CodeInstruction.CallClosure<Func<TraitDef, Func<TraitDef, float>, TraitScore>>((TraitDef _, Func<TraitDef, float> weightFunction) =>
					{
						var s = 20 * RimionshipMod.settings.scaleFactor;
						var a = s * RimionshipMod.settings.badTraitSuppression;
						var b = s * RimionshipMod.settings.goodTraitSuppression;
						return TraitTools.sortedTraits.RandomElementByWeight(ts =>
						{
							var x = ts.badScore;
							return weightFunction(ts.def) * Mathf.Min(1f / (a * x + 1f), 1f / (b * (1f - x) + 1f));
						});
					}),
					Dup,
					Stloc[operand: local_TraitScore],
					Ldfld[operand: AccessTools.DeclaredField(typeof(TraitScore), nameof(TraitScore.def))]
				)
				.MatchStartForward(
					Ldloc[operand: local_displayclass, name: "start"],
					Ldfld[operand: field_newTraitDef],
					new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => PawnGenerator.RandomTraitDegree(default)))
				);

			var labels = matcher.NamedMatch("start").labels.ToArray();

			return matcher
				.RemoveInstructions(3)
				.Insert(
					Ldloc[operand: local_TraitScore].WithLabels(labels),
					Ldfld[operand: AccessTools.DeclaredField(typeof(TraitScore), nameof(TraitScore.degree))]
				)
				.InstructionEnumeration();
		}
	}

	// add pawn-pawn actions to the floatmenu
	//
	[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders))]
	class FloatMenuMakerMap_AddHumanlikeOrders_Patch
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			var map = pawn.Map;
			if (map.ReadyForSacrification(out var spot, out var sacrification) == false) return;
			if (spot.CanSacrifice(pawn) == false) return;

			using List<Thing>.Enumerator enumerator = IntVec3.FromVector3(clickPos).GetThingList(map).GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is not Pawn clickedPawn) continue;
				if (spot.CanBeSacrificed(clickedPawn) == false) continue;

				opts.Add(new FloatMenuOption("SacrificeColonist".Translate(clickedPawn.LabelShortCap), () =>
				{
					sacrification.sacrificer = pawn;
					sacrification.sacrifice = clickedPawn;
					sacrification.Start();
				},
				MenuOptionPriority.Low));
			}
		}
	}

	// make wealthwatcher update more often
	//
	[HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.RecountIfNeeded))]
	class WealthWatcher_RecountIfNeeded_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.Manipulator(code => code.OperandIs(5000f), code => code.operand = GenDate.TicksPerHour / 2f);
		}
	}

	// get damage to reporter
	//
	[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
	class Thing_TakeDamage_Patch
	{
		static void PostApplyDamage(Thing thing, DamageInfo dinfo, float amount)
		{
			thing.PostApplyDamage(dinfo, amount);

			if (dinfo.instigatorInt?.factionInt == Faction.OfPlayer)
			{
				var reporter = Current.Game.World.GetComponent<Reporter>();
				reporter.HandleDamageDealt(amount);
			}
			else if (thing?.factionInt == Faction.OfPlayer)
			{
				var reporter = Current.Game.World.GetComponent<Reporter>();
				reporter.HandleDamageTaken(thing, amount);
			}
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ((Thing)null).PostApplyDamage(default, default));
			var to = SymbolExtensions.GetMethodInfo(() => PostApplyDamage(default, default, default));
			return instructions.MethodReplacer(from, to);
		}
	}

	// add incidents to queue (1 day delayed) instead of running them directly
	//
	[HarmonyPatch(typeof(Storyteller), nameof(Storyteller.StorytellerTick))]
	class Storyteller_StorytellerTick_Patch
	{
		public static bool TryFire(Storyteller _, FiringIncident fi)
		{
			if (fi.def.Worker.CanFireNow(fi.parms))
			{
				var qi = new QueuedIncident(new FiringIncident(fi.def, fi.source, fi.parms), Find.TickManager.TicksGame + GenDate.TicksPerDay, 0);
				var _unused = Find.Storyteller.incidentQueue.Add(qi);
				return true;
			}
			return false;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ((Storyteller)null).TryFire(default));
			var to = SymbolExtensions.GetMethodInfo(() => TryFire(default, default));
			return instructions.MethodReplacer(from, to);
		}
	}
	//
	[HarmonyPatch(typeof(IncidentQueue), nameof(IncidentQueue.IncidentQueueTick))]
	class IncidentQueue_IncidentQueueTick_Patch
	{
		public static bool TryFire(Storyteller _, FiringIncident fi)
		{
			if (fi?.def?.Worker?.TryExecute(fi.parms) ?? false)
			{
				fi.parms?.target?.StoryState?.Notify_IncidentFired(fi);
				return true;
			}
			return false;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ((Storyteller)null).TryFire(default));
			var to = SymbolExtensions.GetMethodInfo(() => TryFire(default, default));
			return instructions.MethodReplacer(from, to);
		}
	}

	// draw blood god scale
	//
	[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls))]
	class GlobalControlsUtility_DoTimespeedControls_Patch
	{
		public static void Postfix(float leftX, ref float curBaseY)
		{
			BloodGod.Draw(leftX, ref curBaseY);
		}
	}
}
