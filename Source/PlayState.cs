using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace Rimionship
{
	public enum CommState
	{
		Idle,
		Connecting,
		Ready,
		TransientFailure,
		Shutdown
	}

	public enum TournamentState
	{
		Stopped,
		Training,
		Prepare,
		Started,
		Completed
	}

	public enum ModListStatus
	{
		Unknown,
		Invalid,
		Valid
	}

	public static class PlayState
	{
		public static int currentStatsSendingInterval = 10;
		public static int errorCount = 0;

		public static bool modRegistered = false;

		static List<KeyValuePair<string, ulong>> _allowedMods = new();
		public static List<KeyValuePair<string, ulong>> AllowedMods
		{
			get => _allowedMods;
			set
			{
				_allowedMods = value;
				if (_allowedMods.NullOrEmpty())
					modlistStatus = ModListStatus.Unknown;
				else
				{
					var allowed = _allowedMods.Select(mod => mod.Key).ToHashSet();
					if (Tools.InstalledMods().Except(allowed).Any())
						modlistStatus = ModListStatus.Invalid;
					else
						modlistStatus = ModListStatus.Valid;
				}
			}
		}
		public static ModListStatus modlistStatus = ModListStatus.Unknown;

		public static string serverMessage = null;
		public static TournamentState tournamentState = TournamentState.Stopped;
		public static int tournamentStartHour = 0;
		public static int tournamentStartMinute = 0;

		public static int startingPawnCount = 0;

		public static bool Valid => (tournamentState == TournamentState.Started || tournamentState == TournamentState.Training)
			&& modRegistered
			&& modlistStatus == ModListStatus.Valid
			&& Communications.State == CommState.Ready;

		public static string InvalidModsTooltip()
		{
			var invalid = Tools.InstalledMods()
				.Except(AllowedMods.Select(mod => mod.Key))
				.Select(packageId => ModsConfig.activeModsInLoadOrderCached.FirstOrDefault(mod => mod.PackageId == packageId)?.Name)
				.OfType<string>()
				.Join();
			return "InvalidModsTooltip".Translate(new NamedArgument(invalid, "list"));
		}

		public static async Task LoadGame()
		{
			if (await ServerAPI.StartGame() == false)
				return;

			var gameToLoad = Assets.GameFilePath().Replace(".rws", "");

			LongEventHandler.QueueLongEvent(
				() => Current.Game = new Game { InitData = new GameInitData { gameToLoad = gameToLoad } },
				"Play",
				"LoadingLongEvent",
				true,
				ex => Log.Error($"Game loading exception: {ex}"),
				true
			);
		}
	}
}
