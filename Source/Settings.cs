using RimWorld;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Settings : ModSettings
	{
		public float scaleFactor = 0.2f;
		public float goodTraitSuppression = 0.7f;
		public float badTraitSuppression = 0.15f;

		public int maxFreeColonistCount = 5;
		public int risingInterval = 120000; // 2 days
		public int risingCooldown = 60000; // 1 day

		public int randomStartPauseMin = 140;
		public int randomStartPauseMax = 600;

		public int startPauseInterval = 30000; // 0.5 day
		public int finalPauseInterval = 5000; // 2 hours

		public float minThoughtFactor = 1f;
		public float maxThoughtFactor = 3f;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref scaleFactor, "scaleFactor", 0.2f);
			Scribe_Values.Look(ref goodTraitSuppression, "goodTraitSuppression", 0.7f);
			Scribe_Values.Look(ref badTraitSuppression, "badTraitSuppression", 0.15f);

			Scribe_Values.Look(ref maxFreeColonistCount, "maxFreeColonistCount", 5);
			Scribe_Values.Look(ref risingInterval, "risingInterval", 120000);
			Scribe_Values.Look(ref risingCooldown, "risingCooldown", 60000);

			Scribe_Values.Look(ref randomStartPauseMin, "randomStartPauseMin", 140);
			Scribe_Values.Look(ref randomStartPauseMax, "randomStartPauseMax", 600);

			Scribe_Values.Look(ref startPauseInterval, "startPauseInterval", 30000);
			Scribe_Values.Look(ref finalPauseInterval, "finalPauseInterval", 5000);

			Scribe_Values.Look(ref minThoughtFactor, "minThoughtFactor", 1f);
			Scribe_Values.Look(ref maxThoughtFactor, "maxThoughtFactor", 3f);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = inRect.width / 2f };
			list.Begin(inRect);
			list.Gap(12f);

			if (Tools.DevMode)
			{
				_ = list.Label("DEV MODE ON - Server does not control settings");
				list.Gap(12f);
			}

			_ = list.Label($"Traits Change Factor: {scaleFactor:P0}");
			scaleFactor = list.Slider(scaleFactor, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Good Traits Suppression: {goodTraitSuppression:P0}");
			goodTraitSuppression = list.Slider(goodTraitSuppression, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Bad Traits Suppression: {badTraitSuppression:P0}");
			badTraitSuppression = list.Slider(badTraitSuppression, 0, 1);
			list.Gap(12f);

			_ = list.Label("Graph");
			_ = list.TextEntry("https://www.desmos.com/calculator/psoxn2lt1r");
			list.Gap(12f);

			_ = list.Label($"Blood God Max Free Colonists: {maxFreeColonistCount}");
			maxFreeColonistCount = (int)list.Slider(maxFreeColonistCount, 1, 10);
			list.Gap(12f);

			_ = list.Label($"Rising Interval: {risingInterval.ToStringTicksToPeriod()}");
			risingInterval = (int)(GenDate.TicksPerHour * list.Slider(risingInterval / (float)GenDate.TicksPerHour, 0, 96));
			list.Gap(12f);

			_ = list.Label($"Rising Cooldown: {risingCooldown.ToStringTicksToPeriod()}");
			risingCooldown = (int)(GenDate.TicksPerHour * list.Slider(risingCooldown / (float)GenDate.TicksPerHour, 0, 96));
			list.Gap(12f);

			_ = list.Label($"Pause Before Punishment Range: {randomStartPauseMin.ToStringTicksToPeriod()} - {randomStartPauseMax.ToStringTicksToPeriod()}");
			randomStartPauseMin = (int)list.Slider(randomStartPauseMin, 0, 1200);
			randomStartPauseMax = (int)list.Slider(randomStartPauseMax, 0, 1200);

			list.NewColumn();

			_ = list.Label($"Punishment Interval Tier 1: {startPauseInterval.ToStringTicksToPeriod()}");
			startPauseInterval = (int)list.Slider(startPauseInterval, 0, 120000);
			list.Gap(12f);

			_ = list.Label($"Punishment Interval Tier 5: {finalPauseInterval.ToStringTicksToPeriod()}");
			finalPauseInterval = (int)list.Slider(finalPauseInterval, 0, 15000);
			list.Gap(12f);

			_ = list.Label($"Sacrification Thought Factor Range: {minThoughtFactor:F2} - {maxThoughtFactor:F2}");
			minThoughtFactor = list.Slider(minThoughtFactor, 0.1f, 4f);
			maxThoughtFactor = list.Slider(maxThoughtFactor, 0.1f, 4f);

			list.End();
		}
	}
}
