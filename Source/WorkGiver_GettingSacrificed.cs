using RimWorld;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class WorkGiver_GettingSacrificed : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			var map = pawn.Map;
			var sacrification = map.GetComponent<Sacrification>();

			if (sacrification.state != Sacrification.State.Gathering) return null;
			if (pawn != sacrification.sacrifice) return null;

			var spot = SacrificationSpot.ForMap(map);
			if (spot == null) return null;

			return JobMaker.MakeJob(Defs.GettingSacrificed, spot, sacrification.sacrificer);
		}
	}
}
