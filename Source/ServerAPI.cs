using Api;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class ServerAPI
	{
		static readonly CancellationTokenSource source = new();

		static float _nextStatsUpdate;
		public static bool WaitUntilNextStatSend()
		{
			var result = Time.realtimeSinceStartup < _nextStatsUpdate;
			if (result == false)
				_nextStatsUpdate = Time.realtimeSinceStartup + PlayState.currentStatsSendingInterval;
			return result;
		}

		public static void CancelAll()
		{
			source.Cancel();
		}

		public static void WrapCall(Action call)
		{
			if (Communications.Client == null)
				return;
			try
			{
				call();
			}
			catch (RpcException e)
			{
				if (e.ShouldReport())
				{
					PlayState.errorCount++;
					AsyncLogger.Error($"gRPC error: {e}");
				}
			}
			catch (Exception e)
			{
				AsyncLogger.Error($"Exception: {e}");
			}
		}

		public static void SendHello(bool openBrowser = false)
		{
			WrapCall(() =>
			{
				var id = Tools.UniqueModID;
				var request = new HelloRequest() { Id = id };
				//AsyncLogger.Warning("-> Hello");
				var response = Communications.Client.Hello(request, null, null, source.Token);
				PlayState.modRegistered = response.Found;
				PlayState.AllowedMods = response.GetAllowedMods();
				//AsyncLogger.Warning($"{PlayState.modRegistered} {PlayState.AllowedMods.ToArray()} <- Hello");
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
				while (source.IsCancellationRequested == false)
				{
					SendHello();
					Thread.Sleep(5000);
				}
			})
			.Start();
		}

		public static bool StartGame()
		{
			var id = Tools.UniqueModID;
			var existingHash = Tools.FileHash(Assets.GameFilePath());
			var response = Communications.Client.Start(new StartRequest() { Id = Tools.UniqueModID }, null, null, source.Token);
			PlayState.startingPawnCount = response.StartingPawnCount;
			ApplySettings(response.Settings);

			var ourHash = Tools.FileHash(Assets.GameFilePath());
			var theirHash = response.GameFileHash;
			if (ourHash == theirHash)
				return true;

			var fileUrl = response.GameFileUrl;
			var savedServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = (obj, certificate, chain, errors) => true;
			try
			{
				using var client = new WebClient();
				client.Headers.Add("Accept", "application/binary");
				client.Headers.Add("X-ModID", Tools.UniqueModID);
				using var stream = client.OpenRead(fileUrl);
				using var file = File.Create(Assets.GameFilePath());
				stream.CopyTo(file);
				var info = new FileInfo(Assets.GameFilePath());
				return info.Length > 0;
			}
			catch (Exception ex)
			{
				Log.Error($"Exception while downloading {fileUrl}: {ex}");
				return false;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = savedServerCertificateValidationCallback;
			}
		}

		public static void StartSyncing()
		{
			new Thread(() =>
			{
				var WaitForChange = false;
				while (source.IsCancellationRequested == false)
				{
					try
					{
						var id = Tools.UniqueModID;
						var request = new SyncRequest() { Id = id, WaitForChange = WaitForChange };
						WaitForChange = true;
						//AsyncLogger.Warning($"-> Sync");
						var response = Communications.Client.Sync(request, null, null, source.Token);
						//AsyncLogger.Warning($"{response.PartCase} <- Sync");
						HandleSyncResponse(response);
					}
					catch (RpcException e)
					{
						if (e.ShouldReport())
						{
							PlayState.errorCount++;
							AsyncLogger.Error($"gRPC error: {e}");
						}
					}
					catch (Exception e)
					{
						AsyncLogger.Error($"Exception: {e}");
					}
					Thread.Sleep(1000);
				}
			})
			.Start();
		}

		public static void HandleSyncResponse(SyncResponse response)
		{
			PlayState.serverMessage = response.Message;
			ApplySettings(response.Settings);
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
		}

		static void ApplySettings(Api.Settings settings)
		{
			if (Tools.DevMode)
				return;

			settings ??= new Api.Settings();
			var traits = settings.Traits;
			if (traits != null)
			{
				RimionshipMod.settings.scaleFactor = traits.ScaleFactor;
				RimionshipMod.settings.goodTraitSuppression = traits.GoodTraitSuppression;
				RimionshipMod.settings.badTraitSuppression = traits.BadTraitSuppression;
			}
			var rising = settings.Rising;
			if (rising != null)
			{
				RimionshipMod.settings.maxFreeColonistCount = rising.MaxFreeColonistCount;
				RimionshipMod.settings.risingInterval = rising.RisingInterval;
				RimionshipMod.settings.risingCooldown = rising.RisingCooldown;
			}
			var punishment = settings.Punishment;
			if (punishment != null)
			{
				// TODO: remove from API
				// RimionshipMod.settings.randomStartPauseMin = punishment.RandomStartPauseMin;
				// RimionshipMod.settings.randomStartPauseMax = punishment.RandomStartPauseMax;
				RimionshipMod.settings.startPauseInterval = punishment.StartPauseInterval;
				RimionshipMod.settings.finalPauseInterval = punishment.FinalPauseInterval;
				RimionshipMod.settings.minThoughtFactor = punishment.MinThoughtFactor;
				RimionshipMod.settings.maxThoughtFactor = punishment.MaxThoughtFactor;
			}
		}

		public static void SendStat(Model_Stat stat)
		{
			WrapCall(() =>
			{
				var request = stat.TransferModel(Tools.UniqueModID);
				//AsyncLogger.Warning("-> Stats");
				var response = Communications.Client.Stats(request, null, null, source.Token);
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
				_ = Communications.Client.FutureEvents(request, null, null, source.Token);
				//AsyncLogger.Warning("<- FutureEvents");
			});
		}
	}
}
