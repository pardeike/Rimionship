using RimWorld;
using System.Linq;
using Verse;

namespace Rimionship
{
	public static class Stats
	{
		public static void ResetAll()
		{
			Current.Game.playLog = new PlayLog();
			Current.Game.letterStack = new LetterStack();
			Current.Game.history = new History();
			Current.Game.playLog = new PlayLog();
			Current.Game.battleLog = new BattleLog();
			Current.Game.autosaver = new Autosaver();
			WealthWatcher.ResetStaticData();
		}

		public static int ColonyWealth(this Map map)
		{
			if (map == null) return 0;
			map.wealthWatcher.ForceRecount();
			return (int)map.wealthWatcher.WealthTotal;
		}

		public static int AllMaps()
		{
			return Find.Maps.Count(map => map.IsPlayerHome);
		}

		public static int AllColonists()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers.Count;
		}

		public static int AllColonistsNeedTending()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers
				.Count(pawn => pawn.health.HasHediffsNeedingTendByPlayer(true));
		}

		public static int AllMedicalConditions()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers
				.Sum(pawn =>
				{
					var immunity = pawn.health.immunity;
					var hediffs = pawn.health.hediffSet.hediffs;
					var count = 0;
					for (var i = 0; i < hediffs.Count; i++)
					{
						var immunityRecord = immunity.GetImmunityRecord(hediffs[i].def);
						if (immunityRecord != null && immunityRecord.immunity < 1)
							count++;
					}
					return count;
				});
		}

		public static int AllEnemies()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map => map.attackTargetsCache
					.TargetsHostileToFaction(Faction.OfPlayer)
					.Select(target => target.Thing)
					.OfType<Pawn>()
					.Where(pawn => pawn.RaceProps.intelligence > Intelligence.Animal)
					.Count());
		}

		public static int AllWildAnimals()
		{
			return Find.Maps
				.SelectMany(map => map.mapPawns.AllPawnsSpawned)
				.Where(pawn => pawn.training == null)
				.Select(pawn => pawn.RaceProps)
				.Count(raceProp =>
					raceProp.intelligence == Intelligence.Animal &&
					raceProp.FleshType != FleshTypeDefOf.Mechanoid
				);
		}

		public static int AllTamedAnimals()
		{
			return Find.Maps
				.SelectMany(map => map.mapPawns.AllPawnsSpawned)
				.Where(pawn => pawn.training != null)
				.Select(pawn => pawn.RaceProps)
				.Count(raceProp =>
					raceProp.intelligence == Intelligence.Animal &&
					raceProp.FleshType != FleshTypeDefOf.Mechanoid
				);
		}

		/*public static int AllSlaves()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_SlavesOfColony.Count;
		}*/

		public static int AllVisitors()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map => map.mapPawns.AllPawnsSpawned
					.Except(map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer))
					.Except(PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists)
					.Select(target => target.Thing)
					.OfType<Pawn>()
					.Where(pawn => pawn.RaceProps.intelligence > Intelligence.Animal)
					.Count());
		}

		public static int AllPrisoners()
		{
			return PawnsFinder.AllMaps_PrisonersOfColonySpawned.Count;
		}

		public static int AllDownedColonists()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers
				.Count(pawn => pawn.Downed);
		}

		public static int AllMentalColonists()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers
				.Count(pawn => pawn.InMentalState);
		}

		public static int AllRooms()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map => map.listerBuildings.allBuildingsColonist.Count);
		}

		public static int AllCaravans()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Count;
		}

		public static float AllWeaponDps_1()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map =>
					map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
						.OfType<ThingWithComps>()
						.Where(thing =>
							thing.def.IsRangedWeapon
							&& (thing.GetComp<CompForbiddable>()?.forbiddenInt ?? false) == false
							&& (thing.TryGetComp<CompBiocodable>()?.Biocoded ?? false) == false
						)
						.Sum(thing => thing.RangedDPSAverage() / 2)
				);
		}

		public static float AllWeaponDps_2()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists
				.Select(pawn => pawn.equipment.Primary)
				.OfType<ThingWithComps>()
				.Where(thing => thing.def.IsRangedWeapon)
				.Sum(thing => thing.RangedDPSAverage());
		}

		public static float AllWeaponDps_3()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists
				.Select(pawn => pawn.equipment.Primary)
				.OfType<Thing>()
				.Where(thing => thing.def.IsRangedWeapon == false)
				.Sum(thing => thing.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS));
		}

		public static int AllElectricity()
		{
			return (int)Find.Maps
				.Where(map => map.IsPlayerHome)
				.SelectMany(map => map.powerNetManager.AllNetsListForReading)
				.Sum(net => net.CurrentEnergyGainRate() * 60000 + net.CurrentStoredEnergy());
		}

		public static float AllMedicine_1()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map =>
					map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine)
						.OfType<ThingWithComps>()
						.Sum(med => med.GetStatValue(StatDefOf.MedicalPotency) * med.stackCount * 5)
				);
		}

		public static float AllMedicine_2()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists
				.SelectMany(pawn => pawn.inventory.innerContainer.InnerListForReading)
				.Where(thing => thing.def.IsMedicine)
				.Sum(med => med.GetStatValue(StatDefOf.MedicalPotency) * med.stackCount * 10);
		}

		public static float AllFood_1()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map =>
					map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource)
						.OfType<ThingWithComps>()
						.Sum(food => food.GetStatValue(StatDefOf.Nutrition) * food.stackCount / 0.9f)
				);
		}

		public static float AllFood_2()
		{
			return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists
				.SelectMany(pawn => pawn.inventory.innerContainer.InnerListForReading)
				.Sum(food => food.GetStatValue(StatDefOf.Nutrition) * food.stackCount / 0.9f);
		}

		public static int AllFire()
		{
			var total = 0;
			var maps = Find.Maps.Where(map => map.IsPlayerHome).ToArray();
			for (var i = 0; i < maps.Length; i++)
			{
				var home = maps[i].areaManager.Home;
				total += maps[i].listerThings.ThingsOfDef(ThingDefOf.Fire).Count(fire => home[fire.Position]);
			}
			return total;
		}

		public static int AllGameConditions()
		{
			return Find.Maps
				.Where(map => map.IsPlayerHome)
				.Sum(map => map.gameConditionManager.ActiveConditions.Count);
		}

		public static int Temperature(this Map map)
		{
			if (map == null) return 0;
			return (int)map.mapTemperature.OutdoorTemp;
		}

		public static (int, int, int, int, int) AllStoryInfos()
		{
			return (
				Find.StoryWatcher.statsRecord.numRaidsEnemy,
				Find.StoryWatcher.statsRecord.numThreatBigs,
				Find.StoryWatcher.statsRecord.colonistsKilled,
				Find.StoryWatcher.statsRecord.greatestPopulation,
				GenDate.TicksGame / GenDate.TicksPerHour
			);
		}
	}
}
