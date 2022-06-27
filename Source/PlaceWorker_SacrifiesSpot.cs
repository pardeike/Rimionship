using Verse;

namespace Rimionship
{
	public class PlaceWorker_SacrificationSpot : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			if (SacrificationSpot.ForMap(map) != null)
				return AcceptanceReport.WasRejected;
			return AcceptanceReport.WasAccepted;
		}
	}
}
