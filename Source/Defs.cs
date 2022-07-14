using RimWorld;
using Verse;

namespace Rimionship
{
	[DefOf]
	[StaticConstructorOnStartup]
	public static class Defs
	{
		public static ThingDef SacrificationSpot;

		public static JobDef GettingSacrificed;
		public static JobDef SacrificeColonist;
		public static JobDef WatchSacrification;

		public static SoundDef Ambience;
		public static SoundDef EvilChoir;
		public static SoundDef Execution;
		public static SoundDef Thunder;
		public static SoundDef Bloodgod;
		public static SoundDef Nope;

		// vanilla defs that are not exposed by code

		public static MentalStateDef Slaughterer;
		public static MentalStateDef InsultingSpree;
		public static MentalStateDef SadisticRage;
		public static MentalStateDef TargetedInsultingSpree;
		public static MentalStateDef MurderousRage;
		public static MentalStateDef GiveUpExit;

		public static TraitDef FastLearner;
		public static TraitDef Immunity;
		public static TraitDef QuickSleeper;
		public static TraitDef Nimble;
		public static TraitDef NightOwl;
		public static TraitDef TorturedArtist;
		public static TraitDef Neurotic;
		public static TraitDef Gourmand;
		public static TraitDef SlowLearner;
	}
}
