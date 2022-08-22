using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class Patches_Mods
	{
		public static void Patch(Harmony harmony)
		{
			void Transpiler(MethodInfo original, MethodInfo patch)
			{
				if (original == null)
					return;
				_ = harmony.Patch(original, transpiler: new HarmonyMethod(patch));
			}

			MethodInfo original, patch;

			// move heatmap display of mod 'Heat Map' down if necessary
			//
			original = AccessTools.Method("HeatMap.Main:UpdateOutdoorThermometer");
			patch = SymbolExtensions.GetMethodInfo(() => UpdateOutdoorThermometer_Transpiler(default));
			Transpiler(original, patch);

			// removes drawing artifact of more planning
			//
			original = AccessTools.Method("MorePlanning.Designators.AddDesignator:DrawMouseAttachments");
			patch = SymbolExtensions.GetMethodInfo(() => DrawMouseAttachments_Transpiler(default));
			Transpiler(original, patch);
			original = AccessTools.Method("MorePlanning.Designators.RemoveDesignator:DrawMouseAttachments");
			patch = SymbolExtensions.GetMethodInfo(() => DrawMouseAttachments_Transpiler(default));
			Transpiler(original, patch);
		}

		static void UpdateOutdoorThermometer_RectFixer(ref Rect rect)
		{
			if (Tools.assetsInited == false)
				return;

			var screenSize = new Vector2(UI.screenWidth, UI.screenHeight) * Prefs.UIScale;
			var rectTransform = Assets.scorePanel.GetComponent<RectTransform>();
			var position = rectTransform.position;
			var sizeDelta = rectTransform.sizeDelta;

			if (rect.xMax < screenSize.x - sizeDelta.x - 8)
				return;

			var top = sizeDelta.y - (position.y - screenSize.y) + 8;
			if (rect.y < top)
				rect.y = top;
		}

		static IEnumerable<CodeInstruction> UpdateOutdoorThermometer_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var rectVar = instructions.First(instr => instr.opcode == OpCodes.Ldloca || instr.opcode == OpCodes.Ldloca_S);
			var m_TutorSystem_AdaptiveTrainingEnabled = AccessTools.PropertyGetter(typeof(TutorSystem), nameof(TutorSystem.AdaptiveTrainingEnabled));
			foreach (var instr in instructions)
			{
				if (instr.Calls(m_TutorSystem_AdaptiveTrainingEnabled))
				{
					yield return rectVar;
					Rect r = default;
					yield return CodeInstruction.Call(() => UpdateOutdoorThermometer_RectFixer(ref r));
				}
				yield return instr;
			}
		}

		//

		static void DrawTextureFixer(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color)
		{
			var savedColor = GUI.color;
			GUI.color = color;
			Widgets.DrawTextureFitted(screenRect, texture, 1, new Vector2(texture.width, texture.height), sourceRect);
			_ = leftBorder;
			_ = rightBorder;
			_ = topBorder;
			_ = bottomBorder;
			GUI.color = savedColor;
		}

		static IEnumerable<CodeInstruction> DrawMouseAttachments_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var replacement = SymbolExtensions.GetMethodInfo(() => DrawTextureFixer(default, (Texture)null, default, 0, 0, 0, 0, Color.white));
			var m_Graphics_DrawTexture = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawTexture), replacement.GetParameters().Types());
			foreach (var instr in instructions)
			{
				if (instr.Calls(m_Graphics_DrawTexture))
					instr.operand = SymbolExtensions.GetMethodInfo(() => DrawTextureFixer(default, (Texture)null, default, 0, 0, 0, 0, Color.white));
				yield return instr;
			}
		}
	}
}
