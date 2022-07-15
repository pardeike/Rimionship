using Api;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
				if (e.ShouldReport())
				{
					PlayState.errorCount++;
					AsyncLogger.Error($"gRPC error: {e}", new StackTrace());
				}
			}
			catch (Exception e)
			{
				AsyncLogger.Error($"Exception: {e}", new StackTrace());
			}
		}

		public static void SendHello(bool openBrowser = false)
		{
			WrapCall(() =>
			{
				var id = Tools.UniqueModID;
				var request = new HelloRequest() { Id = id };
				//AsyncLogger.Warning("-> Hello");
				var response = Communications.Client.Hello(request);
				PlayState.modRegistered = response.Found;
				PlayState.AllowedMods = response.GetAllowedMods();
				AsyncLogger.Warning($"{PlayState.modRegistered} {PlayState.AllowedMods.ToArray()} <- Hello");
				HUD.Update(response);
				if (PlayState.modRegistered == false && openBrowser)
				{
					var rnd = Tools.GenerateHexString(256);
					var url = $"https://{Communications.hostName}?rnd={rnd}&id={id}";
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
				}
			});
		}

		public static void StartConnecting()
		{
			new Thread(() =>
			{
				while (cancelled == false)
				{
					SendHello();
					Thread.Sleep(5000);
				}
			})
			.Start();
		}

		public static void StartSyncing()
		{
			new Thread(() =>
			{
				while (cancelled == false)
				{
					try
					{
						var id = Tools.UniqueModID;
						var request = new SyncRequest() { Id = id };
						//AsyncLogger.Warning($"-> Sync");
						var response = Communications.Client.Sync(request);
						//AsyncLogger.Warning($"{response.PartCase} <- Sync");
						HandleSyncResponse(response);
					}
					catch (RpcException e)
					{
						if (e.ShouldReport())
						{
							PlayState.errorCount++;
							AsyncLogger.Error($"gRPC error: {e}", new StackTrace());
						}
					}
					catch (Exception e)
					{
						AsyncLogger.Error($"Exception: {e}", new StackTrace());
					}
					Thread.Sleep(1000);
				}
			})
			.Start();
		}

		public static void HandleSyncResponse(SyncResponse response)
		{
			switch (response.PartCase)
			{
				case SyncResponse.PartOneofCase.Message:
					PlayState.serverMessage = response.Message ?? "";
					break;
				case SyncResponse.PartOneofCase.Settings:
					response.Settings ??= new Api.Settings();
					var traits = response.Settings.Traits;
					if (traits != null)
					{
						RimionshipMod.settings.scaleFactor = traits.ScaleFactor;
						RimionshipMod.settings.goodTraitSuppression = traits.GoodTraitSuppression;
						RimionshipMod.settings.badTraitSuppression = traits.BadTraitSuppression;
					}
					var rising = response.Settings.Rising;
					if (rising != null)
					{
						RimionshipMod.settings.maxFreeColonistCount = rising.MaxFreeColonistCount;
						RimionshipMod.settings.risingInterval = rising.RisingInterval;
					}
					var punishment = response.Settings.Punishment;
					if (punishment != null)
					{
						RimionshipMod.settings.randomStartPauseMin = punishment.RandomStartPauseMin;
						RimionshipMod.settings.randomStartPauseMax = punishment.RandomStartPauseMax;
						RimionshipMod.settings.startPauseInterval = punishment.StartPauseInterval;
						RimionshipMod.settings.finalPauseInterval = punishment.FinalPauseInterval;
					}
					break;
				case SyncResponse.PartOneofCase.State:
					response.State ??= new State();
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
				//AsyncLogger.Warning("-> Stats");
				var response = Communications.Client.Stats(request);
				//AsyncLogger.Warning($"{response.Interval} <- Stats");
				PlayState.currentStatsSendingInterval = response.Interval;
			});
		}

		public static void SendFutureEvents(IEnumerable<FutureEvent> events)
		{
			WrapCall(() =>
			{
				var request = new FutureEventsRequest() { Id = Tools.UniqueModID };
				request.AddEvents(events);
				//AsyncLogger.Warning("-> FutureEvents");
				_ = Communications.Client.FutureEvents(request);
				//AsyncLogger.Warning("<- FutureEvents");
			});
		}
	}
}
