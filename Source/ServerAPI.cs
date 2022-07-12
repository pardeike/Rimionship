using Api;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class ServerAPI
	{
		public static bool cancelled = false;

		private static float _nextStatsUpdate;
		public static bool WaitUntilNextStatSend()
		{
			var result = Time.realtimeSinceStartup < _nextStatsUpdate;
			if (result == false)
				_nextStatsUpdate = Time.realtimeSinceStartup + PlayState.currentStatsSendingInterval;
			return result;
		}

		public static void WrapCall(Action call)
		{
			if (Communications.Client == null) return;
			try
			{
				call();
			}
			catch (RpcException e)
			{
				if (e.StatusCode != StatusCode.Unavailable)
				{
					PlayState.errorCount++;
					Log.Error("gRPC error: " + e);
				}
			}
		}

		public static void SendHello()
		{
			WrapCall(() =>
			{
				var id = Tools.UniqueModID;
				var request = new HelloRequest() { Id = id };
				var response = Communications.Client.Hello(request);
				PlayState.modRegistered = response.Found;
				PlayState.allowedMods = response.GetAllowedMods();
				if (PlayState.modRegistered == false)
				{
					var rnd = Tools.GenerateHexString(256);
					var url = $"https://{Communications.hostName}?rnd={rnd}&id={id}";
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
				}
			});
		}

		public static void StartSyncing()
		{
			var task = Task.Run(async () =>
			{
				while (cancelled == false)
				{
					try
					{
						var request = new SyncRequest() { Id = Tools.UniqueModID };
						var stream = Communications.Client.Sync(request).ResponseStream;
						while (cancelled == false && await stream.MoveNext())
							HandleSyncResponse(stream.Current);
					}
					catch (RpcException e)
					{
						if (e.StatusCode != StatusCode.Unavailable)
						{
							PlayState.errorCount++;
							Log.Error("gRPC error: " + e);
						}
					}
					catch (Exception e)
					{
						Log.Error("Exception: " + e);
					}
					await Task.Delay(1000);
				}
			});
		}

		public static void HandleSyncResponse(SyncResponse response)
		{
			switch (response.PartCase)
			{
				case SyncResponse.PartOneofCase.Message:
					PlayState.serverMessage = response.Message;
					break;
				case SyncResponse.PartOneofCase.Settings:
					var traits = response.Settings.Traits;
					RimionshipMod.settings.scaleFactor = traits.ScaleFactor;
					RimionshipMod.settings.goodTraitSuppression = traits.GoodTraitSuppression;
					RimionshipMod.settings.badTraitSuppression = traits.BadTraitSuppression;
					var rising = response.Settings.Rising;
					RimionshipMod.settings.maxFreeColonistCount = rising.MaxFreeColonistCount;
					RimionshipMod.settings.risingInterval = rising.RisingInterval;
					var punishment = response.Settings.Punishment;
					RimionshipMod.settings.randomStartPauseMin = punishment.RandomStartPauseMin;
					RimionshipMod.settings.randomStartPauseMax = punishment.RandomStartPauseMax;
					RimionshipMod.settings.startPauseInterval = punishment.StartPauseInterval;
					RimionshipMod.settings.finalPauseInterval = punishment.FinalPauseInterval;
					RimionshipMod.settings.Write();
					break;
				case SyncResponse.PartOneofCase.State:
					PlayState.tournamentState = response.State.Game switch
					{
						State.Types.Game.Stopped => TournamentState.Stopped,
						State.Types.Game.Training => TournamentState.Training,
						State.Types.Game.Prepare => TournamentState.Prepare,
						State.Types.Game.Started => TournamentState.Started,
						State.Types.Game.Completed => TournamentState.Completed,
						_ => TournamentState.Stopped,
					};
					PlayState.tournamentStartHour = response.State.PlannedStartHour;
					PlayState.tournamentStartMinute = response.State.PlannedStartMinute;
					break;
			}
		}

		public static void SendStat(Model_Stat stat)
		{
			WrapCall(() =>
			{
				var request = stat.TransferModel(Tools.UniqueModID);
				PlayState.currentStatsSendingInterval = Communications.Client.Stats(request).Interval;
			});
		}

		public static void SendFutureEvents(IEnumerable<FutureEvent> events)
		{
			WrapCall(() =>
			{
				var request = new FutureEventsRequest() { Id = Tools.UniqueModID };
				request.AddEvents(events);
				_ = Communications.Client.FutureEvents(request);
			});
		}
	}
}
