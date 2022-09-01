using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimionship
{
	public class IncidentWorker_StuffFromAbove : IncidentWorker
	{
		static readonly Dictionary<ThingDef, int> weightedThings = new()
		{
			{ ThingDefOf.Jade, 45 },
			{ ThingDefOf.Granite, 40 },
			{ ThingDefOf.Plasteel, 10 },
			{ ThingDefOf.Uranium, 5 },
		};

		public override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			return TryFindCell(out var _, map);
		}

		public override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (!TryFindCell(out var intVec, map))
				return false;

			_ = GenCollection.TryRandomElementByWeight(weightedThings.Keys, def => weightedThings[def], out var def);

			var list = new List<Building>();
			for (var i = 1; i <= 13; i++)
			{
				var stuff = ThingMaker.MakeThing(def, null);
				if (stuff is Building building)
				{
					building.canChangeTerrainOnDestroyed = false;
					list.Add(building);
				}
			}
			SendStandardLetter(Defs.StuffFromAbove.letterLabel, Defs.StuffFromAbove.letterText, LetterDefOf.ThreatBig, parms, new TargetInfo(intVec, map, false), Array.Empty<NamedArgument>());
			_ = SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, list, intVec, map);
			return true;
		}

		static bool TryFindCell(out IntVec3 cell, Map map)
		{
			var victim = map.mapPawns.FreeColonists.RandomElement();
			if (victim == null)
			{
				cell = IntVec3.Invalid;
				return false;
			}
			cell = victim.pather.nextCell;
			if (cell.IsValid == false)
				cell = victim.Position;
			return true;
		}
	}
}
