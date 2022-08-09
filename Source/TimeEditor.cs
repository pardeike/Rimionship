using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public interface ITimeUnit
	{
		public string Label { get; }
		public int TickUnit { get; }
	}

	public class Ticks : ITimeUnit
	{
		public string Label => "Ticks";
		public int TickUnit => 1;

		public static ITimeUnit Instance = new Ticks();
	}

	public class Hours : ITimeUnit
	{
		public string Label => "Hours";
		public int TickUnit => GenDate.TicksPerHour;

		public static ITimeUnit Instance = new Hours();
	}

	public class Days : ITimeUnit
	{
		public string Label => "Days";
		public int TickUnit => GenDate.TicksPerDay;

		public static ITimeUnit Instance = new Days();
	}

	public class Quadrums : ITimeUnit
	{
		public string Label => "Quadrums";
		public int TickUnit => GenDate.TicksPerQuadrum;

		public static ITimeUnit Instance = new Quadrums();
	}

	public static class TimeEditorExtension
	{
		static readonly Dictionary<string, ITimeUnit> chosenUnitCache = new();

		public static void TimeEditor(this Listing_Standard list, string label, int value, int fractions, ITimeUnit defaultUnit, Action<int> valueChanger)
		{
			var decimalFactor = Mathf.Pow(10, fractions);

			void UnitMenu(Rect rect, ITimeUnit currentUnit, Action<ITimeUnit> callback)
			{
				if (Widgets.ButtonText(rect, currentUnit.Label))
				{
					var options = new ITimeUnit[] { Ticks.Instance, Hours.Instance, Days.Instance, Quadrums.Instance }
						.Select(unit => new FloatMenuOption(unit.Label, () => { if (currentUnit != unit) callback(unit); }))
						.ToList();
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}

			var rect = list.GetRect(30f);
			var spacing = 8f;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			var labelWidth = label.GetWidthCached();
			Widgets.Label(rect.LeftPartPixels(labelWidth), label);

			var currentUnit = chosenUnitCache.GetValueOrDefault(label, defaultUnit);
			chosenUnitCache[label] = currentUnit;
			var unitsWidth = currentUnit.Label.GetWidthCached() + 2 * spacing;
			var textFieldWidth = rect.width - labelWidth - unitsWidth - 2 * spacing;

			var unitValue = Mathf.RoundToInt(value * decimalFactor / currentUnit.TickUnit) / decimalFactor;
			Text.Anchor = TextAnchor.MiddleRight;
			string textBuffer = null;
			Widgets.TextFieldNumeric(rect.RightPartPixels(textFieldWidth + spacing + unitsWidth).LeftPartPixels(textFieldWidth), ref unitValue, ref textBuffer);

			UnitMenu(rect.RightPartPixels(unitsWidth), currentUnit, newUnit =>
			{
				unitValue *= currentUnit.TickUnit / newUnit.TickUnit;
				currentUnit = newUnit;
				chosenUnitCache[label] = currentUnit;
			});

			var newValue = Mathf.RoundToInt(unitValue * currentUnit.TickUnit);
			if (newValue != value)
				valueChanger(newValue);

			Text.Anchor = TextAnchor.UpperLeft;
		}
	}
}
