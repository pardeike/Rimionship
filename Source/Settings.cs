using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Settings : ModSettings
	{
		public float scaleFactor = 0.2f;
		public float goodTraitSuppression = 0.7f;
		public float badTraitSuppression = 0.15f;

		public int maxMeleeSkill = 6;
		public int maxMeleeFlames = 1;
		public int maxShootingSkill = 6;
		public int maxShootingFlames = 1;

		public int maxFreeColonistCount = 5;
		public int risingInterval = 2400000; // 40 days
		public int risingReductionPerColonist = 240000; // 4 days
		public int risingIntervalMinimum = 120000; // 2 days
		public int risingCooldown = 120000; // 2 day

		public int startPauseInterval = 120000; // 2 days
		public int finalPauseInterval = 120000; // 2 days

		public float minThoughtFactor = 1f;
		public float maxThoughtFactor = 3f;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref scaleFactor, "scaleFactor", 0.2f);
			Scribe_Values.Look(ref goodTraitSuppression, "goodTraitSuppression", 0.7f);
			Scribe_Values.Look(ref badTraitSuppression, "badTraitSuppression", 0.15f);

			Scribe_Values.Look(ref maxMeleeSkill, "maxMeleeSkill", 6);
			Scribe_Values.Look(ref maxMeleeFlames, "maxMeleeFlames", 1);
			Scribe_Values.Look(ref maxShootingSkill, "maxShootingSkill", 6);
			Scribe_Values.Look(ref maxShootingFlames, "maxShootingFlames", 1);

			Scribe_Values.Look(ref maxFreeColonistCount, "maxFreeColonistCount", 5);
			Scribe_Values.Look(ref risingInterval, "risingInterval", 2400000);
			Scribe_Values.Look(ref risingReductionPerColonist, "risingReductionPerColonist", 240000);
			Scribe_Values.Look(ref risingIntervalMinimum, "risingIntervalMinimum", 120000);
			Scribe_Values.Look(ref risingCooldown, "risingCooldown", 120000);

			Scribe_Values.Look(ref startPauseInterval, "startPauseInterval", 30000);
			Scribe_Values.Look(ref finalPauseInterval, "finalPauseInterval", 5000);

			Scribe_Values.Look(ref minThoughtFactor, "minThoughtFactor", 1f);
			Scribe_Values.Look(ref maxThoughtFactor, "maxThoughtFactor", 3f);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 17f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);

			_ = list.Label($"Traits Scale: {scaleFactor:P0}");
			scaleFactor = list.Slider(scaleFactor, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Good Traits Suppression: {goodTraitSuppression:P0}");
			goodTraitSuppression = list.Slider(goodTraitSuppression, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Bad Traits Suppression: {badTraitSuppression:P0}");
			badTraitSuppression = list.Slider(badTraitSuppression, 0, 1);
			list.Gap(12f);

			if (list.ButtonText("Help graph for trait values"))
				Application.OpenURL("https://www.desmos.com/calculator/psoxn2lt1r");
			list.Gap(20f);

			_ = list.Label($"Max Melee Skill: {maxMeleeSkill}");
			maxMeleeSkill = (int)list.Slider(maxMeleeSkill, 0, 20);
			list.Gap(12f);

			_ = list.Label($"Max Melee Flames: {maxMeleeFlames}");
			maxMeleeFlames = (int)list.Slider(maxMeleeFlames, 0, 2);
			list.Gap(12f);

			_ = list.Label($"Max Shooting Skill: {maxShootingSkill}");
			maxShootingSkill = (int)list.Slider(maxShootingSkill, 0, 20);
			list.Gap(12f);

			_ = list.Label($"Max Shooting Flames: {maxShootingFlames}");
			maxShootingFlames = (int)list.Slider(maxShootingFlames, 0, 2);

			list.NewColumn();

			_ = list.Label($"Blood God Max Free Colonists: {maxFreeColonistCount}");
			maxFreeColonistCount = (int)list.Slider(maxFreeColonistCount, 1, 10);
			list.Gap(20f);

			list.TimeEditor("Rising Interval", risingInterval, 1, Days.Instance, n => { risingInterval = n; });
			list.Gap(12f);

			list.TimeEditor("Rising Reduction Per Colonist", risingReductionPerColonist, 1, Days.Instance, n => { risingReductionPerColonist = n; });
			list.Gap(12f);

			list.TimeEditor("Rising Interval Minimum", risingIntervalMinimum, 1, Days.Instance, n => { risingIntervalMinimum = n; });
			list.Gap(12f);

			list.TimeEditor("Rising Cooldown", risingCooldown, 1, Days.Instance, n => { risingCooldown = n; });
			list.Gap(32f);

			list.TimeEditor("Punishment Interval Tier 1", startPauseInterval, 1, Hours.Instance, n => { startPauseInterval = n; });
			list.Gap(12f);

			list.TimeEditor("Punishment Interval Tier 5", finalPauseInterval, 1, Hours.Instance, n => { finalPauseInterval = n; });
			list.Gap(32f);

			_ = list.Label($"Sacrification Thought Factor Range: {minThoughtFactor:F2} - {maxThoughtFactor:F2}");
			minThoughtFactor = list.Slider(minThoughtFactor, 0.1f, 4f);
			maxThoughtFactor = list.Slider(maxThoughtFactor, 0.1f, 4f);

			list.End();
		}
	}
}
