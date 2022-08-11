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
			EndSuccess,
			EndFailure
		}

		public State state;
		public Pawn sacrifice;
		public Pawn sacrificer;

		public Sacrification(Map map) : base(map) { }

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref state, "state");
			Scribe_References.Look(ref sacrifice, "sacrifice");
			Scribe_References.Look(ref sacrificer, "sacrificer");
		}

		public void Start()
		{
			state = State.Gathering;
			map.InterruptAllColonistsOnMap();
		}
		public void MarkFailed()
		{
			state = State.EndFailure;
			map.InterruptAllColonistsOnMap(true);
		}

		public void MakeSuccess()
		{
			state = State.EndSuccess;
			map.InterruptAllColonistsOnMap(true);
		}

		public bool IsRunning() => state == State.Gathering || state == State.Executing;
		public bool IsNotRunning() => IsRunning() == false;
		public bool HasEnded() => state == State.EndSuccess || state == State.EndFailure;

		public override void MapComponentTick()
		{
			if (10.EveryNTick() == false)
				return;

			FixGameInitData();

			if (SacrificationSpot.ForMap(map) == null || BloodGod.Instance.IsInactive)
			{
				if (IsRunning())
					MarkFailed();
				else
					state = State.Idle;
			}

			// ritual ending
			if (state == State.EndSuccess || state == State.EndFailure)
			{
				if (state == State.EndSuccess)
				{
					var spot = SacrificationSpot.ForMap(map);
					var sacrification = map.GetComponent<Sacrification>();
					var bloodGod = Current.Game.World.GetComponent<BloodGod>();
					bloodGod.Satisfy(spot, sacrification);
				}

				state = State.Idle;

				sacrificer = null;
				sacrifice = null;
			}
		}

		void FixGameInitData()
		{
			if (map.mapPawns.ColonistCount > 0 && Find.GameInitData != null && Find.GameInitData.startingPawnCount != -1)
			{
				Find.GameInitData.startingAndOptionalPawns.Clear();
				Find.GameInitData.startingPawnCount = -1;
			}
		}
	}
}
