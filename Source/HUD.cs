﻿using HarmonyLib;
using UnityEngine;

namespace Rimionship
{
	public static class HUD
	{
		public static void SetName(string name)
		{
			Assets.name.text = name.ToUpper();
		}

		public static void SetScore(int score)
		{
			Assets.score.text = score.DotFormatted();
		}

		public static void SetPlacement(int place)
		{
			Assets.placement.text = $"#{place}";
		}

		public static void SetArrow(int n)
		{
			var p = Assets.arrowTransform.anchoredPosition3D;
			Assets.arrowTransform.anchoredPosition3D = new Vector3(p.x, 17 * (n + 1), p.z);
		}

		public static void SetPlacements(params string[] placements)
		{
			Assets.placements.text = placements.Join(p => p.ToUpper(), "\n");
		}

		public static void SetScores(params int[] scores)
		{
			Assets.scores.text = scores.Join(n => n.DotFormatted(), "\n");
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