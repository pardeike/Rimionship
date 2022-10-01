using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimionship
{
	public class SacrificationSpot : Building
	{
		public int created;
		int HoursLeftToRemovable() => created + GenDate.TicksPerDay - Find.TickManager.TicksGame;

		// transient props
		GameObject effect;
		GameObject magic;
		Sustainer evilChoir;
		GameObject spotlightObject;
		Light spotlightLight;
		Light[] lights = System.Array.Empty<Light>();

		float intensity;
		float spotlightRamper;

		public SacrificationSpot() : base()
		{
			created = Find.TickManager.TicksGame;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref created, "created");
			Scribe_Values.Look(ref intensity, "intensity");
			Scribe_Values.Look(ref spotlightRamper, "spotlightRamper");
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);

			effect = Object.Instantiate(Assets.sacrificeEffects);
			effect.transform.position = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.SmallWire);

			magic = effect.transform.Find("Magic").gameObject;
			magic.SetActive(false);

			spotlightObject = effect.transform.Find("Base").Find("Spot Light").gameObject;
			spotlightLight = spotlightObject.GetComponent<Light>();
			spotlightObject.SetActive(false);

			lights = new Light[5];
			for (var i = 0; i < lights.Length; i++)
				lights[i] = effect.transform.Find($"Cylinder{i + 1}").Find("Point Light").GetComponent<Light>();

			var sacrification = Map.GetComponent<Sacrification>();
			if (sacrification != null && sacrification.state == Sacrification.State.Executing)
			{
				spotlightObject.SetActive(true);
				StartEvilChoir();
			}
		}

		public void Destroy()
		{
			Object.Destroy(effect);
			effect = null;
			magic = null;
			evilChoir = null;
			spotlightObject = null;
			spotlightLight = null;
			lights = System.Array.Empty<Light>();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			Destroy();
			base.DeSpawn(mode);
		}

		public void SetVisible(bool visible)
		{
			effect?.SetActive(visible);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			var ticks = HoursLeftToRemovable();
			yield return new Command_Action
			{
				defaultLabel = "Remove".Translate(),
				icon = Assets.RemoveSpot,
				disabled = ticks > 0,
				disabledReason = "WaitToRemove".Translate(ticks.ToStringTicksToPeriod(false, false, false, false)),
				defaultDesc = "RemoveDesc".Translate(),
				order = -20f,
				action = () =>
				{
					var saved = allowDestroyNonDestroyable;
					allowDestroyNonDestroyable = true;
					Destroy();
					allowDestroyNonDestroyable = saved;
				}
			};

			if (Map.GetComponent<Sacrification>().IsRunning())
				yield return new Command_Action
				{
					defaultLabel = "DesignatorCancel".Translate(),
					icon = Assets.CancelSpot,
					defaultDesc = "CancelDesc".Translate(),
					order = -19f,
					action = () => Map.GetComponent<Sacrification>().MarkFailed()
				};
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			var map = selPawn.Map;
			if (map.ReadyForSacrification(out var _, out var sacrification) == false)
				yield break;
			if (this.CanBeSacrificed(selPawn) == false)
				yield break;
			if (selPawn.CanReserveAndReach(Position, PathEndMode.OnCell, Danger.Deadly) == false)
				yield break;
			if (Position.InAllowedArea(selPawn) == false)
				yield break;

			void action()
			{
				var availableSacrifice = map.mapPawns.AllPawnsSpawned
					.Where(pawn => pawn != selPawn && this.CanSacrifice(pawn) && pawn.CanReserveAndReach(Position, PathEndMode.OnCell, Danger.Deadly))
					.ToList();

				sacrification.sacrificer = availableSacrifice.Any() ? availableSacrifice.RandomElement() : null;
				sacrification.sacrifice = selPawn;

				if (sacrification.sacrificer == null)
					Messages.Message("NobodyCanSacrifice".Translate(), MessageTypeDefOf.RejectInput, false);
				else
					sacrification.Start();
			}

			var disabled = BloodGod.Instance.IsInactive;
			yield return new FloatMenuOption(
				(disabled ? "CannotSacrificeMyself" : "SacrificeMyself").Translate(),
				disabled ? null : action,
				MenuOptionPriority.VeryLow
			);
		}

		void StartEvilChoir()
		{
			if (evilChoir != null)
				return;

			var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
			evilChoir = Defs.EvilChoir.TrySpawnSustainer(info);

			Find.MusicManagerPlay.disabled = true;
			Find.MusicManagerPlay.audioSource?.Stop();
		}

		void StopEvilChoir()
		{
			if (evilChoir == null)
				return;

			evilChoir = null;

			Find.MusicManagerPlay.disabled = false;
			if (Find.MusicManagerPlay.gameObjectCreated)
				Find.MusicManagerPlay.StartNewSong();
		}

		public override void Tick()
		{
			base.Tick();

			if (effect == null)
				return;

			var sacrification = Map.GetComponent<Sacrification>();
			if (sacrification == null)
				return;

			var magicOn = sacrification.IsRunning();
			if (magic.activeSelf != magicOn)
				magic.SetActive(magicOn);

			if (sacrification.state == Sacrification.State.Executing)
			{
				if (spotlightRamper == 0)
				{
					spotlightObject.SetActive(true);
					StartEvilChoir();
				}
				if (spotlightRamper < 1f)
					spotlightRamper += 0.01f;
				intensity = spotlightRamper * (10 + 10 * Mathf.Sin(GenTicks.TicksGame / 60f));
				spotlightLight.intensity = intensity;

				evilChoir?.Maintain();
			}
			else
			{
				if (spotlightRamper > 0)
				{
					spotlightRamper -= 0.01f;
					spotlightLight.intensity = intensity * spotlightRamper;
				}
				else
				{
					spotlightObject.SetActive(false);
					spotlightRamper = 0f;
					spotlightLight.intensity = 0f;
					StopEvilChoir();
				}
			}

			lights[Rand.Range(0, lights.Length)].intensity = Rand.Range(2f, 3f);
		}

		public static SacrificationSpot ForMap(Map map)
		{
			return map.listerBuildings.AllBuildingsColonistOfClass<SacrificationSpot>().FirstOrDefault();
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = true;
		}
	}
}
