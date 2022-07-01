using RimWorld;

namespace Rimionship
{
	struct TraitScore
	{
		static int scoreIndex = 0;

		public float badScore;
		public TraitDef def;
		public int degree;

		public TraitScore(TraitDef def, int degree = 0)
		{
			badScore = scoreIndex++ / (float)TraitTools.traitCount;
			this.def = def;
			this.degree = degree;
		}
	}

	static class TraitTools
	{
		public static int traitCount = 65;

		public static TraitScore[] sortedTraits = new[]
		{
			new TraitScore(TraitDefOf.Tough),
			new TraitScore(Defs.FastLearner),
			new TraitScore(TraitDefOf.Industriousness, 2), // Industrious
			new TraitScore(TraitDefOf.Industriousness, 1), // Hard Worker
			new TraitScore(TraitDefOf.SpeedOffset, 2), // Jogger
			new TraitScore(TraitDefOf.NaturalMood, 2), // Sanguine
			new TraitScore(TraitDefOf.NaturalMood, 1), // Optimist
			new TraitScore(Defs.Immunity, 1), // Super-immune
			new TraitScore(TraitDefOf.Nerves, 2), // Iron Willed
			new TraitScore(Defs.QuickSleeper),
			new TraitScore(TraitDefOf.Nerves, 1), // Steadfast
			new TraitScore(TraitDefOf.Kind),
			new TraitScore(TraitDefOf.Psychopath),
			new TraitScore(Defs.Nimble),
			new TraitScore(TraitDefOf.SpeedOffset, 1), // Fast Walker
			new TraitScore(TraitDefOf.GreatMemory),
			new TraitScore(TraitDefOf.Beauty, 2), // Beautiful
			new TraitScore(TraitDefOf.Bloodlust),
			new TraitScore(TraitDefOf.Masochist),
			new TraitScore(TraitDefOf.Beauty, 1), // Pretty
			new TraitScore(TraitDefOf.Transhumanist),
			new TraitScore(TraitDefOf.Cannibal),
			new TraitScore(TraitDefOf.ShootingAccuracy, -1), // Trigger Happy
			new TraitScore(TraitDefOf.Ascetic),
			new TraitScore(TraitDefOf.TooSmart),
			new TraitScore(TraitDefOf.Brawler),
			new TraitScore(TraitDefOf.ShootingAccuracy, 1), // Careful Shooter
			new TraitScore(TraitDefOf.Undergrounder),
			new TraitScore(Defs.NightOwl),
			new TraitScore(TraitDefOf.Bisexual),
			new TraitScore(TraitDefOf.PsychicSensitivity, 2), // Psychically Hypersensitive
			new TraitScore(TraitDefOf.PsychicSensitivity, 1), // Psychically Sensitive
			new TraitScore(TraitDefOf.PsychicSensitivity, -2), // Psychically Deaf
			new TraitScore(TraitDefOf.PsychicSensitivity, -1), // Psychically Dull
			new TraitScore(TraitDefOf.Gay),
			new TraitScore(Defs.TorturedArtist),
			new TraitScore(TraitDefOf.Asexual),
			new TraitScore(Defs.Neurotic, 1), // Neurotic
			new TraitScore(Defs.Neurotic, 2), // Very Neurotic
			new TraitScore(TraitDefOf.DrugDesire, -1), // Teetotaler
			new TraitScore(Defs.Gourmand),
			new TraitScore(TraitDefOf.Nudist),
			new TraitScore(TraitDefOf.Nerves, -1), // Nervous
			new TraitScore(TraitDefOf.Beauty, -1), // Ugly
			new TraitScore(TraitDefOf.Abrasive),
			new TraitScore(TraitDefOf.DrugDesire, 1), // Chemical Interest
			new TraitScore(TraitDefOf.NaturalMood, -1), // Pessimist
			new TraitScore(Defs.Immunity, -1), // Sickly
			new TraitScore(TraitDefOf.Industriousness, -1), // Lazy
			new TraitScore(TraitDefOf.AnnoyingVoice),
			new TraitScore(TraitDefOf.DislikesMen), // Misandrist
			new TraitScore(TraitDefOf.Nerves, -2), // Volatile
			new TraitScore(TraitDefOf.SpeedOffset, -1), // Slowpoke
			new TraitScore(TraitDefOf.DrugDesire, 2), // Chemical Fascination
			new TraitScore(TraitDefOf.BodyPurist),
			new TraitScore(TraitDefOf.DislikesWomen), // Misogynist
			new TraitScore(TraitDefOf.CreepyBreathing),
			new TraitScore(TraitDefOf.Beauty, -2), // Staggeringly Ugly
			new TraitScore(TraitDefOf.Greedy),
			new TraitScore(TraitDefOf.Wimp),
			new TraitScore(Defs.SlowLearner),
			new TraitScore(TraitDefOf.Industriousness, -2), // Slothful
			new TraitScore(TraitDefOf.Jealous),
			new TraitScore(TraitDefOf.NaturalMood, -2), // Depressive
			new TraitScore(TraitDefOf.Pyromaniac),
		};
	}
}
