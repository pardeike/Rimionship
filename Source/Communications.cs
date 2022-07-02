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
		public static void Load(string rootDir)
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			//var option = new List<ChannelOption> { new ChannelOption(ChannelOptions.SslTargetNameOverride, "localhost") };
			var channel = new Channel("mod.rimionship.com:443", new SslCredentials(caRoots)); // , option);
			Communications.client = new API.APIClient(channel);
			Communications.CreateModId();
		}
	}

	public static class Communications
	{
		public static API.APIClient client;
		public static string id;

		public static int interval = 10;
		public static float nextUpdate;

		public static void CreateModId()
		{
			var id = PlayerPrefs.GetString("rimionship-id") ?? "";
			var oldId = id;
			var request = new HelloRequest() { Id = id };
			try
			{
				id = client.Hello(request).Id;
				PlayerPrefs.SetString("rimionship-id", id);
				var url = $"https://mod.rimionship.com?id={id}";
				if (oldId != id)
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
			}
			catch (RpcException e)
			{
				Log.Error("gRPC error: " + e);
			}
			Communications.id = id;
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
