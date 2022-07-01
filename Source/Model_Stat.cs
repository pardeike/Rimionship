using Api;
using Verse;

namespace Rimionship
{
	public class Model_Stat : IExposable
	{
		public int wealth;
		public int mapCount;
		public int colonists;
		public int colonistsNeedTending;
		public int medicalConditions;
		public int enemies;
		public int wildAnimals;
		public int tamedAnimals;
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
		public int numRaidsEnemy;
		public int numThreatBigs;
		public int colonistsKilled;
		public int greatestPopulation;
		public int inGameHours;
		public float damageTakenPawns;
		public float damageTakenThings;
		public float damageDealt;

		public Model_Stat() { }

		public void ExposeData()
		{
			Scribe_Values.Look(ref wealth, "wealth");
			Scribe_Values.Look(ref mapCount, "mapCount");
			Scribe_Values.Look(ref colonists, "colonists");
			Scribe_Values.Look(ref colonistsNeedTending, "colonistsNeedTending");
			Scribe_Values.Look(ref medicalConditions, "medicalConditions");
			Scribe_Values.Look(ref enemies, "enemies");
			Scribe_Values.Look(ref wildAnimals, "wildAnimals");
			Scribe_Values.Look(ref tamedAnimals, "tamedAnimals");
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
			Scribe_Values.Look(ref numRaidsEnemy, "numRaidsEnemy");
			Scribe_Values.Look(ref numThreatBigs, "numThreatBigs");
			Scribe_Values.Look(ref colonistsKilled, "colonistsKilled");
			Scribe_Values.Look(ref greatestPopulation, "greatestPopulation");
			Scribe_Values.Look(ref inGameHours, "inGameHours");
			Scribe_Values.Look(ref damageTakenPawns, "damageTakenPawns");
			Scribe_Values.Look(ref damageTakenThings, "damageTakenThings");
			Scribe_Values.Look(ref damageDealt, "damageDealt");
		}

		public StatsRequest TransferModel(string id)
		{
			return new()
			{
				Id = id,
				Wealth = wealth,
				MapCount = mapCount,
				Colonists = colonists,
				ColonistsNeedTending = colonistsNeedTending,
				MedicalConditions = medicalConditions,
				Enemies = enemies,
				WildAnimals = wildAnimals,
				TamedAnimals = tamedAnimals,
				Visitors = visitors,
				Prisoners = prisoners,
				DownedColonists = downedColonists,
				MentalColonists = mentalColonists,
				Rooms = rooms,
				Caravans = caravans,
				WeaponDps = weaponDps,
				Electricity = electricity,
				Medicine = medicine,
				Food = food,
				Fire = fire,
				Conditions = conditions,
				Temperature = temperature,
				NumRaidsEnemy = numRaidsEnemy,
				NumThreatBigs = numThreatBigs,
				ColonistsKilled = colonistsKilled,
				GreatestPopulation = greatestPopulation,
				InGameHours = inGameHours,
				DamageTakenPawns = damageTakenPawns,
				DamageTakenThings = damageTakenThings,
				DamageDealt = damageDealt
			};
		}
	}
}
