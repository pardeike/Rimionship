using Api;
using HarmonyLib;
using RimionshipServer.API;
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

			var scores = response.GetScores();
			var emptyScores = scores.Count == 0;

			var index = scores.FindIndex(score => score.TwitchName == response.TwitchName);
			var myScore = index < 0 ? 0 : scores[index].LatestScore;

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

			SetName(response.TwitchName);
			SetPlacement(emptyScores ? 0 : response.Position);
			SetScore(myScore);
			SetPlacements(scores.Select(score => score.TwitchName).ToArray());
			SetScores(scores.Select(score => score.LatestScore).ToArray());
			SetArrow(emptyScores ? -99 : index);
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
			Assets.arrowTransform.anchoredPosition3D = new Vector3(p.x, 16.5f * (n - 1), p.z);
			Log.Warning($"[{n}] -> {p} -> {Assets.arrowTransform.anchoredPosition3D}");
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
