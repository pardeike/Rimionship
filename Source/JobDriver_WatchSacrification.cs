using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class JobDriver_WatchSacrification : JobDriver
	{
		public IntVec3 watchSpot;

		public static IntVec3[][] watchPositions =
		{
			new []
			{
				new IntVec3(-2, 0, -1),
				new IntVec3(-2, 0, 0),
				new IntVec3(-2, 0, 1),
				new IntVec3(2, 0, -1),
				new IntVec3(2, 0, 0),
				new IntVec3(2, 0, 1),
				new IntVec3(-1, 0, -2),
				new IntVec3(0, 0, -2),
				new IntVec3(1, 0, -2),
				new IntVec3(-1, 0, 2),
				new IntVec3(0, 0, 2),
				new IntVec3(1, 0, 2),
			},
			new []
			{
				new IntVec3(-3, 0, -1),
				new IntVec3(-3, 0, 0),
				new IntVec3(-3, 0, 1),
				new IntVec3(3, 0, -1),
				new IntVec3(3, 0, 0),
				new IntVec3(3, 0, 1),
				new IntVec3(-1, 0, -3),
				new IntVec3(0, 0, -3),
				new IntVec3(1, 0, -3),
				new IntVec3(-1, 0, 3),
				new IntVec3(0, 0, 3),
				new IntVec3(1, 0, 3),
				new IntVec3(-2, 0, -2),
				new IntVec3(2, 0, -2),
				new IntVec3(-2, 0, 2),
				new IntVec3(2, 0, 2),
			},
			new []
			{
				new IntVec3(-4, 0, -1),
				new IntVec3(-4, 0, 0),
				new IntVec3(-4, 0, 1),
				new IntVec3(4, 0, -1),
				new IntVec3(4, 0, 0),
				new IntVec3(4, 0, 1),
				new IntVec3(-1, 0, -4),
				new IntVec3(0, 0, -4),
				new IntVec3(1, 0, -4),
				new IntVec3(-1, 0, 4),
				new IntVec3(0, 0, 4),
				new IntVec3(1, 0, 4),
				new IntVec3(-3, 0, -2),
				new IntVec3(-2, 0, -3),
				new IntVec3(3, 0, 2),
				new IntVec3(2, 0, 3),
				new IntVec3(-3, 0, 2),
				new IntVec3(-2, 0, 3),
				new IntVec3(3, 0, -2),
				new IntVec3(2, 0, -3),
			}
		};

		bool ValidWatchPosition(Map map, IntVec3 center, IntVec3 cell)
		{
			return cell.Standable(map)
					&& cell.IsForbidden(pawn) == false
					&& map.pawnDestinationReservationManager.IsReserved(cell) == false
					&& pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly)
					&& GenSight.LineOfSight(cell, center, map, true);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			var map = Map;
			var center = TargetThingB.Position;

			for (var i = 0; i < 3; i++)
			{
				watchSpot = watchPositions[i].Select(c => c + center)
					.Where(cell => ValidWatchPosition(map, center, cell))
					.RandomElementWithFallback(IntVec3.Invalid);

				if (watchSpot.IsValid)
				{
					_ = map.reservationManager.Reserve(pawn, job, watchSpot);
					break;
				}
			}

			return watchSpot.IsValid;
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(() => Map.GetComponent<Sacrification>().MarkFailed(), TargetIndex.A, TargetIndex.B, TargetIndex.C);
			AddEndCondition(() => Map.GetComponent<Sacrification>().HasEnded() ? JobCondition.Incompletable : JobCondition.Ongoing);

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
