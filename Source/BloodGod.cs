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

		static readonly Color scaleBG = new(1, 1, 1, 0.25f);
		static readonly Color scaleFG = new(227f / 255f, 38f / 255f, 38f / 255f);
		static readonly SoundInfo onCameraPerTick = SoundInfo.OnCamera(MaintenanceType.PerTick);

		public enum State
		{
			Idle,
			Rising,
			Preparing,
			Punishing,
			Pausing,
			Cooldown
		}

		public State state;
		public int startTicks;
		public int cooldownTicks;
		public int randomPause;
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
			Scribe_Values.Look(ref cooldownTicks, "cooldownTicks");
			Scribe_Values.Look(ref randomPause, "randomPause");
			Scribe_Values.Look(ref punishLevel, "punishLevel");
		}

		public float RisingClamped01()
		{
			var currentTicks = Find.TickManager.TicksGame;
			if (state == State.Idle || state == State.Cooldown)
				return 0f;
			if (state > State.Rising)
				return 1f;
			return Mathf.Clamp01((currentTicks - startTicks) / (float)RimionshipMod.settings.risingInterval);
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
						StartPhase(State.Rising);
					break;

				case State.Rising:
					if (currentTicks - startTicks > RimionshipMod.settings.risingInterval)
					{
						var minPause = RimionshipMod.settings.randomStartPauseMin;
						var maxPause = RimionshipMod.settings.randomStartPauseMax;
						randomPause = currentTicks + Rand.Range(minPause, maxPause);
						Defs.Bloodgod.PlayOneShotOnCamera();
						punishLevel = 1;
						StartPhase(State.Preparing, setStartTicks: false);
					}
					break;

				case State.Preparing:
					if (currentTicks > randomPause)
						StartPhase(State.Punishing);
					break;

				case State.Punishing:
					if (CommencePunishment())
					{
						Defs.Thunder.PlayOneShotOnCamera();
						StartPhase(State.Pausing);
					}
					break;

				case State.Pausing:
					var minInterval = RimionshipMod.settings.startPauseInterval;
					var maxInterval = RimionshipMod.settings.finalPauseInterval;
					var interval = GenMath.LerpDoubleClamped(1, 5, minInterval, maxInterval, punishLevel);
					if (currentTicks - startTicks > interval)
					{
						punishLevel = Math.Min(punishLevel + 1, 5);
						Defs.Bloodgod.PlayOneShotOnCamera();
						StartPhase(State.Preparing);
					}
					break;

				case State.Cooldown:
					if (currentTicks > cooldownTicks)
						StartPhase(State.Rising);
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

			sacrification.sacrificer.GiveThought(ThoughtDefOf.EncouragingSpeech, factor);
			sacrification.sacrificer.GiveThought(ThoughtDefOf.KilledHumanlikeBloodlust, factor);

			var pawns = Tools.ColonistsNear(spot.Position, spot.Map, 7f);
			Tools.HasLineOfSightTo(spot.Position, spot.Map, pawns).Do(pawn => pawn.GiveThought(ThoughtDefOf.WitnessedDeathBloodlust, factor));

			cooldownTicks = Find.TickManager.TicksGame + RimionshipMod.settings.risingCooldown;
			StartPhase(State.Cooldown);
		}

		void StartPhase(State state, bool setStartTicks = true)
		{
			if (setStartTicks)
				startTicks = Find.TickManager.TicksGame;
			this.state = state;
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} phase => {state}");
		}

		public static Pawn NonMentalColonist(bool withViolence, Pawn exclude = null)
		{
			static float SkillWeight(SkillRecord skill)
				=> skill.levelInt * (
					skill.passion == Passion.None ? 1f : skill.passion == Passion.Minor ? 1.5f : 2f
				);

			var candidates = PawnsFinder
				.AllMaps_FreeColonistsSpawned
					.Where(pawn => pawn != exclude
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
				var otherPawn = NonMentalColonist(true, pawn);
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
			var reporter = Current.Game.World.GetComponent<Reporter>();
			var parms = new IncidentParms
			{
				target = reporter.ChosenMap,
				faction = Faction.OfPlayer,
				forced = true
			};
			var result = incidentDef.Worker.TryExecute(parms);
			AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} {incidentDef.defName} [{isAnimal}] => {result}");
			return result;
		}

		public bool CommencePunishment()
		{
			switch (punishLevel)
			{
				case 1:
					switch (Rand.RangeInclusive(1, 4))
					{
						case 1:
							if (MakeGameCondition(GameConditionDefOf.Flashstorm, GenDate.TicksPerDay))
								return true;
							break;
						case 2:
							if (MakeGameCondition(GameConditionDefOf.PsychicDrone, GenDate.TicksPerDay))
								return true;
							break;
						case 3:
							if (MakeGameCondition(GameConditionDefOf.SolarFlare, GenDate.TicksPerDay))
								return true;
							break;
						case 4:
							if (MakeGameCondition(GameConditionDefOf.ToxicFallout, GenDate.TicksPerDay))
								return true;
							break;
					}
					break;
				case 2:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							if (MakeMentalBreak(Defs.Slaughterer, true))
								return true;
							break;
						case 2:
							if (MakeMentalBreak(MentalStateDefOf.SocialFighting, true))
								return true;
							break;
						case 3:
							if (MakeRandomDisease(true))
								return true;
							break;
					}
					break;
				case 3:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							if (MakeMentalBreak(MentalStateDefOf.Berserk, true))
								return true;
							break;
						case 2:
							if (MakeRandomDisease(false))
								return true;
							break;
						case 3:
							if (MakeMentalBreak(Defs.InsultingSpree, false))
								return true;
							break;
					}
					break;
				case 4:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							if (MakeMentalBreak(Defs.SadisticRage, true))
								return true;
							break;
						case 2:
							if (MakeMentalBreak(Defs.TargetedInsultingSpree, false))
								return true;
							break;
						case 3:
							if (MakeRandomHediffGiver())
								return true;
							break;
					}
					break;
				case 5:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							var looser = NonMentalColonist(false);
							AsyncLogger.Warning($"BLOOD GOD #{Instance.punishLevel} ColonistBecomesDumber => {looser != null}");
							if (looser != null)
							{
								looser.skills.skills.Do(skill => skill.levelInt /= 2);
								var letter = LetterMaker.MakeLetter("ColonistBecomesDumberTitle".Translate(looser.LabelShortCap), "ColonistBecomesDumberText".Translate(looser.LabelShortCap), LetterDefOf.NegativeEvent, looser);
								Find.LetterStack.ReceiveLetter(letter, null);
								return true;
							}
							break;
						case 2:
							if (MakeMentalBreak(Defs.MurderousRage, true))
								return true;
							break;
						case 3:
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
			if (state == State.Idle)
				return;

			var f = Instance.RisingClamped01();
			var n = Instance.state >= State.Rising ? (int)(1 + 4 * f) : 0;
			if (f > 0.9f)
				n = (int)GenMath.LerpDoubleClamped(-0.9f, 0.9f, 0, 5, Mathf.Sin(Time.realtimeSinceStartup * 5));

			var left = leftX + 18;
			var top = curBaseY - 7;

			GUI.DrawTexture(new Rect(left, top - 24, 26, 24), Assets.Pentas[n]);
			Widgets.DrawBoxSolid(new Rect(left + 22, top - 10, 103, 3), scaleBG);
			Widgets.DrawBoxSolid(new Rect(left + 23, top - 11, f * 103, 3), scaleFG);

			var mouseRect = new Rect(leftX, top - 24 + 3, 200, 24 - 6);
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
