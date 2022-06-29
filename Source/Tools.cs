using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimionship
{
	public static class Tools
	{
		public static bool EveryNTick(this int ticks, int offset = 0)
		{
			return (Find.TickManager.TicksGame + offset) % ticks == 0;
		}

		public static string DotFormatted(this int nr)
		{
			if (nr == 0) return "0";
			var parts = new List<string>();
			while (nr > 0)
			{
				var part = (ushort)(nr % 1000);
				parts.Insert(0, nr < 1000 ? part.ToString() : $"{part:D3}");
				nr /= 1000;
			}
			return string.Join(".", parts.ToArray());
		}

		public static void EndOnDespawnedOrNull<T>(this T f, Action cleanupAction, params TargetIndex[] indices) where T : IJobEndable
		{
			foreach (var ind in indices)
				f.AddEndCondition(delegate
				{
					var target = f.GetActor().jobs.curJob.GetTarget(ind);
					var thing = target.Thing;
					if (thing == null && target.IsValid)
					{
						return JobCondition.Ongoing;
					}
					if (thing == null || !thing.Spawned || thing.Map != f.GetActor().Map)
					{
						cleanupAction();
						return JobCondition.Incompletable;
					}
					return JobCondition.Ongoing;
				});
		}

		public static bool ReadyForSacrification(this Map map, out SacrificationSpot spot, out Sacrification sacrification)
		{
			sacrification = map.GetComponent<Sacrification>();
			if (sacrification == null || sacrification.state != Sacrification.State.Idle)
			{
				spot = null;
				return false;
			}
			spot = SacrificationSpot.ForMap(map);
			return spot != null;
		}

		public static bool CanSacrifice(this SacrificationSpot spot, Pawn pawn)
		{
			return pawn.factionInt == Faction.OfPlayer
				&& pawn.RaceProps.Humanlike
				&& pawn.IsSlave == false
				&& pawn.IsPrisoner == false
				&& pawn.InMentalState == false
				&& pawn.Downed == false
				&& pawn.WorkTagIsDisabled(WorkTags.Violent) == false
				&& pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)
				&& pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
				&& pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)
				&& ReachabilityUtility.CanReach(pawn, spot, PathEndMode.OnCell, Danger.Deadly);
		}

		public static bool CanBeSacrificed(this SacrificationSpot spot, Pawn pawn)
		{
			if (pawn.factionInt != Faction.OfPlayer) return false;
			if (pawn.RaceProps.Humanlike == false) return false;
			if (pawn.IsSlave) return false;
			if (pawn.IsPrisoner) return false;
			if (pawn.InMentalState) return false;
			if (pawn.Downed) return false;
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) == false) return false;
			return ReachabilityUtility.CanReach(pawn, spot, PathEndMode.OnCell, Danger.Deadly);
		}

		public static float RangedSpeed(ThingWithComps weapon, VerbProperties atkProps)
		{
			return atkProps.warmupTime
				+ weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown)
				+ (atkProps.burstShotCount - 1) * atkProps.ticksBetweenBurstShots / 60f;
		}

		public static float RangedDPSAverage(this ThingWithComps weapon)
		{
			var atkProps = (weapon.GetComp<CompEquippable>()).PrimaryVerb.verbProps;
			var damage = atkProps.defaultProjectile == null ? 0 : atkProps.defaultProjectile.projectile.GetDamageAmount(weapon);
			return (damage * atkProps.burstShotCount) / RangedSpeed(weapon, atkProps);
		}
	}
}
