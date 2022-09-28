using Grpc.Core;
using RimWorld;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.Steam;

namespace Rimionship
{
	public static class Tools
	{
		static readonly System.Random _RND = new();

		public static bool assetsInited = false;
		public static readonly Rect Rect01 = new(0, 0, 1, 1);
		public static readonly bool DevMode = Configuration.UseDevMode;
		public static bool boomalopeManhunters = false;

		static Tools()
		{
			if (DevMode)
				Log.Warning($"Rimionship runs in dev mode");
		}

		public static string GetModVersionString()
		{
			var assembly = Assembly.GetAssembly(typeof(RimionshipMod));
			var t_attribute = typeof(AssemblyFileVersionAttribute);
			var attribute = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly, t_attribute, false);
			var str = attribute.Version;
			if (str.EndsWith(".0"))
				str = str.Substring(0, str.Length - 2);
			if (str.EndsWith(".0"))
				str = str.Substring(0, str.Length - 2);
			return str;
		}

		public static int ModsStillDownloading()
		{
			var rimionshipMods = PlayState.AllowedMods.Select(mod => new PublishedFileId_t(mod.Value)).ToHashSet();
			return WorkshopItems.subbedItems
				.OfType<WorkshopItem_NotInstalled>()
				.Where(item => rimionshipMods.Contains(item.PublishedFileId))
				.Count();
		}

		public static Texture2D LoadTexture(string path, bool makeReadonly = true)
		{
			var fullPath = Path.Combine(RimionshipMod.rootDir, "Textures", $"{path}.png");
			var data = File.ReadAllBytes(fullPath);
			if (data == null || data.Length == 0)
				throw new Exception($"Cannot read texture {fullPath}");
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
			if (tex.LoadImage(data) == false)
				throw new Exception($"Cannot create texture {fullPath}");
			tex.Compress(true);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = FilterMode.Trilinear;
			tex.Apply(true, makeReadonly);
			return tex;
		}

		public static bool BloodGodButton(Rect rect, string label)
		{
			var anchor = Text.Anchor;
			var color = GUI.color;
			var atlas = Assets.ButtonBGAtlas;
			if (Mouse.IsOver(rect))
			{
				atlas = Assets.ButtonBGAtlasOver;
				if (Input.GetMouseButton(0))
					atlas = Assets.ButtonBGAtlasClick;
			}
			Widgets.DrawAtlas(rect, atlas);
			var r = rect.ExpandedBy(-1);
			var s = rect.size;
			var f = r.size.x / r.size.y;
			if (s.x / s.y < f)
				s.x = s.y * f;
			Widgets.DrawTextureFitted(r, Assets.ButtonPattern, 1f, s, new Rect(0f, 0f, 1f, 1f));
			MouseoverSounds.DoRegion(rect);
			Text.Anchor = TextAnchor.MiddleCenter;
			var wordWrap = Text.WordWrap;
			if (rect.height < Text.LineHeight * 2f)
				Text.WordWrap = false;
			Widgets.Label(rect, label);
			Text.Anchor = anchor;
			GUI.color = color;
			Text.WordWrap = wordWrap;
			return Widgets.ButtonInvisible(rect, false);
		}

		static readonly Color disabledButtonLabelColor = Color.white.ToTransparent(0.35f);
		public static void DisabledButton(Rect rect, string label)
		{
			Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlas);
			Widgets.DrawBoxSolid(rect, Color.black.ToTransparent(0.25f));
			var r = rect.ExpandedBy(-1);
			var s = rect.size;
			var f = r.size.x / r.size.y;
			if (s.x / s.y < f)
				s.x = s.y * f;
			GUI.color = disabledButtonLabelColor;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, label);
			Text.Anchor = anchor;
			GUI.color = Color.white;
		}

		public static string GenerateHexString(int digits)
		{
			return string.Concat(Enumerable.Range(0, digits).Select(_ => _RND.Next(16).ToString("x")));
		}

		public static WaitUntil ToWaitUntil(this Task task) => new(() => task.IsCompleted);

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

		public static bool Between<T>(this T val, T lower, T upper) where T : IComparable
		{
			return val.CompareTo(lower) >= 0 && val.CompareTo(upper) <= 0;
		}

		public static string DotFormatted(this int nr, bool useSpace = false)
		{
			if (nr == 0)
				return useSpace ? " " : "";
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

		public static List<Map> PlayerMaps
		{
			get
			{
				var maps = Find.Maps ?? new List<Map>();
				return maps.Where(m => m.IsPlayerHome).ToList();
			}
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
				case StatusCode.Cancelled:
				case StatusCode.Unavailable:
				case StatusCode.PermissionDenied:
				case StatusCode.DeadlineExceeded:
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

		public static Rect OffsetBy(this Rect rect, float dx, float dy)
		{
			return new Rect(rect.position + new Vector2(dx, dy), rect.size);
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
						return JobCondition.None;
					if (thing == null || !thing.Spawned || thing.Map != f.GetActor().Map)
					{
						cleanupAction();
						return JobCondition.Incompletable;
					}
					return JobCondition.None;
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

		public static bool HasSimpleWeapon(this Pawn pawn)
		{
			return WorkGiver_HunterHunt.HasHuntingWeapon(pawn);
		}

		public static void InterruptAllColonistsOnMap(this Map map, bool onlyOurJobs = false)
		{
			var colonists = map.mapPawns.FreeColonistsSpawned.Where(pawn => pawn.CanParticipateInSacrificationRitual()).ToList();
			foreach (var pawn in colonists)
			{
				var jobDef = pawn.CurJobDef;
				if (onlyOurJobs == false || jobDef == Defs.SacrificeColonist || jobDef == Defs.GettingSacrificed || jobDef == Defs.WatchSacrification)
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
			}
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

		public static Toil WaitUntil(Func<bool> condition, int minTicks, TargetIndex face = TargetIndex.None)
		{
			var deadline = 0;
			var toil = new Toil
			{
				initAction = () => deadline = Find.TickManager.TicksGame + minTicks,
				defaultCompleteMode = ToilCompleteMode.Never,
				handlingFacing = true
			};
			toil.tickAction = delegate ()
			{
				if (Find.TickManager.TicksGame > deadline && condition())
					toil.actor.jobs.curDriver.ReadyForNextToil();
				if (face != TargetIndex.None)
					toil.actor.rotationTracker.FaceTarget(toil.actor.CurJob.GetTarget(face));
			};
			return toil;
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

		public static List<Building_Battery> AllBatteries()
		{
			return PlayerMaps
				.SelectMany(map => map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.Battery).OfType<Building_Battery>())
				.ToList();
		}

		public static List<PowerNet> AllPowerNets()
		{
			return PlayerMaps
				.SelectMany(map => map.powerNetManager.AllNetsListForReading)
				.ToList();
		}

		public static List<Pawn> Generate(this PawnKindDef pawnKindDef, int animalCount = 0)
		{
			var list = new List<Pawn>();
			for (int i = 0; i < animalCount; i++)
				list.Add(PawnGenerator.GeneratePawn(pawnKindDef));
			return list;
		}

		public static IEnumerable<IncidentDef> AllIncidentDefs() => DefDatabase<IncidentDef>.AllDefsListForReading;
	}

	public class GZipWebClient : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = (HttpWebRequest)base.GetWebRequest(address);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			return request;
		}
	}
}
