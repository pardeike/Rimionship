using System.Collections.Generic;
using System.Linq;
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

	public static class PlayState
	{
		public static int currentStatsSendingInterval = 10;
		public static int errorCount = 0;

		public static bool modRegistered = false;

		public static List<KeyValuePair<string, ulong>> allowedMods = new();
		public static bool modlistValid = false;
		public static bool recheckModlist = true;

		public static string serverMessage = null;
		public static TournamentState tournamentState = TournamentState.Stopped;
		public static int tournamentStartHour = 0;
		public static int tournamentStartMinute = 0;

		public static bool Valid => tournamentState == TournamentState.Started
			&& modRegistered
			&& modlistValid
			&& Communications.State == CommState.Ready;

		public static void EvaluateModlist()
		{
			if (recheckModlist == false) return;
			recheckModlist = false;
			var allowed = allowedMods.Select(mod => mod.Key).ToHashSet();
			modlistValid = Tools.InstalledMods().Except(allowed).Any() == false;
		}
	}
}
