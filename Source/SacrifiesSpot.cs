using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimionship
{
	public class SacrificationSpot : Building
	{
		public int created;
		private GameObject effect;

		private GameObject magic;
		private Sustainer evilChoir;

		private GameObject spotlightObject;
		private Light spotlightLight;
		private float intensity = 0f;
		private float spotlightRamper = 0f;

		private Light[] lights = System.Array.Empty<Light>();

		public SacrificationSpot() : base()
		{
			created = Find.TickManager.TicksGame;
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
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			Object.Destroy(effect);
			effect = null;
			lights = System.Array.Empty<Light>();

			base.DeSpawn(mode);
		}

		public override void Draw()
		{
			// nothing to draw
		}

		public override void Tick()
		{
			base.Tick();
			if (effect == null) return;

			var sacrification = Map.GetComponent<Sacrification>();
			if (sacrification == null) return;

			var magicOn = sacrification.state == Sacrification.State.Gathering || sacrification.state == Sacrification.State.Executing;
			if (magic.activeSelf != magicOn)
				magic.SetActive(magicOn);

			if (sacrification.state == Sacrification.State.Executing)
			{
				if (spotlightRamper == 0)
				{
					spotlightObject.SetActive(true);
					if (evilChoir == null)
					{
						var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
						evilChoir = Defs.EvilChoir.TrySpawnSustainer(info);
					}
				}
				if (spotlightRamper < 1f) spotlightRamper += 0.01f;
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
					evilChoir = null;
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

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref created, "created");
		}
	}
}
