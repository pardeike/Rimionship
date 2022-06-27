using RimWorld;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class WorkGiver_WatchSacrification : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			var map = pawn.Map;
			var sacrification = map.GetComponent<Sacrification>();
			if (sacrification.state == Sacrification.State.Idle) return null;
			if (sacrification.state == Sacrification.State.Ending) return null;
			if (pawn == sacrification.sacrifice || pawn == sacrification.sacrificer) return null;

			var spot = SacrificationSpot.ForMap(map);
			if (spot == null) return null;

			return JobMaker.MakeJob(Defs.WatchSacrification, sacrification.sacrifice, spot, sacrification.sacrificer);
		}
	}
}
