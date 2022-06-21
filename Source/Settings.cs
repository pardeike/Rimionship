using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Settings : ModSettings
	{
		public float scaleFactor = 0.2f;
		public float goodTraitSuppression = 0.7f;
		public float badTraitSuppression = 0.15f;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref scaleFactor, "scaleFactor", 0.2f);
			Scribe_Values.Look(ref goodTraitSuppression, "goodTraitSuppression", 0.7f);
			Scribe_Values.Look(ref badTraitSuppression, "badTraitSuppression", 0.15f);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = inRect.width / 2f };
			list.Begin(inRect);
			list.Gap(12f);

			_ = list.Label($"Scale: {scaleFactor:P0}");
			scaleFactor = list.Slider(scaleFactor, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Good Traits Suppression: {goodTraitSuppression:P0}");
			goodTraitSuppression = list.Slider(goodTraitSuppression, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Bad Traits Suppression: {badTraitSuppression:P0}");
			badTraitSuppression = list.Slider(badTraitSuppression, 0, 1);
			list.Gap(12f);

			_ = list.Label($"Graph");
			_ = list.TextEntry("https://www.desmos.com/calculator/psoxn2lt1r");

			list.End();
		}
	}
}
