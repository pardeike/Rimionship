using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class JobDriver_GettingSacrificed : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			var map = Map;

			if (TargetLocA.Standable(map) == false) return false;
			if (TargetLocA.IsForbidden(pawn)) return false;
			if (map.pawnDestinationReservationManager.IsReserved(TargetLocA)) return false;
			if (pawn.CanReserveAndReach(TargetLocA, PathEndMode.OnCell, Danger.Deadly) == false) return false;

			_ = map.reservationManager.Reserve(pawn, job, TargetLocA);

			return true;
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(() => Map.GetComponent<Sacrification>().MarkFailed(), TargetIndex.A, TargetIndex.B);
			AddEndCondition(() => Map.GetComponent<Sacrification>().HasEnded() ? JobCondition.Incompletable : JobCondition.Ongoing);

			yield return Toils_Goto.GotoCell(TargetLocA, PathEndMode.OnCell);
			yield return Toils_General.Do(() => Map.GetComponent<Sacrification>().state = Sacrification.State.Executing);
			yield return new Toil
			{
				tickAction = () => pawn.Rotation = Rot4.South,
				socialMode = RandomSocialMode.Quiet,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
		}
	}
}
