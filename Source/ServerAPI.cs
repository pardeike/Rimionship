using Api;
using Grpc.Core;
using HarmonyLib;
using RimionshipServer.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class ServerAPI
	{
		const int API_VERSION = 1;
		static readonly bool LOGGING = Configuration.UseApiLogging;

		static readonly CancellationTokenSource source = new();
		static float _nextStatsUpdate;

		public static bool WaitUntilNextStatSend()
		{
			var result = Time.realtimeSinceStartup < _nextStatsUpdate;
			if (result == false)
				_nextStatsUpdate = Time.realtimeSinceStartup + PlayState.currentStatsSendingInterval;
			return result;
		}

		public static DateTime DefaultDeadline => DateTime.UtcNow.AddMinutes(10);
		public static DateTime ShortDeadline => DateTime.UtcNow.AddSeconds(4);

		public static void CancelAll()
		{
			source.Cancel();
		}

		public static async Task SendHello()
		{
			await ServerAPITools.WrapCall(async () =>
			{
				var id = Tools.UniqueModID;
				var request = new HelloRequest() { ApiVersion = API_VERSION, Id = id };
				if (LOGGING)
					AsyncLogger.Warning($"-> Hello");
				var response = await Communications.Client.HelloAsync(request, null, DefaultDeadline, source.Token);
				if (LOGGING)
				{
					var scores = response.GetScores().Join(score => $"{score.Position}|{score.TwitchName}|{score.LatestScore}", ",");
					AsyncLogger.Warning($"exists={response.UserExists} quit={response.HasQuit} ({response.TwitchName}) #{response.Position} in [{scores}] <- Hello");
				}
				PlayState.modRegistered = response.UserExists;
				PlayState.hasQuit = response.HasQuit;
				PlayState.AllowedMods = response.GetAllowedMods();
				HUD.Update(response);
			});
		}

		public static async Task Login()
		{
			await ServerAPITools.WrapCall(async () =>
			{
				var loginRequest = new LoginRequest() { Id = Tools.UniqueModID };
				if (LOGGING)
					AsyncLogger.Warning($"-> Login");
				var loginResponse = await Communications.Client.LoginAsync(loginRequest, null, DefaultDeadline, source.Token);
				var loginToken = loginResponse.LoginToken;

				Application.OpenURL(loginResponse.LoginUrl);

				var timeout = DateTime.Now.AddSeconds(60);
				while (DateTime.Now < timeout)
				{
					var linkAccountRequest = new LinkAccountRequest() { Id = Tools.UniqueModID, LoginToken = loginResponse.LoginToken };
					if (LOGGING)
						AsyncLogger.Warning($"-> LinkAccount");
					var linkAccountResponse = await Communications.Client.LinkAccountAsync(linkAccountRequest, null, DefaultDeadline, source.Token);
					if (LOGGING)
						AsyncLogger.Warning($"exists={linkAccountResponse.UserExists} ({linkAccountResponse.TwitchName}) <- LinkAccount");
					if (linkAccountResponse.UserExists)
					{
						PlayState.modRegistered = true;
						var twitchName = linkAccountResponse.TwitchName;
						AsyncLogger.Warning($"USER = {twitchName}");
						HUD.SetName(twitchName);
						break;
					}
					await Task.Delay(1000);
				}
			});
		}

		public static async Task StartConnecting()
		{
			while (source.IsCancellationRequested == false)
			{
				await SendHello();
				await Task.Delay(5000);
			}
		}

		public static async Task<bool> StartGame()
		{
			var id = Tools.UniqueModID;
			if (LOGGING)
				AsyncLogger.Warning($"-> Start");
			var response = await Communications.Client.StartAsync(new StartRequest() { Id = Tools.UniqueModID }, null, DefaultDeadline, source.Token);
			if (LOGGING)
				AsyncLogger.Warning($"pawns={response.StartingPawnCount} <- Start");
			PlayState.startingPawnCount = response.StartingPawnCount;
			ApplySettings(response.Settings);

			try
			{
				var ourHash = Tools.FileHash(Assets.GameFilePath());
				var theirHashUrl = response.GameFileHash;
				using var httpClient = new WebClient();
				var theirHash = httpClient.DownloadString(theirHashUrl);
				if (ourHash == theirHash)
					return true;
			}
			catch (Exception ex)
			{
				Log.Error($"Exception while getting info for {response.GameFileHash}: {ex}");
				Find.WindowStack.Add(new Dialog_Information("Download Error", ex.Message, "Oh"));
				return false;
			}

			Find.WindowStack.Add(new Dialog_DownloadingGame());

			var fileUrl = response.GameFileUrl;
			var savedServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = (obj, certificate, chain, errors) => true;
			try
			{
				using var client = new GZipWebClient();
				client.Headers.Add(HttpRequestHeader.Accept, "application/gzip");
				client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
				using var stream = new GZipStream(client.OpenRead(fileUrl), CompressionMode.Decompress);
				using var file = File.Create(Assets.GameFilePath());
				await stream.CopyToAsync(file);
				var info = new FileInfo(Assets.GameFilePath());
				return info.Length > 0;
			}
			catch (Exception ex)
			{
				Log.Error($"Exception while downloading {fileUrl}: {ex}");
				Find.WindowStack.Add(new Dialog_Information("Download Error", ex.Message, "Oh"));
				return false;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = savedServerCertificateValidationCallback;
				_ = Find.WindowStack.TryRemove(typeof(Dialog_DownloadingGame));
			}
		}

		public static async Task StopGame()
		{
			var id = Tools.UniqueModID;
			if (LOGGING)
				AsyncLogger.Warning($"-> Stop");
			try
			{
				_ = await Communications.Client.StopAsync(new StopRequest() { Id = Tools.UniqueModID }, null, deadline: DefaultDeadline, source.Token);
			}
			catch (Exception ex)
			{
				AsyncLogger.Warning($"Stop EX:{ex}");
			}
			if (LOGGING)
				AsyncLogger.Warning($"<- Stop");
		}

		public static async Task StartSyncing()
		{
			var WaitForChange = false;
			while (source.IsCancellationRequested == false)
			{
				try
				{
					var id = Tools.UniqueModID;
					var request = new SyncRequest() { Id = id, WaitForChange = WaitForChange };
					WaitForChange = true;
					if (LOGGING)
						AsyncLogger.Warning($"-> Sync");
					var response = await Communications.Client.SyncAsync(request, null, deadline: DefaultDeadline, source.Token);
					if (LOGGING)
						AsyncLogger.Warning($"{response.State} <- Sync");
					HandleSyncResponse(response);
				}
				catch (RpcException e)
				{
					if (LOGGING)
						AsyncLogger.Warning($"{e.Status.StatusCode} <- Sync");

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

		static void ApplySettings(RimionshipServer.API.Settings settings)
		{
			if (Configuration.CustomSettings)
				return;

			settings ??= new RimionshipServer.API.Settings();
			var traits = settings.Traits;
			if (traits != null)
			{
				RimionshipMod.settings.scaleFactor = traits.ScaleFactor;
				RimionshipMod.settings.goodTraitSuppression = traits.GoodTraitSuppression;
				RimionshipMod.settings.badTraitSuppression = traits.BadTraitSuppression;
				RimionshipMod.settings.maxMeleeSkill = traits.MaxMeleeSkill;
				RimionshipMod.settings.maxMeleeFlames = traits.MaxMeleeFlames;
				RimionshipMod.settings.maxShootingSkill = traits.MaxShootingSkill;
				RimionshipMod.settings.maxShootingFlames = traits.MaxShootingFlames;
			}
			var rising = settings.Rising;
			if (rising != null)
			{
				RimionshipMod.settings.maxFreeColonistCount = rising.MaxFreeColonistCount;
				RimionshipMod.settings.risingInterval = rising.RisingInterval;
				RimionshipMod.settings.risingReductionPerColonist = rising.RisingReductionPerColonist;
				RimionshipMod.settings.risingIntervalMinimum = rising.RisingIntervalMinimum;
				RimionshipMod.settings.risingCooldown = rising.RisingCooldown;
			}
			var punishment = settings.Punishment;
			if (punishment != null)
			{
				RimionshipMod.settings.startPauseInterval = punishment.StartPauseInterval;
				RimionshipMod.settings.finalPauseInterval = punishment.FinalPauseInterval;
				RimionshipMod.settings.minThoughtFactor = punishment.MinThoughtFactor;
				RimionshipMod.settings.maxThoughtFactor = punishment.MaxThoughtFactor;
			}
		}

		public static async Task SendStat(Model_Stat stat)
		{
			await ServerAPITools.WrapCall(async () =>
			{
				var request = stat.TransferModel(Tools.UniqueModID);
				if (LOGGING)
					AsyncLogger.Warning("-> Stats");
				var response = await Communications.Client.StatsAsync(request, null, ShortDeadline, source.Token);
				if (LOGGING)
					AsyncLogger.Warning($"{response.Interval} <- Stats");
				PlayState.currentStatsSendingInterval = response.Interval;
			});
		}

		public static async Task SendFutureEvents(IEnumerable<FutureEvent> events)
		{
			await ServerAPITools.WrapCall(async () =>
			{
				var request = new FutureEventsRequest() { Id = Tools.UniqueModID };
				request.AddEvents(events);
				if (LOGGING)
					AsyncLogger.Warning("-> FutureEvents");
				_ = await Communications.Client.FutureEventsAsync(request, null, ShortDeadline, source.Token);
				if (LOGGING)
					AsyncLogger.Warning("<- FutureEvents");
			});
		}

		public static void TriggerAttention(string context, int delta)
		{
			if (Find.TickManager.TicksGame < 2)
				return;

			if (LOGGING)
				AsyncLogger.Warning($"Attention +{delta} [{context}]");

			_ = Task.Run(async () =>
			{
				var request = new AttentionRequest() { Id = Tools.UniqueModID, Delta = delta };
				if (LOGGING)
					AsyncLogger.Warning("-> Attention");
				_ = await Communications.Client.AttentionAsync(request, null, ShortDeadline, source.Token);
				if (LOGGING)
					AsyncLogger.Warning("<- Attention");
			});
		}
	}
}
