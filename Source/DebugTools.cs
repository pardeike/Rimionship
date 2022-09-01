using RimWorld;
using Verse;
using Verse.Sound;

namespace Rimionship
{
	class DebugTools
	{
		static void Execute(int level, int choice)
		{
			var oldLevel = BloodGod.Instance.punishLevel;
			BloodGod.Instance.punishLevel = level;
			if (BloodGod.Instance.CommencePunishment(choice))
			{
				Find.LetterStack.ReceiveLetter("PunishmentLetterTitle".Translate(), "PunishmentLetterContent".Translate(level), LetterDefOf.NegativeEvent, null);
				Defs.Thunder.PlayOneShotOnCamera();
			}
			BloodGod.Instance.punishLevel = oldLevel;
		}

		// Level 1

		[DebugAction("Punishment", "1 - Flashstorm", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentFlashstorm() => Execute(1, 0);

		[DebugAction("Punishment", "1 - Psychic Drone", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentPsychicDrone() => Execute(1, 1);

		[DebugAction("Punishment", "1 - Solar Flare", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentSolarFlare() => Execute(1, 2);

		[DebugAction("Punishment", "1 - Toxic Fallout", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentToxicFallout() => Execute(1, 3);

		// Level 2

		[DebugAction("Punishment", "2 - Slaughterer", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentSlaughterer() => Execute(2, 0);

		[DebugAction("Punishment", "2 - Social Fighting", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentSocialFighting() => Execute(2, 1);

		[DebugAction("Punishment", "2 - Random Disease Animal", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentRandomDiseaseAnimal() => Execute(2, 2);

		[DebugAction("Punishment", "2 - Tantrum", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentTantrum() => Execute(2, 3);

		[DebugAction("Punishment", "2 - Bzzt All Batteries", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentBzztAll() => Execute(2, 4);

		// Level 3

		[DebugAction("Punishment", "3 - Berserk", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentBerserk() => Execute(3, 0);

		[DebugAction("Punishment", "3 - Random Disease Human", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentRandomDiseaseHuman() => Execute(3, 1);

		[DebugAction("Punishment", "3 - Insulting Spree", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentInsultingSpree() => Execute(3, 2);

		[DebugAction("Punishment", "3 - Angry Animals", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentAngryAnimals() => Execute(3, 3);

		// Level 4

		[DebugAction("Punishment", "4 - Killing Spree", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentKillingSpree() => Execute(4, 0);

		[DebugAction("Punishment", "4 - Targeted Insulting Spree", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentTargetedInsultingSpree() => Execute(4, 1);

		[DebugAction("Punishment", "4 - Random HediffGiver", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentRandomHediffGiver() => Execute(4, 2);

		[DebugAction("Punishment", "4 - Exploding Boomalopes", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentExplodingBoomalopes() => Execute(4, 3);

		// Level 5

		[DebugAction("Punishment", "5 - ColonistBecomesDumber", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentColonistBecomesDumber() => Execute(5, 0);

		[DebugAction("Punishment", "5 - MurderousRage", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentMurderousRage() => Execute(5, 1);

		[DebugAction("Punishment", "5 - Give Up & Exit", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentGiveUpExit() => Execute(5, 2);

		[DebugAction("Punishment", "5 - Stuff From Above", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void PunishmentStuffFromAbove() => Execute(5, 3);
	}
}
