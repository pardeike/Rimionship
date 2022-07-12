using System;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class MainMenu
	{
		static GUIStyle[] labelStyles = null;

		static readonly Func<(string, Texture2D)>[] textFunctions = new Func<(string, Texture2D)>[]
		{
			() =>
			{
				var state = $"CommState{Communications.State}".Translate();
				return (state, Communications.State == CommState.Ready ? Assets.StateOK : Assets.StateError);
			},
			() =>
			{
				var state = $"Mod{(PlayState.modRegistered ? "" : "Not")}Registered".Translate();
				return (state, PlayState.modRegistered ? Assets.StateOK : Assets.StateError);
			},
			() =>
			{
				var state = $"Modlist{(PlayState.modlistValid ? "Valid" : "Invalid")}".Translate();
				return (state, PlayState.modlistValid ? Assets.StateOK : Assets.StateError);
			},
			() =>
			{
				var args = Array.Empty<NamedArgument>();
				if (PlayState.tournamentState == TournamentState.Started)
				{
					args = new[]
					{
						new NamedArgument(PlayState.tournamentStartHour, "hour"),
						new NamedArgument(PlayState.tournamentStartMinute, "minute"),
					};
				}
				var state = $"Tournament{PlayState.tournamentState}".Translate(args);
				var icon = PlayState.tournamentState switch
				{
					TournamentState.Stopped => Assets.StateError,
					TournamentState.Training => Assets.StateOK,
					TournamentState.Prepare => Assets.StateWait,
					TournamentState.Started => Assets.StateOK,
					TournamentState.Completed => Assets.StateError,
					_ => Assets.StateError
				};
				return (state, icon);
			}
		};

		public static void OnGUI(float x, float y)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			labelStyles ??= new[]
			{
				Assets.menuFontSmall.GUIStyle(Color.white),
				Assets.menuFontLarge.GUIStyle(Color.white)
			};

			var scale = UI.screenWidth * Prefs.UIScale < 1600 ? 2f : 1f;
			var w = Assets.MainMenuInfo.width / scale;
			var h = Assets.MainMenuInfo.height / scale;
			var oy = 48f / scale;

			var rect = new Rect(x - w, y - h / 2f + oy, w, h);
			GUI.DrawTextureWithTexCoords(rect, Assets.MainMenuInfo, Tools.Rect01);

			var field = new Rect(rect.x + 29 / scale, rect.y + 29 / scale, 180 / scale, 140 / scale);
			for (var i = 0; i < 4; i++)
			{
				var text = textFunctions[i]().Item1;
				var state = textFunctions[i]().Item2;

				var r = new Rect(0, 0, 32 / scale, 32 / scale).CenteredOnXIn(field).CenteredOnYIn(field);
				r.y += 40 / scale;
				Graphics.DrawTexture(r, state);

				GUI.Label(field, text, labelStyles[scale == 1f ? 1 : 0]);

				field.x += 200 / scale;
			}
		}
	}
}
