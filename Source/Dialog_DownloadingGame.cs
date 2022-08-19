using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Dialog_DownloadingGame : Window
	{
		public override Vector2 InitialSize => new(240f, 75f);

		public Dialog_DownloadingGame()
		{
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(inRect, "DownloadingGame".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
		}

		public override void OnCancelKeyPressed()
		{
		}

		public override void OnAcceptKeyPressed()
		{
		}
	}
}
