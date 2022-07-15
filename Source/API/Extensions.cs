﻿using System.Collections.Generic;
using System.Linq;

namespace Api
{
	public static partial class Extensions
	{
		public static void AddEvents(this FutureEventsRequest request, IEnumerable<FutureEvent> events)
		{
			request.Event.AddRange(events);
		}

		public static List<KeyValuePair<string, ulong>> GetAllowedMods(this HelloResponse response)
		{
			return response.AllowedMods.Select(mod => new KeyValuePair<string, ulong>(mod.PackageId, mod.SteamId)).ToList();
		}

		public static List<KeyValuePair<string, int>> GetScores(this HelloResponse response)
		{
			return response.Score.Select(score => new KeyValuePair<string, int>(score.TwitchName, score.LatestScore)).ToList();
		}
	}
}
