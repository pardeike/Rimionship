using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
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
		public int cooldownTicks;
		public int punishLevel;

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
			Scribe_Values.Look(ref cooldownTicks, "cooldownTicks");
			Scribe_Values.Look(ref punishLevel, "punishLevel");
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
			Defs.Bloodgod.PlayWithCallback(0f, () => StartPhase(State.Punishing));
			punishLevel = Math.Min(punishLevel + 1, 5);
			AsyncLogger.Warning($"BLOOD GOD Level now #{punishLevel}");
			StartPhase(State.Announcing);
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			TickAmbience();

			if (60.EveryNTick() == false)
				return;
			var allColonists = Stats.AllColonists();

			if (allColonists <= RimionshipMod.settings.maxFreeColonistCount)
				StartPhase(State.Idle);

			var currentTicks = Find.TickManager.TicksGame;
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
					if (CommencePunishment())
					{
						Find.LetterStack.ReceiveLetter("PunishmentLetterTitle".Translate(), "PunishmentLetterContent".Translate(punishLevel), LetterDefOf.NegativeEvent, null);
						Defs.Thunder.PlayOneShotOnCamera();
						var minInterval = RimionshipMod.settings.startPauseInterval;
						var maxInterval = RimionshipMod.settings.finalPauseInterval;
						pauseTicks = Find.TickManager.TicksGame + (int)GenMath.LerpDoubleClamped(1, 5, minInterval, maxInterval, punishLevel);
						StartPhase(State.Pausing);
					}
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

		public static Pawn NonMentalColonist(bool withViolence, Pawn exclude = null, Map map = null)
		{
			static float SkillWeight(SkillRecord skill)
				=> skill.levelInt * (
					skill.passion == Passion.None ? 1f : skill.passion == Passion.Minor ? 1.5f : 2f
				);

			var candidates = PawnsFinder
				.AllMaps_FreeColonistsSpawned
					.Where(pawn => pawn != exclude
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

		public static bool MakeMentalBreak(MentalStateDef def, bool isViolent)
		{
			var pawn = NonMentalColonist(isViolent);
			if (pawn == null)
			{
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeMentalBreak no colonist avail => false");
				return false;
			}
			if (def == MentalStateDefOf.SocialFighting)
			{
				var otherPawn = NonMentalColonist(true, pawn, pawn.Map);
				if (otherPawn == null)
				{
					AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeMentalBreak no other colonist avail => false");
					return false;
				}
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {pawn.LabelShortCap}-{otherPawn.LabelShortCap} {def.defName}");
				pawn.interactions.StartSocialFight(otherPawn, "MessageSocialFight");
				return true;
			}
			var result = pawn.mindState.mentalStateHandler.TryStartMentalState(def, null, true);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {pawn.LabelShortCap} {def.defName} => {result}");
			return result;
		}

		public static bool MakeRandomHediffGiver()
		{
			var pawn = NonMentalColonist(false);
			if (pawn == null)
			{
				AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomHediffGiver no colonist avail => false");
				return false;
			}
			var hediffGiver = ThingDefOf.Human.race.hediffGiverSets
				.SelectMany((HediffGiverSetDef set) => set.hediffGivers)
				.RandomElement();
			var result = hediffGiver.TryApply(pawn, null);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {hediffGiver} => {result}");
			return result;
		}

		public static bool MakeRandomDisease(bool isAnimal)
		{
			var category = isAnimal ? IncidentCategoryDefOf.DiseaseAnimal : IncidentCategoryDefOf.DiseaseHuman;
			var incidentDef = Tools.AllIncidentDefs()
				.Where(def => def.category == category)
				.RandomElement();
			var parms = new IncidentParms
			{
				target = Reporter.Instance.ChosenMap,
				// faction = Faction.OfPlayer,
				forced = true
			};
			var result = incidentDef.Worker.TryExecute(parms);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {incidentDef.defName} [{isAnimal}] => {result}");
			return result;
		}

		static int PunishmentChoice(int max)
		{
			return Rand.RangeInclusive(1, max);
		}

		public bool CommencePunishment()
		{
			switch (punishLevel)
			{
				case 1:
					switch (PunishmentChoice(4))
					{
						case 1:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Flashstorm");
							if (MakeGameCondition(GameConditionDefOf.Flashstorm, GenDate.TicksPerDay))
								return true;
							break;
						case 2:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} PsychicDrone");
							if (MakeGameCondition(GameConditionDefOf.PsychicDrone, GenDate.TicksPerDay))
								return true;
							break;
						case 3:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} SolarFlare");
							if (MakeGameCondition(GameConditionDefOf.SolarFlare, GenDate.TicksPerDay))
								return true;
							break;
						case 4:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} ToxicFallout");
							if (MakeGameCondition(GameConditionDefOf.ToxicFallout, GenDate.TicksPerDay))
								return true;
							break;
					}
					break;
				case 2:
					switch (PunishmentChoice(3))
					{
						case 1:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Slaughterer");
							if (MakeMentalBreak(Defs.Slaughterer, true))
								return true;
							break;
						case 2:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} SocialFighting");
							if (MakeMentalBreak(MentalStateDefOf.SocialFighting, true))
								return true;
							break;
						case 3:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomDisease-Animal");
							if (MakeRandomDisease(true))
								return true;
							break;
					}
					break;
				case 3:
					switch (PunishmentChoice(3))
					{
						case 1:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} Berserk");
							if (MakeMentalBreak(MentalStateDefOf.Berserk, true))
								return true;
							break;
						case 2:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomDisease-Human");
							if (MakeRandomDisease(false))
								return true;
							break;
						case 3:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} InsultingSpree");
							if (MakeMentalBreak(Defs.InsultingSpree, false))
								return true;
							break;
					}
					break;
				case 4:
					switch (PunishmentChoice(3))
					{
						case 1:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} SadisticRage");
							if (MakeMentalBreak(Defs.SadisticRage, true))
								return true;
							break;
						case 2:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} TargetedInsultingSpree");
							if (MakeMentalBreak(Defs.TargetedInsultingSpree, false))
								return true;
							break;
						case 3:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MakeRandomHediffGiver");
							if (MakeRandomHediffGiver())
								return true;
							break;
					}
					break;
				case 5:
					switch (PunishmentChoice(3))
					{
						case 1:
							var looser = NonMentalColonist(false);
							AsyncLogger.Warning($"BLOOD GOD #{punishLevel} ColonistBecomesDumber => {looser != null}");
							if (looser != null)
							{
								looser.skills.skills.Do(skill => skill.levelInt /= 2);
								var letter = LetterMaker.MakeLetter("ColonistBecomesDumberTitle".Translate(looser.LabelShortCap), "ColonistBecomesDumberText".Translate(looser.LabelShortCap), LetterDefOf.NegativeEvent, looser);
								Find.LetterStack.ReceiveLetter(letter, null);
								return true;
							}
							break;
						case 2:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} MurderousRage");
							if (MakeMentalBreak(Defs.MurderousRage, true))
								return true;
							break;
						case 3:
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} GiveUpExit");
							if (MakeMentalBreak(Defs.GiveUpExit, false))
								return true;
							break;
					}
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
