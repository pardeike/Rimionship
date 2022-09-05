using System;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Dialog_Information : Window
	{
		const float headerSize = 40f;
		const float buttonSize = 35f;

		readonly string headline;
		readonly string body;
		readonly Action action;

		public override Vector2 InitialSize => new(640f, 380f);

		public Dialog_Information(string headline, string body, Action action)
		{
			this.headline = headline;
			this.body = body;
			this.action = action;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Medium;
			var rect = inRect.TopPartPixels(headerSize);
			Widgets.Label(rect, headline.Translate());
			Text.Font = GameFont.Small;
			rect = inRect.OffsetBy(0, headerSize).TopPartPixels(inRect.height - headerSize - buttonSize);
			Widgets.Label(rect, body.Translate());

			var x = inRect.width / 3f;
			if (Widgets.ButtonText(new Rect(x, inRect.height - buttonSize, x - 10f, buttonSize), "Agree".Translate()))
				Close(true);
		}

		public override void OnCancelKeyPressed()
		{
		}

		public override void OnAcceptKeyPressed()
		{
		}

		public override void PostClose()
		{
			base.PostClose();
			action?.Invoke();
		}

	}
}
