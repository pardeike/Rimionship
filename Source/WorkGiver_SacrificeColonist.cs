using RimWorld;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class WorkGiver_SacrificeColonist : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			var map = pawn.Map;
			var sacrification = map.GetComponent<Sacrification>();
			if (pawn != sacrification.sacrificer) return null;

			if (sacrification.state != Sacrification.State.Executing) return null;

			var spot = SacrificationSpot.ForMap(map);
			if (spot == null) return null;

			return JobMaker.MakeJob(Defs.SacrificeColonist, sacrification.sacrifice, spot);
		}
	}
}
