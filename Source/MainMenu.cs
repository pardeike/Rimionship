using System;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class MainMenu
	{
		static GUIStyle[] labelStyles = null;

		static readonly Func<(string, Texture2D, Action)>[] textFunctions = new Func<(string, Texture2D, Action)>[]
		{
			() =>
			{
				var state = $"CommState{Communications.State}".Translate();
				return
				(
					state,
					Communications.State == CommState.Ready ? Assets.StateOK : Assets.StateError,
					null
				);
			},
			() =>
			{
				var state = $"Mod{(PlayState.modRegistered ? "" : "Not")}Registered".Translate();
				return
				(
					state,
					PlayState.modRegistered ? Assets.StateOK : (DateTime.Now.Millisecond % 1500) < 750 ? null : Assets.StateAction,
					PlayState.modRegistered ? (Action)null : () => ServerAPI.SendHello(true)
				);
			},
			() =>
			{
				var state = $"Modlist{(PlayState.modlistValid ? "Valid" : "Invalid")}".Translate();
				return
				(
					state,
					PlayState.modlistValid ? Assets.StateOK : Assets.StateError,
					null
				);
			},
			() =>
			{
				var args = Array.Empty<NamedArgument>();
				if (PlayState.tournamentState == TournamentState.Prepare)
				{
					args = new[]
					{
						new NamedArgument($"{PlayState.tournamentStartHour:D2}", "hour"),
						new NamedArgument($"{PlayState.tournamentStartMinute:D2}", "minute"),
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
				return
				(
					state,
					icon,
					null
				);
			}
		};

		public static void OnGUI(float x, float y)
		{
			labelStyles ??= new[]
			{
				Assets.menuFontSmall.GUIStyle(Color.white),
				Assets.menuFontLarge.GUIStyle(Color.white)
			};

			var scale = UI.screenWidth * Prefs.UIScale < 1600 ? 2f : 1f;

			Rect rect;

			/*if (PlayState.modRegistered == false)
			{
				var w = Assets.MainMenuLogin.width / scale;
				var h = Assets.MainMenuLogin.height / scale;
				rect = new Rect(x - w, y - h / 2f, w, h);
				var tex = Assets.MainMenuLogin;
				if (Mouse.IsOver(rect))
				{
					tex = Assets.MainMenuLoginOver;
					if (Widgets.ButtonInvisible(rect))
						ServerAPI.SendHello();
				}
				GUI.DrawTextureWithTexCoords(rect, tex, Tools.Rect01);
				return;
			}
			else
			{*/
			var w = Assets.MainMenuInfo.width / scale;
			var h = Assets.MainMenuInfo.height / scale;
			var oy = 48f / scale;
			rect = new Rect(x - w, y - h / 2f + oy, w, h);
			GUI.DrawTextureWithTexCoords(rect, Assets.MainMenuInfo, Tools.Rect01);
			//}

			var field = new Rect(rect.x + 29 / scale, rect.y + 29 / scale, 180 / scale, 140 / scale);
			for (var i = 0; i < 4; i++)
			{
				var text = textFunctions[i]().Item1;
				var state = textFunctions[i]().Item2;
				var action = textFunctions[i]().Item3;

				if (state != null)
				{
					var r = new Rect(0, 0, 32 / scale, 32 / scale).CenteredOnXIn(field).CenteredOnYIn(field);
					r.y += 40 / scale;
					Graphics.DrawTexture(r, state);
				}

				GUI.Label(field, text, labelStyles[scale == 1f ? 1 : 0]);

				if (action != null)
				{
					if (Mouse.IsOver(field))
					{
						Widgets.DrawHighlight(field);
						if (Widgets.ButtonInvisible(field))
							action();
					}
				}

				field.x += 200 / scale;
			}
		}
	}
}
