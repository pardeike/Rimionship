using Api;
using Grpc.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace Rimionship
{
	public class Loader
	{
		public static readonly string hostName = "mod.rimionship.com";

		public static void Load(string rootDir)
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			var channel = new Channel($"{hostName}:443", new SslCredentials(caRoots));
			Communications.client = new API.APIClient(channel);
			Communications.Hello();
		}
	}

	public static class Communications
	{
		public static API.APIClient client;

		public static int interval = 10;
		public static float nextUpdate;

		public static void Hello()
		{
			var id = Tools.UniqueModID;
			var request = new HelloRequest() { Id = id };
			try
			{
				if (client.Hello(request).Found == false)
				{
					var rnd = Tools.GenerateHexString(256);
					var url = $"https://{Loader.hostName}?rnd={rnd}&id={id}";
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
				}
			}
			catch (RpcException e)
			{
				Log.Error("gRPC error: " + e);
			}
		}

		public static bool WaitUntilNextStatSend()
		{
			var result = Time.realtimeSinceStartup < nextUpdate;
			if (result == false)
				nextUpdate = Time.realtimeSinceStartup + interval;
			return result;
		}

		public static void SendStat(Model_Stat stat)
		{
			if (client == null) return;
			var id = PlayerPrefs.GetString("rimionship-id");
			var request = stat.TransferModel(id);
			try
			{
				interval = client.Stats(request).Interval;
			}
			catch (RpcException e)
			{
				Log.Error("gRPC error: " + e);
			}
		}

		public static void SendFutureEvents(IEnumerable<FutureEvent> events)
		{
			if (client == null) return;
			var id = PlayerPrefs.GetString("rimionship-id");
			var request = new FutureEventsRequest() { Id = id };
			request.AddEvents(events);
			try
			{
				_ = client.FutureEvents(request);
			}
			catch (RpcException e)
			{
				Log.Error("gRPC error: " + e);
			}
		}
	}
}
