using HarmonyLib;
using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
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
			AsyncLogger.StartCoroutine();
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
			if (inited == false || __instance.destroyed)
				return;
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
		static readonly string modsLabel = "Mods".Translate();

		public static void Prefix(List<ListableOption> optList)
		{
			var option = optList.FirstOrDefault(opt => opt.label == modsLabel);
			if (option != null)
			{
				var n1 = ModsConfig.data.activeMods.Count;
				var n2 = PlayState.AllowedMods.Count;
				if (n1 != n2)
					option.label += $" ({n1} {"OfLower".Translate()} {n2})";
			}

			option = optList.FirstOrDefault(opt => opt.label == newColonyLabel);
			if (option != null)
			{
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
					_ = Task.Run(PlayState.LoadGame);
				};
			}
		}
	}

	// avoid incidents too early
	//
	[HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
	static class IncidentWorker_CanFireNow_Patch
	{
		public static bool Prefix(ref bool __result)
		{
			if (Find.TickManager.TicksGame < GenDate.TicksPerHour)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	// catch mouse events when necessary
	//
	[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.HandleEventsHighPriority))]
	static class WindowStack_HandleEventsHighPriority_Patch
	{
		public static void Prefix()
		{
			var type = Event.current.type;
			if (Assets.catchMouseEvents && (type == EventType.MouseDown || type == EventType.MouseUp))
				Event.current.Use();
		}
	}

	// turn dev mode off
	//
	[HarmonyPatch(typeof(Prefs), nameof(Prefs.DevMode), MethodType.Getter)]
	static class Prefs_DevMode_Patch
	{
		public static bool Prepare() => Tools.DevMode == false;

		public static bool Prefix(ref bool __result)
		{
			__result = false;
			return false;
		}
	}

	// turn off learning helper
	//
	[HarmonyPatch(typeof(LearningReadout), nameof(LearningReadout.LearningReadoutOnGUI))]
	static class LearningReadout_LearningReadoutOnGUI_Patch
	{
		public static bool Prefix()
		{
			return false;
		}
	}

	// turn off in-game storyteller changing
	//
	[HarmonyPatch(typeof(TutorSystem), nameof(TutorSystem.AllowAction))]
	static class TutorSystem_AllowAction_Patch
	{
		public static bool Prepare() => Tools.DevMode == false;

		public static bool Prefix(EventPack ep, ref bool __result)
		{
			if (ep.Tag != "ChooseStoryteller")
				return true;
			__result = false;
			return false;
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
				Find.WindowStack.Add(new Page_ConfigurePawns());
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
		static int lastDownloadingItemsCount = 0;

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
				.ToList()
				.Do(missingPackageId =>
				{
					lastDownloadingItemsCount = -1;

					var mod = PlayState.AllowedMods.FirstOrDefault(mod => mod.Key == missingPackageId);
					var callResult = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create((pCallback, bIOFailure) =>
					{
						if (pCallback.m_eResult != EResult.k_EResultOK || bIOFailure)
							Log.Error($"Error downloading mod {missingPackageId}: {pCallback.m_eResult}");
					});
					var handle = SteamRemoteStorage.SubscribePublishedFile(new PublishedFileId_t(mod.Value));
					callResult.Set(handle);
				});
		}

		[HarmonyPrefix]
		public static void Downloader(Page_ModsConfig __instance)
		{
			var count = WorkshopItems.DownloadingItemsCount;
			if (count > 0)
			{
				var winRect = new Rect((UI.screenWidth - 320f) / 2f, (UI.screenHeight - 180f) / 2f, 320f, 180f);
				Find.WindowStack.ImmediateWindow(6673095, winRect, WindowLayer.Super, delegate
				{
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.WordWrap = true;
					var r = new Rect(10, 10, 300, 80);
					Widgets.Label(r, "ModLoading".Translate(new NamedArgument(count, "count")));
					Text.Anchor = TextAnchor.UpperLeft;
					r = new Rect(100, 120, 120, 40);
					if (Widgets.ButtonText(r, "DesignatorCancel".Translate(), true, true, true))
					{
						lastDownloadingItemsCount = count;
						__instance.Close();
					}
				}, true, true, 1f, null);

				__instance.selectedMod = null;
			}

			if (lastDownloadingItemsCount != count)
			{
				lastDownloadingItemsCount = count;

				if (count == 0)
				{
					ModsConfig.data.activeMods = PlayState.AllowedMods.Select(mod => mod.Key).ToList();
					ModsConfig.RecacheActiveMods();
				}

				ModLister.RebuildModList();
				__instance.modsInListOrderDirty = true;
				__instance.selectedMod = __instance.ModsInListOrder().FirstOrDefault<ModMetaData>();

				if (count == 0)
				{
					ModsConfig.activeModsInLoadOrderCachedDirty = true;
					Page_ModsConfig.modWarningsCached = ModsConfig.GetModWarnings();
				}
			}
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.MatchStartForward(new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => ModsConfig.TrySortMods())))
				.InsertAndAdvance(Ldarg_0)
				.SetOperandAndAdvance(SymbolExtensions.GetMethodInfo(() => LoadRimionshipMods(default)))
				.Start()
				.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "ResolveModOrder"))
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

	// reduce skill/passion for melee and shooting
	//
	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateSkills))]
	class PawnGenerator_GenerateSkills_Patch
	{
		public static void Postfix(Pawn pawn)
		{
			static void Limit(SkillRecord record, int maxLevel, Passion maxPassion)
			{
				if (record.Level > maxLevel)
					record.Level = maxLevel;
				if (record.passion > maxPassion)
					record.passion = maxPassion;
			}

			Limit(pawn.skills.GetSkill(SkillDefOf.Melee), RimionshipMod.settings.maxMeleeSkill, (Passion)RimionshipMod.settings.maxMeleeFlames);
			Limit(pawn.skills.GetSkill(SkillDefOf.Shooting), RimionshipMod.settings.maxShootingSkill, (Passion)RimionshipMod.settings.maxShootingFlames);
		}
	}

	// allow custom thoughts
	//
	[HarmonyPatch(typeof(ThoughtUtility), nameof(ThoughtUtility.CanGetThought))]
	class ThoughtUtility_CanGetThought_Patch
	{
		public static bool Prefix(ref bool __result)
		{
			if (Tools.AllowOverride)
			{
				__result = true;
				return false;
			}
			return true;
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
			if (BloodGod.Instance.IsInactive)
				return;
			if (map.ReadyForSacrification(out var spot, out var sacrification) == false)
				return;
			if (spot.CanSacrifice(pawn) == false)
				return;

			using List<Thing>.Enumerator enumerator = IntVec3.FromVector3(clickPos).GetThingList(map).GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is not Pawn clickedPawn)
					continue;
				if (spot.CanBeSacrificed(clickedPawn) == false)
					continue;

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

	// fake prio our sacrification jobs
	//
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryFindAndStartJob))]
	class Pawn_JobTracker_TryFindAndStartJob_Patch
	{
		public static bool Prefix(Pawn_JobTracker __instance, Pawn ___pawn)
		{
			if (___pawn.CanParticipateInSacrificationRitual() == false)
				return true;

			if (__instance.curJob != null)
				__instance.EndCurrentJob(JobCondition.InterruptForced, false);

			var sacrification = ___pawn.Map.GetComponent<Sacrification>();
			if (sacrification.IsNotRunning())
				return true;

			var spot = SacrificationSpot.ForMap(___pawn.Map);
			if (spot == null)
				return true;

			if (sacrification.sacrificer == ___pawn)
			{
				___pawn.drafter.Drafted = false;
				__instance.StartJob(JobMaker.MakeJob(Defs.SacrificeColonist, sacrification.sacrifice, spot));
				return false;
			}

			if (sacrification.sacrifice == ___pawn)
			{
				___pawn.drafter.Drafted = false;
				__instance.StartJob(JobMaker.MakeJob(Defs.GettingSacrificed, spot, sacrification.sacrificer));
				return false;
			}

			var job = JobMaker.MakeJob(Defs.WatchSacrification, sacrification.sacrifice, spot, sacrification.sacrificer);
			___pawn.drafter.Drafted = false;
			__instance.StartJob(job);
			return false;
		}
	}
	//
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
	class Pawn_JobTracker_StartJob_Patch
	{
		public static void Postfix(Pawn ___pawn)
		{
			if (___pawn?.MentalStateDef == null || ___pawn.RaceProps.Humanlike == false)
				return;

			var sacrification = ___pawn.Map.GetComponent<Sacrification>();
			if (sacrification.IsRunning() == false)
				return;

			if (___pawn == sacrification.sacrifice || ___pawn == sacrification.sacrificer)
				sacrification.MarkFailed();
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
				Reporter.Instance.HandleDamageDealt(amount);
			else if (thing?.factionInt == Faction.OfPlayer)
				Reporter.Instance.HandleDamageTaken(thing, amount);
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
		public static bool TryFire(Storyteller __instance, FiringIncident fi)
		{
			if (fi.def.Worker.CanFireNow(fi.parms))
			{
				var qi = new QueuedIncident(new FiringIncident(fi.def, fi.source, fi.parms), Find.TickManager.TicksGame + GenDate.TicksPerDay, 0);
				_ = __instance.incidentQueue.Add(qi);
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

	// support callbacks for sounds played with Tools.PlayWithCallback
	//
	[HarmonyPatch(typeof(SampleOneShot), nameof(SampleOneShot.TryMakeAndPlay))]
	class SampleOneShot_TryMakeAndPlay_Patch
	{
		public static void Postfix(SubSoundDef def, AudioClip clip, SampleOneShot __result)
		{
			if (__result != null && Tools.PlayCallbacks.TryRemove(def.parentDef, out var delayedAction))
			{
				var msWait = (int)((clip.length + delayedAction.delay) * 1000);
				new Task(async () =>
				{
					await Task.Delay(msWait);
					delayedAction.action();
				})
				.Start();
			}
		}
	}

	// turn sacrification spot on/off to only show it on its own map
	//
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate))]
	class MapInterface_MapInterfaceUpdate_Patch
	{
		public static void Prefix()
		{
			var currentMap = Find.CurrentMap;
			foreach (var map in Current.Game.Maps)
				SacrificationSpot.ForMap(map)?.SetVisible(map == currentMap);
		}
	}

	// draw blood god scale
	//
	[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls))]
	class GlobalControlsUtility_DoTimespeedControls_Patch
	{
		public static void Postfix(float leftX, ref float curBaseY)
		{
			BloodGod.Instance.Draw(leftX, ref curBaseY);
		}
	}

	// show extra UX on architect popup
	//
	[HarmonyPatch]
	class ArchitectCategoryTab_Patch
	{
		public static MethodInfo TargetMethod()
		{
			var cls = AccessTools.FirstInner(typeof(ArchitectCategoryTab), type => AccessTools.Field(type, "designator") != null);
			if (cls == null)
				return null;
			return AccessTools.GetDeclaredMethods(cls).FirstOrDefault();
		}

		public static void Postfix(Designator ___designator, Rect ___infoRect)
		{
			if (___designator != null)
				return;
			var rect = ___infoRect.AtZero().ContractedBy(7f);

			var list = new Listing_Standard();
			list.Begin(rect);

			Text.Font = GameFont.Medium;
			Widgets.DrawTextureFitted(list.GetRect(27), Assets.Rimionship, 1f);
			list.Gap(12f);

			var activateLabel = "Activate".Translate();
			if (Find.CurrentMap == Reporter.Instance.ChosenMap)
			{
				Text.Font = GameFont.Small;
				_ = list.Label("MainMapIsActive".Translate());
			}
			else
			{
				Text.Font = GameFont.Small;
				_ = list.Label("MainMapIsInactive".Translate(activateLabel));
				list.Gap(7f);

				if (list.ButtonText(activateLabel))
				{
					Reporter.Instance.ChosenMap = Find.CurrentMap;
					Reporter.Instance.RefreshWealth();
				}
			}

			list.End();
		}
	}

	// count animal meat produced
	//
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
	class Pawn_ButcherProducts_Patch
	{
		public static void Postfix(IEnumerable<Thing> __result)
		{
			var count = __result.Where(thing => thing.HasThingCategory(ThingCategoryDefOf.MeatRaw)).Sum(thing => thing.stackCount);
			Reporter.Instance.NewAnimalMeat(count);
		}
	}

	// count blood cleaned
	//
	[HarmonyPatch]
	class ThinFilth_Patch
	{
		public static MethodInfo TargetMethod()
		{
			static MethodInfo OurMethod(Type type) => type == null ? null : AccessTools.GetDeclaredMethods(type)
				.FirstOrDefault(method => method.Name.Contains("MakeNewToils") && method.Name.Contains("__1"));
			var type = AccessTools.FirstInner(typeof(JobDriver_CleanFilth), type => OurMethod(type) != null);
			return OurMethod(type);
		}

		static void ThinFilth(Filth instance)
		{
			if (instance.def == ThingDefOf.Filth_Blood || instance.def == ThingDefOf.Filth_DriedBlood)
				Reporter.Instance.NewBloodCleaned();
			instance.ThinFilth();
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ((Filth)null).ThinFilth());
			var to = SymbolExtensions.GetMethodInfo(() => ThinFilth(default));
			return instructions.MethodReplacer(from, to);
		}
	}

	// attention: colonist killed
	//
	[HarmonyPatch(typeof(StatsRecord), nameof(StatsRecord.Notify_ColonistKilled))]
	class StatsRecord_Notify_ColonistKilled_Patch
	{
		public static void Postfix()
		{
			ServerAPI.TriggerAttention(Constants.Attention.colonistKilled);
		}
	}

	// attention: incidents
	//
	[HarmonyPatch(typeof(StoryState), nameof(StoryState.Notify_IncidentFired))]
	class StoryState_Notify_IncidentFired_Patch
	{
		static readonly Dictionary<IncidentCategoryDef, int> scores = new()
		{
			{ IncidentCategoryDefOf.Misc, Constants.Attention.incidentMisc },
			{ IncidentCategoryDefOf.ThreatSmall, Constants.Attention.threatSmall },
			{ IncidentCategoryDefOf.ThreatBig, Constants.Attention.threatBig },
			{ IncidentCategoryDefOf.FactionArrival, Constants.Attention.factionArrival },
			{ IncidentCategoryDefOf.OrbitalVisitor, Constants.Attention.orbitalVisitor },
			{ IncidentCategoryDefOf.ShipChunkDrop, Constants.Attention.shipChunkDrop },
			{ IncidentCategoryDefOf.DiseaseHuman, Constants.Attention.diseaseHuman },
			{ IncidentCategoryDefOf.DiseaseAnimal, Constants.Attention.diseaseAnimal },
			{ IncidentCategoryDefOf.AllyAssistance, Constants.Attention.allyAssistance },
			{ IncidentCategoryDefOf.EndGame_ShipEscape, Constants.Attention.endGameShipEscape },
			{ IncidentCategoryDefOf.GiveQuest, Constants.Attention.giveQuest },
			{ IncidentCategoryDefOf.DeepDrillInfestation, Constants.Attention.deepDrillInfestation },
		};

		public static void Postfix(FiringIncident fi)
		{
			if (scores.TryGetValue(fi.def.category, out var score))
				ServerAPI.TriggerAttention(score);
		}
	}

	// attention: combat
	//
	[HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.Notify_AttackedTarget))]
	class Pawn_MindState_Notify_AttackedTarget_Patch
	{
		public static void Postfix(Pawn ___pawn, LocalTargetInfo target)
		{
			if (___pawn.IsColonist == false)
				return;
			if (target.Thing is Pawn pawn && pawn.RaceProps.Humanlike)
				ServerAPI.TriggerAttention(Constants.Attention.attackedHuman);
			else
				ServerAPI.TriggerAttention(Constants.Attention.attackedNonHuman);
		}
	}

	// attention: general damage
	//
	[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.PreApplyDamage))]
	class ThingWithComps_PreApplyDamage_Patch
	{
		public static void Postfix(ThingWithComps __instance, ref DamageInfo dinfo)
		{
			if (dinfo.Instigator is Pawn)
			{
				var score = __instance.Faction.IsPlayer
					? Constants.Attention.damagePlayerThing
					: Constants.Attention.damageNonPlayerThing;
				ServerAPI.TriggerAttention(score);
			}
		}
	}
}
