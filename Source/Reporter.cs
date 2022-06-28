using RimWorld.Planet;
using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Reporter : WorldComponent
	{
		// transient
		public readonly float updateInterval = 4;
		public float nextUpdate;

		private int _chosenMap = 0;
		public Map ChosenMap => Current.Game.maps[_chosenMap];

		public long wealth;
		public int mapCount;
		public int colonists;
		public int colonistsNeedTending;
		public int medicalConditions;
		public int enemies;
		public int wildAnimals;
		public int tamedAnimals;
		//public int slaves;
		public int visitors;
		public int prisoners;
		public int downedColonists;
		public int mentalColonists;
		public int rooms;
		public int caravans;
		public int weaponDps;
		public int electricity;
		public int medicine;
		public int food;
		public int fire;
		public int conditions;
		public int temperature;

		public Reporter(World world) : base(world)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref _chosenMap, "chosenMap");
			Scribe_Values.Look(ref wealth, "wealth");
			Scribe_Values.Look(ref colonists, "colonists");
			Scribe_Values.Look(ref colonistsNeedTending, "colonistsNeedTending");
			Scribe_Values.Look(ref medicalConditions, "medicalConditions");
			Scribe_Values.Look(ref enemies, "enemies");
			Scribe_Values.Look(ref wildAnimals, "wildAnimals");
			Scribe_Values.Look(ref tamedAnimals, "tamedAnimals");
			//Scribe_Values.Look(ref slaves, "slaves");
			Scribe_Values.Look(ref visitors, "visitors");
			Scribe_Values.Look(ref prisoners, "prisoners");
			Scribe_Values.Look(ref downedColonists, "downedColonists");
			Scribe_Values.Look(ref mentalColonists, "mentalColonists");
			Scribe_Values.Look(ref rooms, "rooms");
			Scribe_Values.Look(ref caravans, "caravans");
			Scribe_Values.Look(ref weaponDps, "weaponDps");
			Scribe_Values.Look(ref electricity, "electricity");
			Scribe_Values.Look(ref medicine, "medicine");
			Scribe_Values.Look(ref fire, "fire");
			Scribe_Values.Look(ref conditions, "conditions");
			Scribe_Values.Look(ref temperature, "temperature");
		}

		public override void FinalizeInit()
		{
			_ = Find.CameraDriver.StartCoroutine(Coroutine());
		}

		private IEnumerator UpdateWeath()
		{
			try
			{
				wealth = Stats.ColonyWealth(ChosenMap);
				HUD.SetScore(wealth);
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMaps()
		{
			try
			{
				mapCount = Stats.MapCount();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateColonists()
		{
			try
			{
				colonists = Stats.AllColonists();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateColonistsNeedTending()
		{
			try
			{
				colonistsNeedTending = Stats.ColonistsNeedTending();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMedicalConditionsCount()
		{
			try
			{
				medicalConditions = Stats.MedicalConditionsCount();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateEnemies()
		{
			try
			{
				enemies = Stats.AllEnemies();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateWildAnimals()
		{
			try
			{
				wildAnimals = Stats.AllWildAnimals();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateTamedAnimals()
		{
			try
			{
				tamedAnimals = Stats.AllTamedAnimals();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		/*private IEnumerator UpdateSlaves()
		{
			try
			{
				slaves = Stats.AllSlaves();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}*/

		private IEnumerator UpdateVisitors()
		{
			try
			{
				visitors = Stats.AllVisitors();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdatePrisoners()
		{
			try
			{
				prisoners = Stats.AllPrisoners();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateDownedColonists()
		{
			try
			{
				downedColonists = Stats.AllDownedColonists();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMentalColonists()
		{
			try
			{
				mentalColonists = Stats.AllMentalColonists();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateRooms()
		{
			try
			{
				rooms = Stats.AllRooms();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateCaravans()
		{
			try
			{
				caravans = Stats.AllCaravans();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateWeaponDps()
		{
			var w1 = 0f;
			var w2 = 0f;
			var w3 = 0f;
			try
			{
				w1 = Stats.AllWeaponDps_1();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				w2 = Stats.AllWeaponDps_2();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				w3 = Stats.AllWeaponDps_3();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			weaponDps = (int)(w1 + w2 + w3);
			yield return null;
		}

		private IEnumerator UpdateElectricity()
		{
			try
			{
				electricity = Stats.AllElectricity();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMedicine()
		{
			var m1 = 0f;
			var m2 = 0f;
			try
			{
				m1 = Stats.AllMedicine_1();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				m2 = Stats.AllMedicine_2();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			medicine = (int)(m1 + m2);
			yield return null;
		}

		private IEnumerator UpdateFood()
		{
			var f1 = 0f;
			var f2 = 0f;
			try
			{
				f1 = Stats.AllFood_1();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				f2 = Stats.AllFood_2();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			food = (int)(f1 + f2);
			yield return null;
		}

		private IEnumerator UpdateFire()
		{
			try
			{
				fire = Stats.AllFire();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateConditions()
		{
			try
			{
				conditions = Stats.GameConditions();
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateTemperature()
		{
			try
			{
				temperature = Stats.Temperature(ChosenMap);
			}
			catch (Exception ex)
			{
				Log.Warning($"EX: {ex}");
			}
			yield return null;
		}

		public IEnumerator Coroutine()
		{
			while (Current.ProgramState != ProgramState.Playing)
				yield return null;

			while (Current.Game != null)
			{
				yield return UpdateWeath();
				yield return UpdateMaps();
				yield return UpdateColonists();
				yield return UpdateColonistsNeedTending();
				yield return UpdateMedicalConditionsCount();
				yield return UpdateEnemies();
				yield return UpdateWildAnimals();
				yield return UpdateTamedAnimals();
				// yield return UpdateSlaves();
				yield return UpdateVisitors();
				yield return UpdatePrisoners();
				yield return UpdateDownedColonists();
				yield return UpdateMentalColonists();
				yield return UpdateRooms();
				yield return UpdateCaravans();
				yield return UpdateWeaponDps();
				yield return UpdateElectricity();
				yield return UpdateMedicine();
				yield return UpdateFood();
				yield return UpdateFire();
				yield return UpdateConditions();
				yield return UpdateTemperature();

				while (Time.realtimeSinceStartup < nextUpdate)
					yield return null;
				nextUpdate = Time.realtimeSinceStartup + updateInterval;

				Log.Warning($"[{(int)nextUpdate}] ${wealth} maps:{mapCount} col:{colonists} tend:{colonistsNeedTending} med:{medicalConditions} wild:{wildAnimals} tame:{tamedAnimals} visitors:{visitors} prisoners:{prisoners} downed:{downedColonists} mental:{mentalColonists} rooms:{rooms} caravans:{caravans} dps:{weaponDps} el:{electricity} med:{medicine} food:{food} fire:{fire} cond:{conditions} temp:{temperature}");
				yield return null;
			}
		}
	}
}
