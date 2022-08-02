using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimionship
{
	class Page_ConfigurePawns : Page_ConfigureStartingPawns
	{
		public override void PreOpen()
		{
			Current.Game.InitData = new GameInitData();
			Find.GameInitData.startingAndOptionalPawns = new List<Pawn>();
			Find.GameInitData.startingPawnCount = PlayState.startingPawnCount;

			for (int i = 0; i < Find.GameInitData.startingPawnCount; i++)
			{
				var pawn = StartingPawnUtility.NewGeneratedStartingPawn();
				Find.GameInitData.startingAndOptionalPawns.Add(pawn);
			}

			if (Find.CameraDriver.rootSize < 40)
				Find.CameraDriver.SetRootPosAndSize(Find.CameraDriver.rootPos, 40);

			base.PreOpen();
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
			Find.StoryWatcher.statsRecord.greatestPopulation = 0;

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
