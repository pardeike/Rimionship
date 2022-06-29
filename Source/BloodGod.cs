using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public class BloodGod : WorldComponent
	{
		static readonly Color scaleBG = new Color(1, 1, 1, 0.25f);
		static readonly Color scaleFG = new Color(227f / 255f, 38f / 255f, 38f / 255f);

		public BloodGod(World world) : base(world)
		{
		}

		public static void Draw(float leftX, ref float curBaseY)
		{
			var n = 4;
			var f = 1f;

			var left = leftX + 18;
			var top = curBaseY - 7;

			GUI.DrawTexture(new Rect(left, top - 24, 26, 24), Assets.Pentas[n]);
			Widgets.DrawBoxSolid(new Rect(left + 22, top - 10, 103, 3), scaleBG);
			Widgets.DrawBoxSolid(new Rect(left + 23, top - 11, f * 103, 3), scaleFG);

			curBaseY -= 24;
		}
	}
}
