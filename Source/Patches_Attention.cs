using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Rimionship
{
	// attention: colonist downed
	//
	[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.MakeDowned))]
	class Pawn_HealthTracker_MakeDowned_Patch
	{
		public static void Postfix(Pawn ___pawn)
		{
			if (___pawn.IsColonist == false)
				return;
			ServerAPI.TriggerAttention("downed", Constants.Attention.colonistDowned);
		}
	}

	// attention: colonist killed
	//
	[HarmonyPatch(typeof(StatsRecord), nameof(StatsRecord.Notify_ColonistKilled))]
	public class StatsRecord_Notify_ColonistKilled_Patch
	{
		public static bool IgnoreKills = false;

		public static void Postfix()
		{
			if (IgnoreKills)
				return;
			ServerAPI.TriggerAttention("killed", Constants.Attention.colonistKilled);
		}
	}

	// attention: incidents
	//
	[HarmonyPatch(typeof(StoryState), nameof(StoryState.Notify_IncidentFired))]
	class StoryState_Notify_IncidentFired_Patch
	{
		static readonly Dictionary<IncidentCategoryDef, int> scores = new()
		{
			{ IncidentCategoryDefOf.Misc, Constants.Attention.incidentMisc },
			{ IncidentCategoryDefOf.ThreatSmall, Constants.Attention.threatSmall },
			{ IncidentCategoryDefOf.ThreatBig, Constants.Attention.threatBig },
			//{ IncidentCategoryDefOf.FactionArrival, Constants.Attention.factionArrival },
			//{ IncidentCategoryDefOf.OrbitalVisitor, Constants.Attention.orbitalVisitor },
			//{ IncidentCategoryDefOf.ShipChunkDrop, Constants.Attention.shipChunkDrop },
			{ IncidentCategoryDefOf.DiseaseHuman, Constants.Attention.diseaseHuman },
			{ IncidentCategoryDefOf.DiseaseAnimal, Constants.Attention.diseaseAnimal },
			{ IncidentCategoryDefOf.AllyAssistance, Constants.Attention.allyAssistance },
			{ IncidentCategoryDefOf.EndGame_ShipEscape, Constants.Attention.endGameShipEscape },
			{ IncidentCategoryDefOf.GiveQuest, Constants.Attention.giveQuest },
			{ IncidentCategoryDefOf.DeepDrillInfestation, Constants.Attention.deepDrillInfestation },
		};

		public static void Postfix(FiringIncident fi)
		{
			if (scores.TryGetValue(fi.def.category, out var score))
				ServerAPI.TriggerAttention("incident", score);
		}
	}

	// attention: combat
	//
	[HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.Notify_AttackedTarget))]
	class Pawn_MindState_Notify_AttackedTarget_Patch
	{
		public static void Postfix(Pawn ___pawn, LocalTargetInfo target)
		{
			if (___pawn.IsColonist == false)
				return;
			if (target.Thing is Pawn pawn && pawn.RaceProps.Humanlike)
				ServerAPI.TriggerAttention("human attack", Constants.Attention.attackedHuman);
			else
				ServerAPI.TriggerAttention("non human attack", Constants.Attention.attackedNonHuman);
		}
	}

	// attention: general damage
	//
	[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.PreApplyDamage))]
	class ThingWithComps_PreApplyDamage_Patch
	{
		public static void Postfix(ThingWithComps __instance, ref DamageInfo dinfo)
		{
			if (dinfo.Instigator is Pawn)
			{
				var score = (__instance.Faction?.IsPlayer ?? false)
					? Constants.Attention.damagePlayerThing
					: Constants.Attention.damageNonPlayerThing;
				ServerAPI.TriggerAttention("damage", score);
			}
		}
	}

	// attention: illness/pain
	//
	[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff))]
	[HarmonyPatch(new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
	class Pawn_HealthTracker_AddHediff_Patch
	{
		static readonly Color addiction = new(1, 0, 0.5f);
		static readonly Color luciferium = new(1, 1, 0.5f);
		static readonly Color simpleDesease = new(0.9f, 1, 0.35f);
		static readonly Color toxic = new(0.7f, 1, 0.7f);
		static readonly Color heartAttack = new(1, 0.2f, 0.2f);
		static readonly Color hypothermia = new(0.8f, 0.8f, 1);
		static readonly Color heatStroke_infection = new(0.8f, 0.8f, 0.35f);
		static readonly Color bodyPartMissing = new(0.5f, 0.5f, 0.5f);

		static readonly Dictionary<Color, int> scores = new()
		{
			{ hypothermia, Constants.Attention.hypothermia },
			{ simpleDesease, Constants.Attention.simpleDesease },
			{ addiction, Constants.Attention.addiction },
			{ toxic, Constants.Attention.toxic },
			{ heatStroke_infection, Constants.Attention.heatStroke_infection },
			{ bodyPartMissing, Constants.Attention.bodyPartMissing },
			{ heartAttack, Constants.Attention.heartAttack },
			{ luciferium, Constants.Attention.luciferium },
		};

		public static void Postfix(Pawn ___pawn, Hediff hediff)
		{
			if (___pawn.IsColonist == false)
				return;
			if (hediff.def == HediffDefOf.Burn)
			{
				ServerAPI.TriggerAttention("burn", Constants.Attention.burn);
				return;
			}
			if (hediff.def == HediffDefOf.BloodLoss)
			{
				ServerAPI.TriggerAttention("bleeding", Constants.Attention.bleeding);
				return;
			}
			// Log.Warning($"{___pawn.LabelShortCap} hediff {hediff.def.defName} {hediff.LabelColor}");
			if (scores.TryGetValue(hediff.LabelColor, out var score))
				ServerAPI.TriggerAttention("illness/pain", (int)(score * Constants.Attention.hediffMultiplier));
		}
	}

	// attention: general damage
	//
	[HarmonyPatch(typeof(GatheringWorker), nameof(GatheringWorker.TryExecute))]
	class GatheringWorker_TryExecute_Patch
	{
		public static void Postfix(GatheringWorker __instance, bool __result)
		{
			if (__result == false)
				return;
			var score = __instance is GatheringWorker_MarriageCeremony ? Constants.Attention.marriage : Constants.Attention.party;
			ServerAPI.TriggerAttention("gathering", score);
		}
	}

	// attention: fire on walls/stuff
	//
	[HarmonyPatch(typeof(FireUtility), nameof(FireUtility.TryStartFireIn))]
	class FireUtility_TryAttachFire_Patch
	{
		public static void Prefix(IntVec3 c, Map map)
		{
			var building = GridsUtility.GetEdifice(c, map);
			if (building == null || building.def.fillPercent < 0.55f)
				return;
			ServerAPI.TriggerAttention("structural fire", Constants.Attention.fire);
		}
	}

	// attention: explosions
	//
	[HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
	class GenExplosion_DoExplosion_Patch
	{
		public static void Postfix(Map map, DamageDef damType, Thing instigator, int damAmount)
		{
			if (instigator == null || map == null || damType.harmsHealth == false)
				return;
			var amount = Mathf.Max(damAmount, damType.defaultDamage);
			if (amount <= 0)
				return;
			ServerAPI.TriggerAttention("explosion", (int)(amount * Constants.Attention.explosionMultiplier));
		}
	}
}
