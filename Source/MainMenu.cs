using System;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class MainMenu
	{
		static GUIStyle[] labelStyles = null;

		static readonly Func<(string, Texture2D, string, Action, Func<string>)>[] textFunctions = new Func<(string, Texture2D, string, Action, Func<string>)>[]
		{
			() =>
			{
				var state = $"CommState{Communications.State}".Translate();
				return
				(
					state,
					Communications.State == CommState.Ready ? Assets.StateOK : (ServerAPITools.modTooOld ? Assets.StateAction: Assets.StateError),
					null,
					null,
					() => ServerAPITools.modTooOld ? "ModTooOld".Translate() : null
				);
			},

			() =>
			{
				var state = Communications.State == CommState.Ready
					? $"Mod{(PlayState.modRegistered ? "" : "Not")}Registered".Translate()
					: "ModRegistrationUnknown".Translate();
				return
				(
					state,
					PlayState.modRegistered
						? Assets.StateOK
						: Communications.State == CommState.Ready
							? (DateTime.Now.Millisecond % 1500 < 750 ? null : Assets.StateAction)
							: Assets.StateWait,
					"RegisterMod".Translate(),
					PlayState.modRegistered
						? null
						: Communications.State == CommState.Ready
							? (() => Task.Run(ServerAPI.Login))
							: null,
					null
				);
			},

			() =>
			{
				var state = $"Modlist{(PlayState.modlistStatus)}".Translate();
				return
				(
					state,
					PlayState.modlistStatus.ToAsset(),
					null,
					null,
					() => PlayState.modlistStatus == ModListStatus.Invalid ? PlayState.InvalidModsTooltip() : (string)null
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
				var state = Communications.State == CommState.Ready && PlayState.modRegistered
					? $"Tournament{PlayState.tournamentState}".Translate(args)
					: $"TournamentUnknown".Translate();
				var icon = Communications.State == CommState.Ready && PlayState.modRegistered
					? PlayState.tournamentState switch
						{
							TournamentState.Stopped => Assets.StateError,
							TournamentState.Training => Assets.StateOK,
							TournamentState.Prepare => Assets.StateWait,
							TournamentState.Started => Assets.StateOK,
							TournamentState.Completed => Assets.StateError,
							_ => Assets.StateError
						}
					: Assets.StateWait;
				return
				(
					state,
					icon,
					null,
					null,
					null
				);
			}
		};

		public static void OnGUI(float x, float y)
		{
			if (Assets.MainMenuInfo == null) // changing language breaks graphics
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
				var actionName = textFunctions[i]().Item3;
				var action = textFunctions[i]().Item4;
				var tooltip = textFunctions[i]().Item5;

				if (state != null)
				{
					var r = new Rect(0, 0, 32 / scale, 32 / scale).CenteredOnXIn(field).CenteredOnYIn(field);
					r.y += 40 / scale;
					Graphics.DrawTexture(r, state);
				}

				GUI.Label(field, text, labelStyles[scale == 1f ? 1 : 0]);

				if (tooltip != null)
				{
					var tip = tooltip();
					if (tip != null)
						TooltipHandler.TipRegion(field, () => tip, 545392561);
				}

				if (action != null)
				{
					var size = Text.CalcSize(actionName);
					if (size.x > field.width * 1.5f)
					{
						size.x = field.width * 1.5f;
						size.y = Text.CalcHeight(actionName, size.x);
					}

					var buttonRect = new Rect(Vector2.zero, size + new Vector2(40, 20)).CenteredOnXIn(field);
					buttonRect.center = field.center + new Vector2(0, 40 / scale);
					if (Widgets.ButtonText(buttonRect, actionName))
						action();
				}

				field.x += 200 / scale;
			}
		}
	}
}
