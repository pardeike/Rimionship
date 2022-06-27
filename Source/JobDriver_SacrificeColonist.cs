using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimionship
{
	public class JobDriver_SacrificeColonist : JobDriver
	{
		int speechCount, meditationCount;
		MoteBubble speach, skull;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		void ExecuteByRemovingHead(Pawn victim)
		{
			var num = Mathf.Max(GenMath.RoundRandom(victim.BodySize * 8f), 1);
			for (int i = 0; i < num; i++)
				victim.health.DropBloodFilth();

			var head = victim.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Head);

			var n = Mathf.Clamp((int)victim.health.hediffSet.GetPartHealth(head) - 1, 1, 20);
			DamageInfo damageInfo = new DamageInfo(DamageDefOf.ExecutionCut, n, 999f, -1f, pawn, head, null, DamageInfo.SourceCategory.ThingOrUnknown, null, false, true);
			_ = victim.TakeDamage(damageInfo);

			var hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
			hediff_MissingPart.lastInjury = HediffDefOf.Cut;
			hediff_MissingPart.Part = head;
			hediff_MissingPart.IsFresh = false;
			victim.health.AddHediff(hediff_MissingPart, head, damageInfo, null);

			if (!victim.Dead)
				victim.Kill(new DamageInfo?(damageInfo), null);

			SoundDefOf.Execute_Cut.PlayOneShot(victim);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(() => Map.GetComponent<Sacrification>().state = Sacrification.State.Ending, TargetIndex.A, TargetIndex.B);

			yield return Toils_Goto.GotoCell(TargetThingA.Position + new IntVec3(1, 0, 0), PathEndMode.OnCell);
			yield return new Toil
			{
				initAction = () => meditationCount = 0,
				tickAction = () =>
				{
					pawn.Rotation = Rot4.North;
					if (pawn.IsHashIntervalTick(100) == false) return;
					FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating);
					if (++meditationCount == 4)
						ReadyForNextToil();
				},
				socialMode = RandomSocialMode.Quiet,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
			yield return Toils_General.Wait(30);
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
			yield return new Toil
			{
				initAction = () => speechCount = 0,
				tickAction = () =>
				{
					pawn.Rotation = Rot4.South;
					if (pawn.IsHashIntervalTick(100) == false) return;
					speechCount++;
					if (speechCount == 7)
					{
						pawn.rotationTracker.FaceTarget(TargetThingA);
						ReadyForNextToil();
						return;
					}
					if (speechCount % 2 == 1)
						speach = MoteMaker.MakeSpeechBubble(pawn, new[] { Assets.Blood, Assets.Skull, Assets.Insult }[speechCount / 2]);
					else
					{
						speach.Destroy(DestroyMode.Vanish);
						speach = null;
					}
				},
				socialMode = RandomSocialMode.Quiet,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
			yield return Toils_General.Wait(30);
			yield return Toils_General.Do(() => skull = MoteMaker.MakeSpeechBubble(pawn, Assets.Blood));
			yield return Toils_General.Wait(30);
			yield return Toils_General.Do(() =>
			{
				Defs.Thunder.PlayOneShot(SoundInfo.InMap(TargetThingB));
				skull.Destroy(DestroyMode.Vanish);
				skull = null;
				FleckMaker.ThrowMetaIcon(TargetThingB.Position, TargetThingB.Map, FleckDefOf.PsycastAreaEffect);
				ExecuteByRemovingHead(TargetA.Pawn);
				ThoughtUtility.GiveThoughtsForPawnExecuted(TargetA.Pawn, pawn, PawnExecutionKind.GenericBrutal);
				Map.GetComponent<Sacrification>().state = Sacrification.State.Ending;
			});
		}
	}
}
