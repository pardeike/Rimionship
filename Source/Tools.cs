using Grpc.Core;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimionship
{
	public static class Tools
	{
		static readonly System.Random _RND = new();

		public static bool assetsInited = false;
		public static readonly Rect Rect01 = new(0, 0, 1, 1);
		public static readonly bool DevMode;

		static Tools()
		{
			DevMode = Environment.GetEnvironmentVariable("RIMIONSHIP-DEV") != null;
			if (DevMode)
				Log.Warning($"Rimionship runs in dev mode");
		}

		public static string GenerateHexString(int digits)
		{
			return string.Concat(Enumerable.Range(0, digits).Select(_ => _RND.Next(16).ToString("x")));
		}

		static string uniqueModID;
		public static string UniqueModID
		{
			get
			{
				if (uniqueModID == null)
				{
					uniqueModID = PlayerPrefs.GetString("rimionship-id");
					if (uniqueModID.NullOrEmpty())
						uniqueModID = Guid.NewGuid().ToString();
					PlayerPrefs.SetString("rimionship-id", uniqueModID);
				}
				return uniqueModID;
			}
		}

		public static bool EveryNTick(this int ticks, int offset = 0)
		{
			return (Find.TickManager.TicksGame + offset) % ticks == 0;
		}

		public static string DotFormatted(this int nr)
		{
			if (nr == 0)
				return "";
			var parts = new List<string>();
			while (nr > 0)
			{
				var part = (ushort)(nr % 1000);
				parts.Insert(0, nr < 1000 ? part.ToString() : $"{part:D3}");
				nr /= 1000;
			}
			return string.Join(".", parts.ToArray());
		}

		public static string FileHash(string path)
		{
			if (File.Exists(path) == false)
				return "";
			using var md5 = MD5.Create();
			using var stream = File.OpenRead(path);
			var checksum = md5.ComputeHash(stream);
			return BitConverter.ToString(checksum).Replace("-", "").ToLower();
		}

		public static HashSet<string> InstalledMods()
		{
			return ModsConfig.data.activeMods.ToHashSet();
		}

		public static bool ShouldReport(this RpcException exception)
		{
			var detail = exception.Status.Detail;
			switch (exception.StatusCode)
			{
				case StatusCode.Unavailable:
				case StatusCode.PermissionDenied:
					return false;
				case StatusCode.Unknown:
					if (detail == "Stream removed")
						return false;
					break;
				case StatusCode.Internal:
					if (detail == "GOAWAY received")
						return false;
					break;
				default:
					break;
			}
			return true;
		}

		public static GUIStyle GUIStyle(this Font font, Color color, RectOffset padding = null)
		{
			return new GUIStyle()
			{
				font = font,
				alignment = TextAnchor.MiddleCenter,
				padding = padding ?? new RectOffset(0, 0, 0, 0),
				normal = new GUIStyleState() { textColor = color }
			};
		}

		public static GUIStyle Alignment(this GUIStyle style, TextAnchor anchor)
		{
			style.alignment = anchor;
			return style;
		}

		public static GUIStyle Wrapping(this GUIStyle style)
		{
			style.wordWrap = true;
			return style;
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

		public static bool CanParticipateInSacrificationRitual(this Pawn pawn)
		{
			return pawn.RaceProps.Humanlike &&
				pawn.MentalStateDef == null &&
				pawn.IsSlave == false &&
				pawn.Downed == false &&
				pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
		}

		public static void InterruptAllColonistsOnMap(this Map map)
		{
			map.mapPawns.FreeColonistsSpawned
				.DoIf(
					pawn => pawn.CanParticipateInSacrificationRitual(),
					pawn => pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true)
				);
			;
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

		public static bool AllowOverride = false;
		public static void GiveThought(this Pawn pawn, Pawn otherPawn, ThoughtDef thoughtDef, float powerFactor = 1f)
		{
			var thought = ThoughtMaker.MakeThought(thoughtDef, null);
			thought.SetForcedStage(0);
			thought.moodPowerFactor = powerFactor;
			AllowOverride = true;
			pawn.needs.mood.thoughts.memories.TryGainMemory(thought, otherPawn);
			AllowOverride = false;
		}

		public static IEnumerable<Pawn> HasLineOfSightTo(this IEnumerable<Pawn> pawns, IntVec3 point, Map map)
		{
			bool canBeSeenOver(IntVec3 p) => p.CanBeSeenOverFast(map);
			return pawns.Where(pawn => GenSight.PointsOnLineOfSight(pawn.Position, point).All(canBeSeenOver));
		}

		public static ConcurrentDictionary<SoundDef, DelayedAction> PlayCallbacks = new();
		public static void PlayWithCallback(this SoundDef soundDef, float delay, Action callback)
		{
			PlayCallbacks[soundDef] = new DelayedAction() { action = callback, delay = delay };
			soundDef.PlayOneShotOnCamera();
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
				&& pawn.health.State == PawnHealthState.Mobile
				&& pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)
				&& pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
				&& ReachabilityUtility.CanReach(pawn, spot, PathEndMode.OnCell, Danger.Deadly);
		}

		public static bool CanBeSacrificed(this SacrificationSpot spot, Pawn pawn)
		{
			if (pawn.factionInt != Faction.OfPlayer)
				return false;
			if (pawn.RaceProps.Humanlike == false)
				return false;
			if (pawn.IsSlave)
				return false;
			if (pawn.IsPrisoner)
				return false;
			if (pawn.InMentalState)
				return false;
			if (pawn.Downed)
				return false;
			if (pawn.health.State != PawnHealthState.Mobile)
				return false;
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

		public static Texture2D ToAsset(this ModListStatus status)
		{
			return status switch
			{
				ModListStatus.Unknown => Assets.StateWait,
				ModListStatus.Invalid => Assets.StateError,
				ModListStatus.Valid => Assets.StateOK,
				_ => throw new NotImplementedException()
			};
		}

		public static IEnumerable<IncidentDef> AllIncidentDefs() => DefDatabase<IncidentDef>.AllDefsListForReading;
	}
}
