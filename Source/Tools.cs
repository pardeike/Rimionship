using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimionship
{
	struct ForcedTrait
	{
		public TraitDef def;
		public bool useMin;
	}

	static class TraitTools
	{
		public static TraitDef Neurotic => DefDatabase<TraitDef>.AllDefsListForReading.FirstOrDefault(t => t.defName == "Neurotic");

		class TraitScoreSorter : IComparer<Trait>
		{
			public static readonly Dictionary<string, int> traitScore = new()
			{
				{ "Tough", 0 },
				{ "Psychopath", 10 },
				{ "Cannibal", 20 },
				{ "Bloodlust", 30 },
				{ "Nimble", 40 },
				{ "QuickSleeper", 50 },
				{ "FastLearner", 60 },
				{ "Brawler", 70 },
				{ "GreatMemory", 80 },
				{ "TooSmart", 90 },
				{ "Ascetic", 100 },
				{ "Masochist", 110 },
				{ "Kind", 120 },
				{ "NightOwl", 130 },
				{ "Bisexual", 140 },
				{ "Undergrounder", 150 },
				{ "Transhumanist", 160 },
				{ "TorturedArtist", 170 },
				{ "Gay", 180 },
				{ "Asexual", 190 },
				{ "DislikesMen", 200 },
				{ "DislikesWomen", 210 },
				{ "CreepyBreathing", 220 },
				{ "BodyPurist", 230 },
				{ "Nudist", 240 },
				{ "Greedy", 250 },
				{ "Jealous", 260 },
				{ "AnnoyingVoice", 270 },
				{ "SlowLearner", 280 },
				{ "Gourmand", 290 },
				{ "Abrasive", 300 },
			};

			static int Score(Trait trait) => traitScore.TryGetValue(trait.def.defName, 1000);
			public int Compare(Trait x, Trait y) => Score(x).CompareTo(Score(y));
		}

		static readonly TraitScoreSorter traitScoreSorter = new();

		public static bool IsValid(this PawnGenerationRequest request, TraitDef newTraitDef, int degree, Pawn pawn)
		{
			var traitAllowed = request.KindDef.disallowedTraits == null || request.KindDef.disallowedTraits.Contains(newTraitDef) == false;
			var traitMatchesWorkTags = request.KindDef.requiredWorkTags == WorkTags.None || (newTraitDef.disabledWorkTags & request.KindDef.requiredWorkTags) == WorkTags.None;
			var traitNotProhibited = request.ProhibitedTraits == null || request.ProhibitedTraits.Contains(newTraitDef) == false;
			var traitFactionOK = request.Faction == null || Faction.OfPlayerSilentFail == null || request.Faction.HostileTo(Faction.OfPlayer) == false || newTraitDef.allowOnHostileSpawn;
			var traitWorkTypesOK = newTraitDef.requiredWorkTypes == null || pawn.OneOfWorkTypesIsDisabled(newTraitDef.requiredWorkTypes) == false;
			var traitWorkTagEnabled = pawn.WorkTagIsDisabled(newTraitDef.requiredWorkTags) == false;
			var traitOKWithChildhood = pawn.story.childhood == null || pawn.story.childhood.DisallowsTrait(newTraitDef, degree) == false;
			var traitOKWithAdulthood = pawn.story.adulthood == null || pawn.story.adulthood.DisallowsTrait(newTraitDef, degree) == false;

			return traitAllowed && traitMatchesWorkTags && traitNotProhibited && traitFactionOK && traitWorkTypesOK && traitWorkTagEnabled && traitOKWithChildhood && traitOKWithAdulthood;
		}

		public static void ForceTrait(this Pawn pawn, TraitDef traitDef, int degree)
		{
			new List<Trait>(pawn.story.traits.allTraits)
				.DoIf(t => t.def == traitDef || t.def.ConflictsWith(traitDef), t =>
				{
					// Log.Warning($"# REMOVE CONFLICT {t.def.defName}");
					pawn.story.traits.RemoveTrait(t);
				});

			var trait = new Trait(traitDef, degree, true);
			// Log.Warning($"# ADD {trait.def.defName}");
			pawn.story.traits.GainTrait(trait);
		}

		public static void RemoveBestTrait(this Pawn pawn)
		{
			if (pawn.story.traits.allTraits.Count < 2) return;
			var traits = new List<Trait>(pawn.story.traits.allTraits);
			traits.Sort(traitScoreSorter);
			var bestTrait = traits.First();
			// Log.Warning($"# REMOVE {bestTrait.def.defName}");
			pawn.story.traits.RemoveTrait(bestTrait);
		}
	}
}
