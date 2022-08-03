using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Dialog_DownloadingMods : Window
	{
		TaggedString text;

		public override Vector2 InitialSize => new(320f, 180f);

		public Dialog_DownloadingMods(TaggedString text)
		{
			this.text = text;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(inRect, text);
		}

		public override void OnCancelKeyPressed()
		{
		}

		public override void OnAcceptKeyPressed()
		{
		}
	}
}
