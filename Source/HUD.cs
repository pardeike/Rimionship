using Api;
using HarmonyLib;
using RimionshipServer.API;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class HUD
	{
		static readonly Score EmptyScore = new() { Position = 0, TwitchName = " ", LatestScore = 0 };

		public static void Update(HelloResponse response)
		{
			if (Current.ProgramState != ProgramState.Playing)
				return;

			SetName(response.TwitchName);

			var scores = response.GetScores();
			var index = scores.FindIndex(score => score.TwitchName == response.TwitchName);
			if (scores.Count == 0 || index < 0)
			{
				SetPlacement(0);
				SetScore(0);
				SetPlacements(Array.Empty<string>());
				SetScores(Array.Empty<int>());
				SetArrow(-99);
				return;
			}

			Log.Warning($"# {scores.Join(s => $"{s.Position}|{s.TwitchName}|{s.LatestScore}", " , ")}");

			switch (scores.Count)
			{
				case 0:
					scores.AddRange(new[] { EmptyScore, EmptyScore, EmptyScore });
					break;
				case 1:
					scores.Insert(0, EmptyScore);
					scores.Add(EmptyScore);
					if (index >= 0)
						index++;
					break;
				case 2:
					if (index == 0)
					{
						scores.Insert(0, EmptyScore);
						if (index >= 0)
							index++;
					}
					else
						scores.Add(EmptyScore);
					break;
				default:
					break;
			}

			SetPlacement(response.Position);
			SetScore(scores[index].LatestScore);
			SetPlacements(scores.Select(score => score.TwitchName).ToArray());
			SetScores(scores.Select(score => score.LatestScore).ToArray());
			SetArrow(index);
		}

		public static void SetName(string name)
		{
			Assets.infoName.text = name.ToUpper();
		}

		public static void SetScore(int score)
		{
			Assets.infoScore.text = score.DotFormatted();
		}

		public static void SetPlacement(int place)
		{
			Assets.placement.text = place < 1 ? " " : $"#{place}";
		}

		public static void SetArrow(int n)
		{
			var p = Assets.arrowAnchoredPosition3D;
			Assets.arrowTransform.anchoredPosition3D = new Vector3(p.x, -16.5f * (n - 1), p.z);
		}

		public static void SetPlacements(params string[] placements)
		{
			Assets.placements.text = placements.Join(p => p.ToUpper(), "\n");
		}

		public static void SetScores(params int[] scores)
		{
			Assets.scores.text = scores.Join(n => n.DotFormatted(true), "\n");
		}

		public static void SetPanelVisble(bool state)
		{
			Assets.statsAnimator.ResetTrigger(state ? "appear" : "vanish");
			Assets.statsAnimator.SetTrigger(state ? "appear" : "vanish");
		}

		public static void SetPanelSize(bool expanded)
		{
			Assets.panelAnimator.SetTrigger(expanded ? "full" : "small");
		}
	}
}
