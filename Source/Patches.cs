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
using Verse.Sound;
using Verse.Steam;
using static HarmonyLib.Code;

namespace Rimionship
{
	// start our async safe logger
	//
	[HarmonyPatch(typeof(Current), nameof(Current.Notify_LoadedSceneChanged))]
	static class Current_Notify_LoadedSceneChanged_Patch
	{
		public static void Postfix()
		{
			if (GenScene.InEntryScene)
				_ = Current.Root_Entry.StartCoroutine(AsyncLogger.LogCoroutine());

			if (GenScene.InPlayScene)
				_ = Current.Root_Play.StartCoroutine(AsyncLogger.LogCoroutine());
		}
	}

	// show server messages
	//
	[HarmonyPatch(typeof(Root), nameof(Root.OnGUI))]
	class Root_OnGUI_Patch
	{
		public static bool inited = false;
		static GUIStyle style;

		public static void Postfix(Root __instance)
		{
			if (inited == false || __instance.destroyed) return;
			try
			{
				if (PlayState.serverMessage.NullOrEmpty() == false)
				{
					var x = (UI.screenWidth - Assets.Note.width) / 2f;
					var y = UI.screenHeight * 240f / 1080f;
					var r = new Rect(x, y, Assets.Note.width, Assets.Note.height);
					GUI.DrawTextureWithTexCoords(r, Assets.Note, Tools.Rect01);
					r = r.ExpandedBy(-16, -16);
					r.yMin += 38;
					style ??= Assets.menuFontLarge.GUIStyle(Color.white).Alignment(TextAnchor.UpperLeft).Wrapping();
					GUI.Label(r, PlayState.serverMessage, style);
				}
			}
			finally
			{
			}
		}
	}

	// show main menu info
	//
	[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
	class MainMenuDrawer_DoMainMenuControls_Patch
	{
		public static void Postfix(Rect rect)
		{
			if (Current.ProgramState == ProgramState.Entry)
			{
				Root_OnGUI_Patch.inited = true;
				MainMenu.OnGUI(rect.x - 7f, rect.y + 45f + 7f + 45f / 2f);
			}
		}
	}

	// replace New Game with Rimionship button in the main menu
	//
	[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
	class OptionListingUtility_DrawOptionListing_Patch
	{
		static readonly string newColonyLabel = "NewColony".Translate();

		public static void Prefix(List<ListableOption> optList)
		{
			var option = optList.FirstOrDefault(opt => opt.label == newColonyLabel);
			if (option == null) return;
			option.label = "Rimionship";
			option.action = () =>
			{
				if (PlayState.Valid == false)
				{
					Defs.Nope.PlayOneShotOnCamera();
					Find.WindowStack.Add(new Dialog_MessageBox("CannotStartTournament".Translate()));
					return;
				}
				MainMenuDrawer.CloseMainTab();
				GameDataSaveLoader.LoadGame(Path.Combine(RimionshipMod.rootDir, "Resources", "rimionship"));
			};
		}
	}

	// make all rimionship buttons stand out
	//
	[HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextWorker))]
	class Widgets_ButtonTextWorker_Patch
	{
		static readonly MethodInfo drawAtlasMethod = SymbolExtensions.GetMethodInfo(() => Widgets.DrawAtlas(default, default));

