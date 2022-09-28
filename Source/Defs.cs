using RimWorld;
using Verse;

namespace Rimionship
{
	[DefOf]
	[StaticConstructorOnStartup]
	public static class Defs
	{
		public static ThingDef SacrificationSpot;
		public static ThingDef Filth_BloodHuman;

		public static JobDef GettingSacrificed;
		public static JobDef SacrificeColonist;
		public static JobDef WatchSacrification;

		public static SoundDef Ambience;
		public static SoundDef EvilChoir;
		public static SoundDef Execution;
		public static SoundDef Thunder;
		public static SoundDef Bloodgod;
		public static SoundDef Nope;
		public static SoundDef Prime;
		public static SoundDef BingBong;
		public static SoundDef Death;

		// vanilla defs that are not exposed by code

		public static MentalStateDef Slaughterer;
		public static MentalStateDef InsultingSpree;
		public static MentalStateDef TargetedInsultingSpree;
		public static MentalStateDef MurderousRage;
		public static MentalStateDef GiveUpExit;
		public static MentalStateDef Tantrum;
		public static MentalStateDef KillingSpree; // ours

		public static TraitDef FastLearner;
		public static TraitDef Immunity;
		public static TraitDef QuickSleeper;
		public static TraitDef Nimble;
		public static TraitDef NightOwl;
		public static TraitDef TorturedArtist;
		public static TraitDef Neurotic;
		public static TraitDef Gourmand;
		public static TraitDef SlowLearner;

		public static HediffDef HearingLoss;

		public static PawnKindDef Squirrel;
		public static PawnKindDef GuineaPig;
		public static PawnKindDef Chinchilla;
		public static PawnKindDef Tortoise;
		public static PawnKindDef Rat;

		public static IncidentDef StuffFromAbove;

		public static ThoughtDef FearOfBloodGod;
	}
}
