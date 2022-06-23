using System.Linq;
using Verse;

namespace Rimionship
{
	public class SacrifiesSpot : Building
	{
		public int created;

		public SacrifiesSpot() : base()
		{
			created = Find.TickManager.TicksGame;
		}

		public static SacrifiesSpot PortalForMap(Map map)
		{
			return map.listerThings.AllThings.OfType<SacrifiesSpot>().FirstOrDefault();
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = true;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref created, "created");
		}
	}
}
