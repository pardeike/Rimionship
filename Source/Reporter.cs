using RimWorld.Planet;
using System;
using System.Collections;
using System.Linq;
using Verse;

namespace Rimionship
{
	public class Reporter : WorldComponent
	{
		private int _chosenMap = 0;
		public Map ChosenMap => Current.Game.maps[_chosenMap];

		private Model_Stat stat = new();

		public Reporter(World world) : base(world)
		{
		}

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

		private IEnumerator UpdateWeath()
		{
			try
			{
				stat.wealth = Stats.ColonyWealth(ChosenMap);
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMaps()
		{
			try
			{
				stat.mapCount = Stats.AllMaps();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateColonists()
		{
			try
			{
				stat.colonists = Stats.AllColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateColonistsNeedTending()
		{
			try
			{
				stat.colonistsNeedTending = Stats.AllColonistsNeedTending();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMedicalConditions()
		{
			try
			{
				stat.medicalConditions = Stats.AllMedicalConditions();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateEnemies()
		{
			try
			{
				stat.enemies = Stats.AllEnemies();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateWildAnimals()
		{
			try
			{
				stat.wildAnimals = Stats.AllWildAnimals();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateTamedAnimals()
		{
			try
			{
				stat.tamedAnimals = Stats.AllTamedAnimals();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateVisitors()
		{
			try
			{
				stat.visitors = Stats.AllVisitors();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdatePrisoners()
		{
			try
			{
				stat.prisoners = Stats.AllPrisoners();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateDownedColonists()
		{
			try
			{
				stat.downedColonists = Stats.AllDownedColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateMentalColonists()
		{
			try
			{
				stat.mentalColonists = Stats.AllMentalColonists();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateRooms()
		{
			try
			{
				stat.rooms = Stats.AllRooms();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateCaravans()
		{
			try
			{
				stat.caravans = Stats.AllCaravans();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
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
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				w2 = Stats.AllWeaponDps_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				w3 = Stats.AllWeaponDps_3();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			stat.weaponDps = (int)(w1 + w2 + w3);
			yield return null;
		}

		private IEnumerator UpdateElectricity()
		{
			try
			{
				stat.electricity = Stats.AllElectricity();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
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
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				m2 = Stats.AllMedicine_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			stat.medicine = (int)(m1 + m2);
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
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
			try
			{
				f2 = Stats.AllFood_2();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			stat.food = (int)(f1 + f2);
			yield return null;
		}

		private IEnumerator UpdateFire()
		{
			try
			{
				stat.fire = Stats.AllFire();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateConditions()
		{
			try
			{
				stat.conditions = Stats.AllGameConditions();
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateTemperature()
		{
			try
			{
				stat.temperature = Stats.Temperature(ChosenMap);
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"EX: {ex}");
			}
			yield return null;
		}

		private IEnumerator UpdateStoryWatcherInfos()
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
				AsyncLogger.Warning($"EX: {ex}");
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

			while (Current.Game != null && Find.Maps != null)
			{
				yield return UpdateWeath();
				yield return UpdateMaps();
				yield return UpdateColonists();
				yield return UpdateColonistsNeedTending();
				yield return UpdateMedicalConditions();
				yield return UpdateEnemies();
				yield return UpdateWildAnimals();
				yield return UpdateTamedAnimals();
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
				yield return UpdateStoryWatcherInfos();

				ServerAPI.SendStat(stat);
				yield return null;

				while (ServerAPI.WaitUntilNextStatSend())
					yield return null;

				if (Find.Storyteller.incidentQueue.queuedIncidents.Any())
				{
					var events = Find.Storyteller.incidentQueue.queuedIncidents
						.Select(q => new Api.FutureEvent()
						{
							Ticks = q.fireTick - Find.TickManager.TicksGame,
							Name = q.firingInc.def.defName,
							Quest = q.firingInc.sourceQuestPart?.quest.name ?? "",
							Faction = q.firingInc.parms.faction?.name ?? "",
							Points = q.firingInc.parms.points,
							Strategy = q.firingInc.parms.raidStrategy?.defName ?? "",
							ArrivalMode = q.firingInc.parms.raidArrivalMode?.defName ?? ""
						});
					ServerAPI.SendFutureEvents(events);
					yield return null;
				}
			}
		}
	}
}
