using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class JobDriver_WatchSacrification : JobDriver
	{
		public IntVec3 watchSpot;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			var center = TargetThingB.Position;
			watchSpot = Tools.FindValidWatchPosition(pawn, center);
			return watchSpot.IsValid;
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			//this.EndOnDespawnedOrNull(() => EndJobWith(JobCondition.Incompletable), TargetIndex.A, TargetIndex.B, TargetIndex.C);
			//AddEndCondition(() => Map.GetComponent<Sacrification>().HasEnded() ? JobCondition.Incompletable : JobCondition.Ongoing);

			yield return Toils_Goto.GotoCell(watchSpot, PathEndMode.OnCell);
			yield return new Toil
			{
				tickAction = () => pawn.rotationTracker.FaceCell(TargetLocA),
				socialMode = RandomSocialMode.Normal,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
		}

		public override object[] TaleParameters()
		{
			return new object[]
			{
				pawn,
				TargetA.Pawn
			};
		}
	}
}
