using RimionshipServer.API;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimionship
{
	public class Reporter : WorldComponent
	{
		public static Reporter Instance => Current.Game.World.GetComponent<Reporter>();

		int _chosenMap = 0;
		public Map ChosenMap
		{
			get => Current.Game.maps[_chosenMap];
			set
			{
				_chosenMap = value.Index;
			}
		}

		Model_Stat stat = new();

		public Reporter(World world) : base(world)
		{
		}

		public void RefreshWealth()
		{
			var map = ChosenMap;
			if (map != null)
				stat.wealth = Stats.ColonyWealth(map);
		}

		public void NewAnimalMeat(int n) => stat.animalMeatCreated += n;
		public void NewBloodCleaned() => stat.amountBloodCleaned += 1;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref _chosenMap, "chosenMap");
			Scribe_Deep.Look(ref stat, "stat");
			stat ??= new();
		}

		public override void FinalizeInit()
		{
			_ = Find.CameraDriver.StartCoroutine(Coroutine());
		}

		public override void WorldComponentTick()
		{
			stat.ticksLowColonistMood += Stats.LowMoodColonists();
			var bloodGod = BloodGod.Instance;
			if (bloodGod.state != BloodGod.State.Idle && bloodGod.punishLevel > 1)
				stat.ticksIgnoringBloodGod += bloodGod.punishLevel - 1;
		}

		IEnumerator UpdateWeath()
		{
			try
			{
				if (Current.ProgramState == ProgramState.Playing)
				{
					var map = ChosenMap;
					if (map != null)
						stat.wealth = Stats.ColonyWealth(map);
				}
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateMaps()
		{
			try
			{
				stat.mapCount = Stats.AllMaps();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateColonists()
		{
			try
			{
				stat.colonists = Stats.AllColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateColonistsNeedTending()
		{
			try
			{
				stat.colonistsNeedTending = Stats.AllColonistsNeedTending();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateMedicalConditions()
		{
			try
			{
				stat.medicalConditions = Stats.AllMedicalConditions();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateEnemies()
		{
			try
			{
				stat.enemies = Stats.AllEnemies();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateWildAnimals()
		{
			try
			{
				stat.wildAnimals = Stats.AllWildAnimals();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateTamedAnimals()
		{
			try
			{
				stat.tamedAnimals = Stats.AllTamedAnimals();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateVisitors()
		{
			try
			{
				stat.visitors = Stats.AllVisitors();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdatePrisoners()
		{
			try
			{
				stat.prisoners = Stats.AllPrisoners();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateDownedColonists()
		{
			try
			{
				stat.downedColonists = Stats.AllDownedColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateMentalColonists()
		{
			try
			{
				stat.mentalColonists = Stats.AllMentalColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateRooms()
		{
			try
			{
				stat.rooms = Stats.AllRooms();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateCaravans()
		{
			try
			{
				stat.caravans = Stats.AllCaravans();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateWeaponDps()
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
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
			try
			{
				w2 = Stats.AllWeaponDps_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
			try
			{
				w3 = Stats.AllWeaponDps_3();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			stat.weaponDps = (int)(w1 + w2 + w3);
			yield return null;
		}

		IEnumerator UpdateElectricity()
		{
			try
			{
				stat.electricity = Stats.AllElectricity();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateMedicine()
		{
			var m1 = 0f;
			var m2 = 0f;
			try
			{
				m1 = Stats.AllMedicine_1();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
			try
			{
				m2 = Stats.AllMedicine_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			stat.medicine = (int)(m1 + m2);
			yield return null;
		}

		IEnumerator UpdateFood()
		{
			var f1 = 0f;
			var f2 = 0f;
			try
			{
				f1 = Stats.AllFood_1();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
			try
			{
				f2 = Stats.AllFood_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			stat.food = (int)(f1 + f2);
			yield return null;
		}

		IEnumerator UpdateFire()
		{
			try
			{
				stat.fire = Stats.AllFire();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateConditions()
		{
			try
			{
				stat.conditions = Stats.AllGameConditions();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateTemperature()
		{
			try
			{
				var map = ChosenMap;
				if (map != null)
					stat.temperature = Stats.Temperature(map);
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		IEnumerator UpdateStoryWatcherInfos()
		{
			try
			{
				(
					stat.numRaidsEnemy,
					stat.numThreatBigs,
					stat.colonistsKilled,
					stat.greatestPopulation,
					stat.inGameHours

				) = Stats.AllStoryInfos();
			}
			catch (Exception ex)
			{
				AsyncLogger.Error($"EX: {ex}");
			}
			yield return null;
		}

		public void HandleDamageTaken(Thing thing, float amount)
		{
			if (thing is Pawn pawn)
				stat.damageTakenPawns += amount;
			else
				stat.damageTakenThings += amount;
		}

		public void HandleDamageDealt(float amount)
		{
			stat.damageDealt += amount;
		}

		public IEnumerator Coroutine()
		{
			while (Current.ProgramState != ProgramState.Playing || PlayState.Valid == false)
				yield return null;

			static bool HasMap() => Current.Game != null && Find.Maps != null;

			while (Current.Game != null && Find.Maps != null)
			{
				if (HasMap())
					yield return UpdateWeath();
				if (HasMap())
					yield return UpdateMaps();
				if (HasMap())
					yield return UpdateColonists();
				if (HasMap())
					yield return UpdateColonistsNeedTending();
				if (HasMap())
					yield return UpdateMedicalConditions();
				if (HasMap())
					yield return UpdateEnemies();
				if (HasMap())
					yield return UpdateWildAnimals();
				if (HasMap())
					yield return UpdateTamedAnimals();
				if (HasMap())
					yield return UpdateVisitors();
				if (HasMap())
					yield return UpdatePrisoners();
				if (HasMap())
					yield return UpdateDownedColonists();
				if (HasMap())
					yield return UpdateMentalColonists();
				if (HasMap())
					yield return UpdateRooms();
				if (HasMap())
					yield return UpdateCaravans();
				if (HasMap())
					yield return UpdateWeaponDps();
				if (HasMap())
					yield return UpdateElectricity();
				if (HasMap())
					yield return UpdateMedicine();
				if (HasMap())
					yield return UpdateFood();
				if (HasMap())
					yield return UpdateFire();
				if (HasMap())
					yield return UpdateConditions();
				if (HasMap())
					yield return UpdateTemperature();
				if (HasMap())
					yield return UpdateStoryWatcherInfos();

				if (Scribe.mode == LoadSaveMode.Inactive && Current.ProgramState == ProgramState.Playing && Current_Notify_LoadedSceneChanged_Patch.notificationText == null)
					yield return ServerAPI.SendStat(stat).ToWaitUntil();

				while (ServerAPI.WaitUntilNextStatSend())
					yield return null;

				var incidents = Find.Storyteller?.incidentQueue.queuedIncidents ?? new List<RimWorld.QueuedIncident>();
				var events = incidents
						.Select(q => new FutureEvent()
						{
							Ticks = q.fireTick - Find.TickManager.TicksGame,
							Name = q.firingInc.def.defName,
							Quest = q.firingInc.sourceQuestPart?.quest.name ?? "",
							Faction = q.firingInc.parms.faction?.name ?? "",
							Points = q.firingInc.parms.points,
							Strategy = q.firingInc.parms.raidStrategy?.defName ?? "",
							ArrivalMode = q.firingInc.parms.raidArrivalMode?.defName ?? ""
						});
				yield return ServerAPI.SendFutureEvents(events).ToWaitUntil();
			}
		}
	}
}
