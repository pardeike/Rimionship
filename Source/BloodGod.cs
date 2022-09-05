using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimionship
{
	public class BloodGod : WorldComponent
	{
		public static BloodGod Instance => Current.Game.World.GetComponent<BloodGod>();

		static readonly Color scaleBG = new(1, 1, 1, 0.35f);
		static readonly Color scaleFG = new(186f / 255f, 0f, 0f);
		static readonly Color scaleCD = new(1f / 255f, 184f / 255f, 1f);
		static readonly SoundInfo onCameraPerTick = SoundInfo.OnCamera(MaintenanceType.PerTick);

		public enum State
		{
			Idle,
			Rising,
			Announcing,
			Punishing,
			Pausing,
			Cooldown
		}

		public State state;
		public int startTicks;
		public int pauseTicks;
		public int pauseLength;
		public int cooldownTicks;
		public int punishLevel;
		public bool helpShown;
		public bool hadLevel3;
		public long primeTimerStart;
		public long primeTimerTotal;

		Sustainer ambience;

		public BloodGod(World world) : base(world)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref state, "state");
			Scribe_Values.Look(ref startTicks, "startTicks");
			Scribe_Values.Look(ref pauseTicks, "pauseTicks");
			Scribe_Values.Look(ref pauseLength, "pauseLength");
			Scribe_Values.Look(ref cooldownTicks, "cooldownTicks");
			Scribe_Values.Look(ref punishLevel, "punishLevel");
			Scribe_Values.Look(ref helpShown, "helpShown");
			Scribe_Values.Look(ref hadLevel3, "hadLevel3");
			Scribe_Values.Look(ref primeTimerStart, "primeTimerStart");
			Scribe_Values.Look(ref primeTimerTotal, "primeTimerTotal");
		}

		public bool IsInactive => state == State.Idle || state == State.Cooldown;
		public bool IsPunishing => state == State.Punishing || state == State.Pausing;

		public float RisingClamped01()
		{
			var currentTicks = Find.TickManager.TicksGame;
			if (state < State.Rising)
				return 0f;
			if (state > State.Rising)
				return 1f;

			var n = Math.Max(0, Stats.AllColonists() - RimionshipMod.settings.maxFreeColonistCount);
			var interval = Mathf.Max(
				RimionshipMod.settings.risingIntervalMinimum,
				RimionshipMod.settings.risingInterval - RimionshipMod.settings.risingReductionPerColonist * n
			);

			return Mathf.Clamp01((currentTicks - startTicks) / (float)RimionshipMod.settings.risingInterval);
		}

		void AnnounceNextLevel()
		{
			// TODO make system to play specific list of sounds randomized per event type
			Defs.Bloodgod.PlayWithCallback(0f, () => StartPhase(State.Punishing));
			punishLevel = Math.Min(punishLevel + 1, 5);
			hadLevel3 |= punishLevel == 3;
			AsyncLogger.Warning($"BLOOD GOD Level now #{punishLevel}");
			StartPhase(State.Announcing);
		}

		void UpdatePrimeTime(int currentTicks)
		{
			var safetyMarginTicks = 4 * 60 * 60; // 4 game minutes
			var primeIntervalMinutes = TimeSpan.TicksPerMinute * 30; // 30 real world minutes
			var dateTicks = DateTime.Now.Ticks;

			if (hadLevel3
				&& (state != State.Rising || currentTicks - startTicks < RimionshipMod.settings.risingInterval - safetyMarginTicks)
				&& (state != State.Pausing || currentTicks.Between(pauseTicks - pauseLength + safetyMarginTicks, pauseTicks - safetyMarginTicks))
				&& state != State.Announcing && state != State.Punishing)
			{
				if (primeTimerStart == 0)
					primeTimerStart = dateTicks;
			}
			else
			{
				if (primeTimerStart > 0)
					primeTimerTotal += dateTicks - primeTimerStart;
				primeTimerStart = 0;
			}

			var pt = primeTimerTotal + (primeTimerStart == 0 ? 0 : dateTicks - primeTimerStart);
			//Log.Warning($"# {pt * 1f / TimeSpan.TicksPerMinute:F2}");
			if (pt >= primeIntervalMinutes)
			{
				primeTimerTotal = 0;
				if (primeTimerStart > 0)
					primeTimerStart = dateTicks;
				Defs.Prime.PlayOneShotOnCamera();
			}
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			TickAmbience();
			if (60.EveryNTick() == false)
				return;

			var currentTicks = Find.TickManager.TicksGame;
			UpdatePrimeTime(currentTicks);

			var allColonists = Stats.AllColonists();
			if (allColonists <= RimionshipMod.settings.maxFreeColonistCount)
				StartPhase(State.Idle);

			switch (state)
			{
				case State.Idle:
					if (allColonists > RimionshipMod.settings.maxFreeColonistCount)
					{
						punishLevel = 0;
						StartPhase(State.Rising);
					}
					break;

				case State.Rising:
					if (currentTicks - startTicks > RimionshipMod.settings.risingInterval)
						AnnounceNextLevel();
					break;

				case State.Announcing:
					break;

				case State.Punishing:
					void Action()
					{
						if (CommencePunishment())
						{
							Find.LetterStack.ReceiveLetter("PunishmentLetterTitle".Translate(), "PunishmentLetterContent".Translate(punishLevel), LetterDefOf.NegativeEvent, null);
							Defs.Thunder.PlayOneShotOnCamera();
							var minInterval = RimionshipMod.settings.startPauseInterval;
							var maxInterval = RimionshipMod.settings.finalPauseInterval;
							pauseLength = (int)GenMath.LerpDoubleClamped(1, 5, minInterval, maxInterval, punishLevel);
							pauseTicks = currentTicks + pauseLength;
							StartPhase(State.Pausing);
						}
					}
					if (helpShown == false)
					{
						helpShown = true;
						Find.TickManager.Pause();
						Find.WindowStack.Add(new Dialog_Information("BloodGodInfoTitle", "BloodGodInfoBody", "OK", Action));
					}
					else
						Action();
					break;

				case State.Pausing:
					if (currentTicks > pauseTicks)
						AnnounceNextLevel();
					break;

				case State.Cooldown:
					if (currentTicks > cooldownTicks)
					{
						punishLevel = 0;
						StartPhase(State.Rising);
					}
					break;
			}
		}

		void TickAmbience()
		{
			if (state != State.Idle)
			{
				if (ambience == null || ambience.Ended)
					ambience = Defs.Ambience.TrySpawnSustainer(onCameraPerTick);
				ambience.externalParams["LerpFactor"] = RisingClamped01();
				ambience.Maintain();
			}
			else
			{
				if (ambience != null)
				{
					ambience.End();
					ambience.Cleanup();
					ambience = null;
				}
			}
		}

		public void Satisfy(SacrificationSpot spot, Sacrification sacrification)
		{
			var factor = GenMath.LerpDouble(1, 5, RimionshipMod.settings.minThoughtFactor, RimionshipMod.settings.maxThoughtFactor, punishLevel);
			AsyncLogger.Warning($"BLOOD GOD #{punishLevel} SATISFIED (factor {factor})");

			sacrification.sacrificer.GiveThought(sacrification.sacrificer, ThoughtDefOf.EncouragingSpeech, factor);
			sacrification.sacrificer.GiveThought(sacrification.sacrificer, ThoughtDefOf.KilledHumanlikeBloodlust, factor);

			spot.Map.mapPawns.FreeColonistsAndPrisoners.HasLineOfSightTo(spot.Position, spot.Map)
				.Do(pawn => pawn.GiveThought(sacrification.sacrificer, ThoughtDefOf.WitnessedDeathBloodlust, factor));

			cooldownTicks = Find.TickManager.TicksGame + RimionshipMod.settings.risingCooldown;
			StartPhase(State.Cooldown);
		}

		void StartPhase(State state, bool setStartTicks = true)
		{
			if (setStartTicks)
				startTicks = Find.TickManager.TicksGame;
			this.state = state;
			AsyncLogger.Warning($"BLOOD GOD #{punishLevel} phase => {state}");
		}

		static bool RepeatWIthPercentageOfColonists(float percentage, Func<bool> action)
		{
			var count = Mathf.FloorToInt(Stats.AllColonists() * percentage) + 1;
			var flag = false;
			for (var i = 1; i <= count; i++)
				if (action())
					flag = true;
				else
					break;
			return flag;
		}

		public static Pawn NonMentalColonist(bool withViolence, Func<Pawn, bool> validator = null, Map map = null)
		{
			static float SkillWeight(SkillRecord skill)
				=> skill.levelInt * (
					skill.passion == Passion.None ? 1f : skill.passion == Passion.Minor ? 1.5f : 2f
				);

			var candidates = PawnsFinder
				.AllMaps_FreeColonistsSpawned
					.Where(pawn => validator == null || validator(pawn)
						&& (map == null || pawn.Map == map)
						&& pawn.InMentalState == false
						&& pawn.Downed == false
						&& pawn.IsPrisoner == false
						&& pawn.IsSlave == false
						&& pawn.health.State == PawnHealthState.Mobile
						&& (withViolence == false || pawn.WorkTagIsDisabled(WorkTags.Violent) == false))
					.ToList();
			if (candidates.Count == 0)
				return null;
			return candidates.RandomElementByWeight(pawn => pawn.skills.skills.Sum(SkillWeight));
		}

		public static bool MakeGameCondition(GameConditionDef def, int duration)
		{
			if (Find.CurrentMap.GameConditionManager.ConditionIsActive(def))
			{
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {def.defName} => false");
				return false;
			}
			var gameCondition = GameConditionMaker.MakeCondition(def, -1);
			gameCondition.Duration = duration;
			Find.CurrentMap.GameConditionManager.RegisterCondition(gameCondition);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {def.defName} => true");
			return true;
		}

		public static Pawn allowedKillingSpree = null;
		public static bool MakeMentalBreak(MentalStateDef def, float percentage, bool isViolent, Func<Pawn, bool> validator = null)
		{
			return RepeatWIthPercentageOfColonists(percentage, () =>
			{
				var pawn = NonMentalColonist(isViolent, validator);
				if (pawn == null)
				{
					AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeMentalBreak no colonist avail => false");
					return false;
				}
				if (def == MentalStateDefOf.SocialFighting)
				{
					var otherPawn = NonMentalColonist(true, p => p != pawn, pawn.Map);
					if (otherPawn == null)
					{
						AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeMentalBreak no other colonist avail => false");
						return false;
					}
					AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {pawn.LabelShortCap}-{otherPawn.LabelShortCap} {def.defName}");
					pawn.interactions.StartSocialFight(otherPawn, "MessageSocialFight");
					return true;
				}
				allowedKillingSpree = pawn;
				var result = pawn.mindState.mentalStateHandler.TryStartMentalState(def, null, true);
				allowedKillingSpree = null;
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {pawn.LabelShortCap} {def.defName} => {result}");
				return result;
			});
		}

		static readonly HashSet<HediffDef> excludedHediffs = new()
		{
			HediffDefOf.Heatstroke,
			HediffDefOf.Hypothermia,
			HediffDefOf.BloodLoss,
			Defs.HearingLoss
		};
		public static bool MakeRandomHediffGiver()
		{
			var pawn = NonMentalColonist(false);
			if (pawn == null)
			{
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomHediffGiver no colonist avail => false");
				return false;
			}

			var hediffGivers = ThingDefOf.Human.race.hediffGiverSets
				.SelectMany((HediffGiverSetDef set) => set.hediffGivers)
				.Where(giver => excludedHediffs.Contains(giver.hediff) == false);
			var hediffGiver = hediffGivers.RandomElement();
			//Log.Warning($"# {hediffGivers.Join(hg => hg.hediff.defName, " ")} => {hediffGiver.hediff.defName}");
			var result = hediffGiver.TryApply(pawn, null);
			if (result)
				Find.LetterStack.ReceiveLetter("ColonistDiseaseTitle".Translate(), "ColonistDiseaseContent".Translate(pawn.LabelShortCap, hediffGiver.hediff.description), LetterDefOf.NegativeEvent, pawn);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {hediffGiver.hediff.description} => {result}");
			return result;
		}

		public static bool MakeRandomDisease(float percentage, bool isAnimal)
		{
			var category = isAnimal ? IncidentCategoryDefOf.DiseaseAnimal : IncidentCategoryDefOf.DiseaseHuman;
			var incidentDef = Tools.AllIncidentDefs()
				.Where(def => def.category == category)
				.RandomElement();
			var map = Reporter.Instance.ChosenMap;
			var result = false;
			if (map != null)
			{
				var parms = new IncidentParms
				{
					target = map,
					forced = true
				};
				incidentDef.diseaseVictimFractionRange = new FloatRange(percentage, percentage);
				incidentDef.diseaseMaxVictims = 999;
				result = incidentDef.Worker.TryExecute(parms);
			}
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {incidentDef.defName} {percentage:P0} [{isAnimal}] => {result}");
			return result;
		}

		public static bool MakeBzztOnAllBatteries()
		{
			var batteries = Tools.AllBatteries().Where(b => b.GetComp<CompPowerBattery>().StoredEnergy > 100f).ToList();
			foreach (var battery in batteries)
			{
				battery.ticksToExplode = Rand.Range(70, 150);
				battery.StartWickSustainer();
			}
			return batteries.Count > 0;
		}

		static readonly PawnKindDef[] pawnKinds = new[] { Defs.Squirrel, Defs.GuineaPig, Defs.Chinchilla, Defs.Tortoise, Defs.Rat };
		public static bool MegaManhunters()
		{
			var counter = 0;
			foreach (var map in Tools.PlayerMaps)
			{
				var amount = Mathf.Min(100, map.mapPawns.ColonistCount * 10);
				for (var i = 1; i <= amount; i++)
				{
					if (RCellFinder.TryFindRandomPawnEntryCell(out var spawnCenter, map, CellFinder.EdgeRoadChance_Animal))
					{
						var pawn = PawnGenerator.GeneratePawn(pawnKinds[counter % pawnKinds.Length]);
						var rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
						var loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10, null);
						_ = GenSpawn.Spawn(pawn, loc, map, rot, WipeMode.Vanish, false);
						_ = pawn.health.AddHediff(HediffDefOf.Scaria);
						_ = pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
						pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(60000, 90000);
						counter++;
					}
				}
			}
			if (counter > 0)
			{
				Find.TickManager.slower.SignalForceNormalSpeedShort();
				Find.LetterStack.ReceiveLetter("LetterLabelManhunterPackArrived".Translate(), "AnimalsLower".Translate(), LetterDefOf.ThreatBig, null);
				return true;
			}
			return false;
		}

		public static bool MakeExplodingBoomalopes()
		{
			var boomalopes = new List<Pawn>();
			foreach (var map in Tools.PlayerMaps)
			{
				if (RCellFinder.TryFindRandomPawnEntryCell(out var spawnCenter, map, CellFinder.EdgeRoadChance_Animal))
				{
					var amount = Mathf.Min(50, map.mapPawns.ColonistCount * 5);
					for (var i = 1; i <= amount; i++)
					{
						var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Boomalope);
						var rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
						var loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10, null);
						_ = GenSpawn.Spawn(pawn, loc, map, rot, WipeMode.Vanish, false);
						_ = pawn.health.AddHediff(HediffDefOf.Scaria);
						_ = pawn.needs.rest.CurLevel = Rand.Range(0.25f, 0.27f);
						_ = pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
						pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(60000, 90000);
						boomalopes.Add(pawn);
					}
				}
			}
			if (boomalopes.Any())
			{
				Find.TickManager.slower.SignalForceNormalSpeedShort();
				Find.LetterStack.ReceiveLetter("LetterLabelManhunterPackArrived".Translate(), "DeadlyBoomalopesArrive".Translate(PawnKindDefOf.Boomalope.GetLabelPlural(-1)), LetterDefOf.ThreatBig, boomalopes);
				return true;
			}
			return false;
		}

		public static bool MakeStuffFromAbove()
		{
			var maps = Tools.PlayerMaps.Where(map => map.mapPawns.FreeColonists.Any()).ToList();
			if (maps.Any() == false)
				return false;
			var map = maps.RandomElement();
			var incidentDef = Defs.StuffFromAbove;
			var parms = new IncidentParms { target = map, forced = true };
			var result = incidentDef.Worker.TryExecute(parms);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {incidentDef.defName} => {result}");
			return result;
		}

		static bool PunishmentChoice(int choice, params Func<bool>[] choices)
		{
			var idx = choice != -1 ? choice : Rand.RangeInclusive(0, choices.Length - 1);
			return choices[idx]();
		}

		public bool CommencePunishment(int choice = -1)
		{
			switch (punishLevel)
			{
				case 1:
					if (PunishmentChoice
					(
						choice,
						() =>
						{
							var ok = MakeGameCondition(GameConditionDefOf.Flashstorm, GenDate.TicksPerDay);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Flashstorm => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeGameCondition(GameConditionDefOf.PsychicDrone, GenDate.TicksPerDay);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} PsychicDrone => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeGameCondition(GameConditionDefOf.SolarFlare, GenDate.TicksPerDay);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} SolarFlare => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeGameCondition(GameConditionDefOf.ToxicFallout, GenDate.TicksPerDay);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} ToxicFallout => {ok}");
							return ok;
						}
					))
						return true;
					break;
				case 2:
					if (PunishmentChoice
					(
						choice,
						() =>
						{
							var ok = MakeMentalBreak(Defs.Slaughterer, 0f, true);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Slaughterer => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(MentalStateDefOf.SocialFighting, 0.25f, true); // careful, it involves 2 colonists at once
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} SocialFighting => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeRandomDisease(0.25f, true);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomDisease-Animal => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(Defs.Tantrum, 0.2f, true);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Tantrum => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeBzztOnAllBatteries();
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Bzzt On All Batteries => {ok}");
							return ok;
						}
					))
						return true;
					break;
				case 3:
					if (PunishmentChoice
					(
						choice,
						() =>
						{
							var ok = MakeMentalBreak(MentalStateDefOf.Berserk, 0.4f, true);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Berserk => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeRandomDisease(0.4f, false);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomDisease-Human => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(Defs.InsultingSpree, 0.25f, false);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} InsultingSpree => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MegaManhunters();
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} AngryAnimals => {ok}");
							return ok;
						}
					))
						return true;
					break;
				case 4:
					if (PunishmentChoice
					(
						choice,
						() =>
						{
							var ok = MakeMentalBreak(Defs.KillingSpree, 0f, true, Tools.HasSimpleWeapon);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} KillingSpree => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(Defs.TargetedInsultingSpree, 0.25f, false);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} TargetedInsultingSpree => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeRandomHediffGiver();
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomHediffGiver => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeExplodingBoomalopes();
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} ExplodingBoomalopes => {ok}");
							return ok;
						}
					))
						return true;
					break;
				case 5:
					if (PunishmentChoice
					(
						choice,
						() =>
						{
							var ok = false;
							var looser = NonMentalColonist(false);
							if (looser != null)
							{
								looser.skills.skills.Do(skill => skill.levelInt /= 2);
								Find.LetterStack.ReceiveLetter("ColonistBecomesDumberTitle".Translate(looser.LabelShortCap), "ColonistBecomesDumberText".Translate(looser.LabelShortCap), LetterDefOf.NegativeEvent, looser, null);
								ok = true;
							}
							AsyncLogger.Warning($"BLOOD GOD #{punishLevel} ColonistBecomesDumber => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(Defs.MurderousRage, 0.25f, true);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MurderousRage => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeMentalBreak(Defs.GiveUpExit, 0f, false);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} GiveUpExit => {ok}");
							return ok;
						},
						() =>
						{
							var ok = MakeStuffFromAbove();
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} StuffFromAbove => {ok}");
							return ok;
						}
					))
						return true;
					break;
			}
			return false;
		}

		public void Draw(float leftX, ref float curBaseY)
		{
			if (IsInactive)
				return;

			var f = Instance.RisingClamped01();
			var left = leftX + 18;
			var top = curBaseY - 7;
			Widgets.DrawBoxSolid(new Rect(left + 22, top - 8, 103, 5), scaleBG);
			Widgets.DrawBoxSolid(new Rect(left + 23, top - 9, f * 103, 5), scaleFG);
			if (Instance.state == State.Pausing)
			{
				f = GenMath.LerpDoubleClamped(startTicks, pauseTicks, 1, 0, Find.TickManager.TicksGame);
				Widgets.DrawBoxSolid(new Rect(left + 23, top - 11, f * 103, 3), scaleCD);
			}
			var i = Mathf.Clamp(Instance.punishLevel, 0, Assets.Pentas.Length - 1);
			GUI.DrawTexture(new Rect(left, top - 24, 26, 24), Assets.Pentas[i]);

			var mouseRect = new Rect(leftX, top - 24, 200, 24);
			if (Mouse.IsOver(mouseRect))
			{
				Widgets.DrawHighlight(mouseRect);
				if (state != State.Cooldown)
					TooltipHandler.TipRegion(mouseRect, new TipSignal("BloodGodScaleHelp".Translate(), 742863));
			}

			curBaseY -= 24;
		}
	}
}
