using Api;
using Grpc.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public static class ServerAPI
	{
		public static int currentStatsSendingInterval = 10;
		public static int errorCount = 0;

		private static float _nextStatsUpdate;
		public static bool WaitUntilNextStatSend()
		{
			var result = Time.realtimeSinceStartup < _nextStatsUpdate;
			if (result == false)
				_nextStatsUpdate = Time.realtimeSinceStartup + currentStatsSendingInterval;
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
					errorCount++;
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

				if (Communications.Client.Hello(request).Found == false)
				{
					var rnd = Tools.GenerateHexString(256);
					var url = $"https://{Communications.hostName}?rnd={rnd}&id={id}";
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
				}
			});
		}

		public static void SendStat(Model_Stat stat)
		{
			WrapCall(() =>
			{
				var request = stat.TransferModel(Tools.UniqueModID);
				currentStatsSendingInterval = Communications.Client.Stats(request).Interval;
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
