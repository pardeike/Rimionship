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
		static readonly Color scaleBG = new(1, 1, 1, 0.25f);
		static readonly Color scaleFG = new(227f / 255f, 38f / 255f, 38f / 255f);
		static readonly SoundInfo onCameraPerTick = SoundInfo.OnCamera(MaintenanceType.PerTick);

		static readonly int maxFreeColonistCount = 5;
		static readonly int risingInterval = GenDate.TicksPerDay * 2;

		static readonly int randomStartPauseMin = 140;
		static readonly int randomStartPauseMax = 600;

		static readonly int startPauseInterval = GenDate.TicksPerDay / 2;
		static readonly int finalPauseInterval = GenDate.TicksPerHour * 2;

		public enum State
		{
			Idle,
			Rising,
			Preparing,
			Punishing,
			Pausing,
		}

		public State state;
		public int startTicks;
		public int randomPause;
		public int punishLevel;

		private Sustainer ambience;

		public BloodGod(World world) : base(world)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref state, "state");
			Scribe_Values.Look(ref startTicks, "startTicks");
			Scribe_Values.Look(ref randomPause, "randomPause");
			Scribe_Values.Look(ref punishLevel, "punishLevel");
		}

		public float RisingClamped01()
		{
			if (state == State.Idle) return 0f;
			if (state > State.Rising) return 1f;
			return Mathf.Clamp01((Find.TickManager.TicksGame - startTicks) / (float)risingInterval);
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();

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

			if (60.EveryNTick() == false) return;

			if (Stats.AllColonists() <= maxFreeColonistCount)
				state = State.Idle;

			switch (state)
			{
				case State.Idle:
					if (Stats.AllColonists() > maxFreeColonistCount)
					{
						startTicks = Find.TickManager.TicksGame;
						state = State.Rising;
					}
					break;

				case State.Rising:
					if (Find.TickManager.TicksGame - startTicks > risingInterval)
					{
						randomPause = Find.TickManager.TicksGame + Rand.Range(randomStartPauseMin, randomStartPauseMax);
						Defs.Bloodgod.PlayOneShotOnCamera();
						punishLevel = 1;
						state = State.Preparing;
					}
					break;

				case State.Preparing:
					if (Find.TickManager.TicksGame > randomPause)
					{
						startTicks = Find.TickManager.TicksGame;
						state = State.Punishing;
					}
					break;

				case State.Punishing:
					if (CommencePunishment())
						state = State.Pausing;
					break;

				case State.Pausing:
					var interval = GenMath.LerpDoubleClamped(1, 5, startPauseInterval, finalPauseInterval, punishLevel);
					if (Find.TickManager.TicksGame - startTicks > interval)
					{
						startTicks = Find.TickManager.TicksGame;
						punishLevel = Math.Min(punishLevel + 1, 5);
						Defs.Bloodgod.PlayOneShotOnCamera();
						state = State.Preparing;
					}
					break;
			}
		}

		public void Satisfy()
		{
			state = State.Idle;
		}

		public static Pawn NonMentalColonist()
		{
			static float SkillWeight(SkillRecord skill) => skill.levelInt * (skill.passion == Passion.None ? 1f : skill.passion == Passion.Minor ? 1.5f : 2f);

			var candidates = PawnsFinder
				.AllMaps_FreeColonistsSpawned
					.Where(pawn =>
						pawn.InMentalState == false
						&& pawn.Downed == false
						&& pawn.IsPrisoner == false
						&& pawn.IsSlave == false
						&& pawn.health.State == PawnHealthState.Mobile)
					.ToList();
			if (candidates.Count == 0) return null;
			return candidates.RandomElementByWeight(pawn => pawn.skills.skills.Sum(SkillWeight));
		}

		public static bool MakeGameCondition(GameConditionDef def, int duration)
		{
			if (Find.CurrentMap.GameConditionManager.ConditionIsActive(def))
			{
				Log.Warning($"# {def.defName} => false");
				return false;
			}
			var gameCondition = GameConditionMaker.MakeCondition(def, -1);
			gameCondition.Duration = duration;
			Find.CurrentMap.GameConditionManager.RegisterCondition(gameCondition);
			Log.Warning($"# {def.defName} => true");
			return true;
		}

		public static bool MakeMentalBreak(string defName)
		{
			var pawn = NonMentalColonist();
			if (pawn == null)
			{
				Log.Warning($"# MakeMentalBreak no colonist avail => false");
				return false;
			}
			var result = defName.Def().Worker.TryStart(pawn, null, false);
			Log.Warning($"# {pawn.LabelShortCap} {defName} => {result}");
			return result;
		}

		public static bool MakeRandomHediffGiver()
		{
			var pawn = NonMentalColonist();
			if (pawn == null)
			{
				Log.Warning($"# MakeRandomHediffGiver no colonist avail => false");
				return false;
			}
			var hediffGiver = ThingDefOf.Human.race.hediffGiverSets
				.SelectMany((HediffGiverSetDef set) => set.hediffGivers)
				.RandomElement();
			var result = hediffGiver.TryApply(pawn, null);
			Log.Warning($"# {hediffGiver} => {result}");
			return result;
		}

		public static bool MakeRandomDisease(bool isAnimal)
		{
			var category = isAnimal ? IncidentCategoryDefOf.DiseaseAnimal : IncidentCategoryDefOf.DiseaseHuman;
			var incidentDef = Tools.AllIncidentDefs()
				.Where(def => def.category == category)
				.RandomElement();
			var result = Find.Storyteller.TryFire(new FiringIncident(incidentDef, null));
			Log.Warning($"# {incidentDef.defName} [{isAnimal}] => {result}");
			return result;
		}

		public bool CommencePunishment()
		{
			Defs.Thunder.PlayOneShotOnCamera();
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
							if (MakeMentalBreak("Slaughterer"))
								return true;
							break;
						case 2:
							if (MakeMentalBreak("SocialFighting"))
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
							if (MakeMentalBreak("Berserk"))
								return true;
							break;
						case 2:
							if (MakeRandomHediffGiver())
								return true;
							break;
						case 3:
							if (MakeMentalBreak("InsultingSpree"))
								return true;
							break;
					}
					break;
				case 4:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							if (MakeMentalBreak("SadisticRage"))
								return true;
							break;
						case 2:
							if (MakeMentalBreak("TargetedInsultingSpree"))
								return true;
							break;
						case 3:
							if (MakeRandomDisease(false))
								return true;
							break;
					}
					break;
				case 5:
					switch (Rand.RangeInclusive(1, 3))
					{
						case 1:
							var looser = NonMentalColonist();
							Log.Warning($"# ColonistBecomesDumber => {looser != null}");
							if (looser != null)
							{
								looser.skills.skills.Do(skill => skill.levelInt /= 2);
								Messages.Message("ColonistBecomesDumber".Translate(looser.LabelShortCap), MessageTypeDefOf.NegativeEvent);
								return true;
							}
							break;
						case 2:
							if (MakeMentalBreak("MurderousRage"))
								return true;
							break;
						case 3:
							if (MakeMentalBreak("GiveUpExit"))
								return true;
							break;
					}
					break;
			}
			return false;
		}

		public static void Draw(float leftX, ref float curBaseY)
		{
			var bloodGod = Current.Game.World.GetComponent<BloodGod>();

			var f = bloodGod.RisingClamped01();
			var n = bloodGod.state >= State.Rising ? (int)(1 + 4 * f) : 0;
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
				TooltipHandler.TipRegion(mouseRect, new TipSignal("BloodGodScaleHelp".Translate(), 742863));
			}

			curBaseY -= 24;
		}
	}
}
