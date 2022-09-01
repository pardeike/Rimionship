using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public class MentalState_KillingSpreeWorker : MentalStateWorker
	{
		public override bool StateCanOccur(Pawn pawn) => pawn == BloodGod.allowedKillingSpree;
	}

	public class MentalState_KillingSpree : MentalState
	{
		public enum State
		{
			idle,
			selecting,
			positioning,
			seeking,
			shooting,
			done
		}

		public Pawn target;
		public State state;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref target, "target");
			Scribe_Values.Look(ref state, "state", State.idle);
		}

		// public override string InspectLine => "KillingPrisoners".Translate();
		public override bool AllowRestingInBed => false;
		public override bool CanEndBeforeMaxDurationNow => false;
		public override bool ForceHostileTo(Thing t) => t is Pawn pawn && pawn.IsPrisonerOfColony && pawn.Downed == false;
		public override RandomSocialMode SocialModeMax() => RandomSocialMode.Off;
		void Do(JobDef job, LocalTargetInfo what) => pawn.jobs.StartJob(JobMaker.MakeJob(job, what), JobCondition.InterruptForced);

		public override void PostStart(string reason)
		{
			base.PostStart(reason);
			Do(JobDefOf.Wait, pawn.Position);
			state = State.selecting;
		}

		bool ValidTarget(Pawn other) => other != null
			&& other.Spawned
			&& other.Dead == false
			&& other.Downed == false
			&& other.IsPrisonerOfColony
			&& pawn.CanReach(other, PathEndMode.Touch, Danger.Deadly);

		public override void MentalStateTick()
		{
			if (Find.TickManager.TicksGame % 2 == 0)
				return;

			if (pawn.stances.stunner.Stunned || pawn.Downed)
				return;

			Verb verb = null;
			if (target != null && ValidTarget(target) == false)
				target = null;

			if (target == null)
			{
				if (state != State.done)
					state = State.selecting;
			}
			else
			{
				verb = pawn.TryGetAttackVerb(target, false);
				if (verb == null)
					state = State.done;
			}

			var oldState = state;
			switch (state)
			{
				case State.idle:
					break;

				case State.selecting:
					var targets = pawn.Map.mapPawns.PrisonersOfColonySpawned
						.Where(pawn => pawn.Downed == false)
						.ToList();
					target = targets.OrderBy(t => (t.DrawPos - pawn.DrawPos).MagnitudeHorizontalSquared()).FirstOrDefault();
					state = target == null ? State.done : State.positioning;
					break;

				case State.positioning:
					if (verb.CanHitTarget(target))
						state = State.shooting;
					else
					{
						Do(JobDefOf.Goto, target.Position);
						state = State.seeking;
					}
					break;

				case State.seeking:
					if (GenSight.LineOfSightToThing(pawn.Position, target, pawn.Map) && verb.CanHitTarget(target))
					{
						if (pawn.TryStartAttack(target))
							state = State.shooting;
						else
							state = State.positioning;
					}
					else if (pawn.pather.AtDestinationPosition() || pawn.CurJob == null || pawn.CurJob.def == JobDefOf.Wait)
						state = State.positioning;
					break;

				case State.shooting:
					if (verb.CanHitTarget(target))
						_ = pawn.TryStartAttack(target);
					else
					{
						Do(JobDefOf.Wait, pawn.Position);
						state = State.seeking;
					}
					break;

				case State.done:
					RecoverFromState();
					pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
					state = State.idle;
					break;
			}
			//if (oldState != state)
			//	Log.Warning($"### {oldState} -> {state}");
		}
	}
}
