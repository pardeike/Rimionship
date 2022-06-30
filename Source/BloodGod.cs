using RimWorld;
using RimWorld.Planet;
using System;
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

		static readonly int maxFreeColonistCount = 3;
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
					if (PunishColonist())
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

		public bool PunishColonist()
		{
			Defs.Thunder.PlayOneShotOnCamera();
			// TODO
			Log.Error($"Punish level {punishLevel}");
			return true; // success
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
