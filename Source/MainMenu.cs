using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class MainMenu
	{
		static readonly Rect texCoords = new(0, 0, 1, 1);
		static GUIStyle[] labelStyles;

		public static void OnGUI(float x, float y)
		{
			if (labelStyles == null)
			{
				labelStyles = new[]
				{
					new GUIStyle()
					{
						font = Assets.menuFontSmall,
						//fontSize = 10,
						alignment = TextAnchor.MiddleCenter,
						padding = new RectOffset(0, 0, 0, 0),
						normal = new GUIStyleState() { textColor = Color.white }
					},
					new GUIStyle()
					{
						font = Assets.menuFontLarge,
						//fontSize = 14,
						alignment = TextAnchor.MiddleCenter,
						padding = new RectOffset(0, 0, 0, 0),
						normal = new GUIStyleState() { textColor = Color.white }
					}
				};
			}

			var txts = new string[]
			{
				 "Verbunden",
				 "Nicht registriert",
				 "OK",
				 "Start um 11:05"
			};

			if (Event.current.type != EventType.Repaint)
				return;

			var scale = UI.screenWidth * Prefs.UIScale < 1600 ? 2f : 1f;
			var w = Assets.MainMenuInfo.width / scale;
			var h = Assets.MainMenuInfo.height / scale;
			var oy = 48f / scale;

			var rect = new Rect(x - w, y - h / 2f + oy, w, h);
			GUI.DrawTextureWithTexCoords(rect, Assets.MainMenuInfo, texCoords);

			var field = new Rect(rect.x + 29 / scale, rect.y + 29 / scale, 180 / scale, 140 / scale);
			for (var i = 0; i < 4; i++)
			{
				var stateTex = Assets.StateOK;
				if (i == 1) stateTex = Assets.StateError;
				if (i == 3) stateTex = Assets.StateWait;

				var r = new Rect(0, 0, 32 / scale, 32 / scale).CenteredOnXIn(field).CenteredOnYIn(field);
				r.y += 40 / scale;
				Graphics.DrawTexture(r, stateTex);

				GUI.Label(field, txts[i], labelStyles[scale == 1f ? 1 : 0]);

				field.x += 200 / scale;
			}
		}
	}
}