		static void DrawAtlas(Rect rect, Texture2D atlas, string label)
		{
			var flag = label.Contains("Rimionship");
			if (flag)
			{
				atlas = Assets.ButtonBGAtlas;
				if (Mouse.IsOver(rect))
					atlas = Input.GetMouseButton(0) ? Assets.ButtonBGAtlasClick : Assets.ButtonBGAtlasOver;
			}
			Widgets.DrawAtlas(rect, atlas);
			if (flag)
			{
				var r = rect.ExpandedBy(-1);
				var s = rect.size;
				var f = r.size.x / r.size.y;
				if (s.x / s.y < f)
					s.x = s.y * f;
				Widgets.DrawTextureFitted(r, Assets.ButtonPattern, 1f, s, new Rect(0f, 0f, 1f, 1f));
			}
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.MatchStartForward(new CodeMatch(operand: drawAtlasMethod))
				.RemoveInstruction()
				.Insert(
					Ldarg_1,
					Call[SymbolExtensions.GetMethodInfo(() => DrawAtlas(default, default, default))]
				)
				.InstructionEnumeration();
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
	[HarmonyPatch(typeof(Window), nameof(Window.PostOpen))]
	class Page_ModsConfig_PostOpen_Patch
	{
		public static void Postfix(Window __instance)
		{
			if (__instance is Page_ModsConfig page)
				Page_ModsConfig_DoWindowContents_Patch.PostOpen(page);
		}
	}
	//
	[HarmonyPatch(typeof(Window), nameof(Window.PreClose))]
	class Page_ModsConfig_PreClose_Patch
	{
		public static void Prefix(Window __instance)
		{
			if (__instance is Page_ModsConfig)
				Page_ModsConfig_DoWindowContents_Patch.PreClose();
		}
	}
	//
	[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
	class Page_ModsConfig_DoWindowContents_Patch
	{
		static Page_ModsConfig currentPage = null;
		static Callback<RemoteStoragePublishedFileSubscribed_t> subscribedCallback;

		internal static void PostOpen(Page_ModsConfig page)
		{
			currentPage = page;
			subscribedCallback = Callback<RemoteStoragePublishedFileSubscribed_t>.Create(new Callback<RemoteStoragePublishedFileSubscribed_t>.DispatchDelegate(OnItemSubscribed));
		}

		internal static void PreClose()
		{
			currentPage = null;
			subscribedCallback.Unregister();
			subscribedCallback = null;
		}

		static void OnItemSubscribed(RemoteStoragePublishedFileSubscribed_t result)
		{
			if (currentPage == null) return;
			if (PlayState.AllowedMods.Any(pair => pair.Value == result.m_nPublishedFileId.m_PublishedFileId) == false) return;

			LongEventHandler.ExecuteWhenFinished(() =>
			{
				// TODO: this repeating code seems necessary. Investigate more

				PlayState.AllowedMods
					.Select(mod => mod.Key)
					.Except(Tools.InstalledMods())
					.Do(missingPackageId =>
					{
						var mod = PlayState.AllowedMods.FirstOrDefault(mod => mod.Key == missingPackageId);
						var _ = SteamUGC.SubscribeItem(new PublishedFileId_t(mod.Value));
					});
				WorkshopItems.RebuildItemsList();

				ModsConfig.data.activeMods = PlayState.AllowedMods.Select(mod => mod.Key).ToList();
				ModsConfig.RecacheActiveMods();
				ModLister.RebuildModList();
				ModsConfig.Save();
				ModsConfig.activeModsInLoadOrderCachedDirty = true;
				Page_ModsConfig.modWarningsCached = ModsConfig.GetModWarnings();
			});
		}

		static string ButtonLabel()
		{
			return SteamManager.Initialized ? "LoadRimionshipMods" : "ResolveModOrder";
		}

		static void LoadRimionshipMods(Page_ModsConfig _)
		{
			if (SteamManager.Initialized == false)
			{
				ModsConfig.TrySortMods();
				return;
			}

			if (PlayState.AllowedMods.NullOrEmpty())
			{
				Defs.Nope.PlayOneShotOnCamera();
				Find.WindowStack.Add(new Dialog_MessageBox("AllowedModsNotAvailable".Translate()));
				return;
			}

			PlayState.AllowedMods
				.Select(mod => mod.Key)
				.Except(Tools.InstalledMods())
				.Do(missingPackageId =>
				{
					var mod = PlayState.AllowedMods.FirstOrDefault(mod => mod.Key == missingPackageId);
					var _ = SteamUGC.SubscribeItem(new PublishedFileId_t(mod.Value));
				});
			WorkshopItems.RebuildItemsList();

			ModsConfig.data.activeMods = PlayState.AllowedMods.Select(mod => mod.Key).ToList();
			ModsConfig.RecacheActiveMods();
			ModLister.RebuildModList();
			ModsConfig.Save();
			ModsConfig.activeModsInLoadOrderCachedDirty = true;
			Page_ModsConfig.modWarningsCached = ModsConfig.GetModWarnings();
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.MatchStartForward(
					new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => ModsConfig.TrySortMods()))
				)
				.InsertAndAdvance(Ldarg_0)
				.SetOperandAndAdvance(SymbolExtensions.GetMethodInfo(() => LoadRimionshipMods(default)))
				.Start()
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldstr, "ResolveModOrder")
				)
				.Set(Call.opcode, SymbolExtensions.GetMethodInfo(() => ButtonLabel()))
				.InstructionEnumeration();
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
