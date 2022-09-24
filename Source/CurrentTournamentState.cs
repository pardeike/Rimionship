using RimWorld.Planet;
using Verse;

namespace Rimionship
{
	public class CurrentTournamentState : WorldComponent
	{
		public TournamentState state = (TournamentState)(-1);

		public CurrentTournamentState(World world) : base(world)
		{
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
				state = PlayState.tournamentState;

			base.ExposeData();
			Scribe_Values.Look(ref state, "state");
		}
	}
}
