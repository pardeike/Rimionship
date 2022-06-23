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
		static void Prefix(List<ListableOption> optList)
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
		static void Postfix()
		{
			if (Find.TickManager.TicksGame > 5000) return;
			Find.WindowStack.Add(new ConfigurePawns());
		}
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

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instruction)
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

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
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

	// add a gizmo for our sacrifies spot
	//
	[HarmonyPatch(typeof(GizmoGridDrawer))]
	[HarmonyPatch(nameof(GizmoGridDrawer.DrawGizmoGrid))]
	static class GizmoGridDrawer_DrawGizmoGrid_Patch
	{
		static Command_Action CreateDeleteResurrectionPortal(SacrifiesSpot spot)
		{
			var h = (spot.created + GenDate.TicksPerDay - Find.TickManager.TicksGame + GenDate.TicksPerHour - 1) / GenDate.TicksPerHour;
			var hours = $"{h} Stunde" + (h != 1 ? "n" : "");
			return new Command_Action
			{
				defaultLabel = "Entfernen",
				icon = ContentFinder<Texture2D>.Get("RemoveSacrifiesSpot", true),
				disabled = h > 0,
				disabledReason = "Du musst noch " + hours + " warten bis du den Spot entfernen kannst",
				defaultDesc = "Entfernt den Blutgottspot damit er woanders wieder aufgebaut werden kann",
				order = -20f,
				action = () => spot.Destroy()
			};
		}

		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref IEnumerable<Gizmo> gizmos)
		{
			if (Find.Selector.SelectedObjects.FirstOrDefault() is not SacrifiesSpot spot) return;
			gizmos = new List<Gizmo>() { CreateDeleteResurrectionPortal(spot) }.AsEnumerable();
		}
	}
}
