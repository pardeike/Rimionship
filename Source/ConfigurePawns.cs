using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimionship
{
	class ConfigurePawns : Page_ConfigureStartingPawns
	{
		bool devMode;

		public override void PreOpen()
		{
			Current.Game.InitData = new GameInitData();
			Find.GameInitData.startingAndOptionalPawns = new List<Pawn>();
			Find.GameInitData.startingPawnCount = 3;

			for (int i = 0; i < Find.GameInitData.startingPawnCount; i++)
			{
				var pawn = StartingPawnUtility.NewGeneratedStartingPawn();
				Find.GameInitData.startingAndOptionalPawns.Add(pawn);
			}

			devMode = Prefs.DevMode;
			Prefs.DevMode = false;

			base.PreOpen();
		}

		public override void PostClose()
		{
			base.PostClose();
			Prefs.DevMode = devMode;
		}

		public override void DoNext()
		{
			var map = Find.CurrentMap;
			foreach (var pawn in Find.GameInitData.startingAndOptionalPawns)
			{
				if (pawn.Name is NameTriple nameTriple && string.IsNullOrEmpty(nameTriple.Nick))
					pawn.Name = new NameTriple(nameTriple.First, nameTriple.First, nameTriple.Last);
			}

			MapGenerator.PlayerStartSpot = Find.CameraDriver.CurrentViewRect.CenterCell;
			var pods = Find.GameInitData.startingAndOptionalPawns.Select(pawn => new List<Thing>() { pawn }).ToList();
			DropPodUtility.DropThingGroupsNear(MapGenerator.PlayerStartSpot, map, pods, 110, false, false, true, true, false, false);

			Close(true);
		}

		public override bool CanDoBack() => true;

		public override void DoBack()
		{
			Close(true);
			GenScene.GoToMainMenu();
		}

	}
}
