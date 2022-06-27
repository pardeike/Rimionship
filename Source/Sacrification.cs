using RimWorld;
using System.Linq;
using Verse;

namespace Rimionship
{
	public class Sacrification : MapComponent
	{
		public enum State
		{
			Idle,
			Gathering,
			Executing,
			Ending
		}

		public State state;
		public Pawn sacrifice;
		public Pawn sacrificer;

		public Sacrification(Map map) : base(map)
		{
		}

		public void Start()
		{
			var playerFaction = Faction.OfPlayer;
			sacrifice = map.mapPawns.AllPawnsSpawned
				.Where(pawn => pawn.factionInt == playerFaction
					&& pawn.RaceProps.Humanlike
					&& pawn.IsSlave == false
					&& pawn.IsPrisoner == false
					&& pawn.InMentalState == false
					&& pawn.Downed == false
				)
				.RandomElement();

			sacrificer = map.mapPawns.AllPawnsSpawned
				.Where(pawn => pawn.factionInt == playerFaction
					&& pawn.RaceProps.Humanlike
					&& pawn.IsSlave == false
					&& pawn.IsPrisoner == false
					&& pawn.InMentalState == false
					&& pawn.Downed == false

					&& pawn != sacrifice

				)
				.RandomElement();

			state = State.Gathering;
		}
	}
}
