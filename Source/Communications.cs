using Grpc.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Verse;
using static RimionshipServer.API.API;

namespace Rimionship
{
	public class Communications
	{
		public static string EndpointUri
		{
			get
			{
				var prefix = "-rimionship-host=";
				var endpoint = Environment.GetCommandLineArgs().FirstOrDefault(f => f.StartsWith(prefix));
				if (!string.IsNullOrEmpty(endpoint))
					return endpoint.Substring(prefix.Length);

				endpoint = Environment.GetEnvironmentVariable("RIMIONSHIP-ENDPOINT");
				if (!string.IsNullOrEmpty(endpoint))
					return endpoint;

				return "mod-test.rimionship.com";
			}
		}

		static object _channel;
		static Channel Channel
		{
			get => (Channel)_channel;
			set => _channel = value;
		}

		static object _client;
		public static APIClient Client
		{
			get => (APIClient)_client;
			set => _client = value;
		}

		public static void Start(string rootDir)
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			Channel = new Channel(EndpointUri, new SslCredentials(caRoots));
			Client = new APIClient(Channel);

			if (Tools.DevMode)
				Log.Warning($"MOD ID: {Tools.UniqueModID}");
			_ = Task.Run(ServerAPI.StartConnecting);
			_ = Task.Run(ServerAPI.StartSyncing);
		}

		public static void Stop()
		{
			Channel?.ShutdownAsync().Wait();
			Channel = null;
			Client = null;
		}

		public static CommState State
		{
			get
			{
				if (Channel == null)
					return CommState.Shutdown;
				return Channel.State switch
				{
					ChannelState.Idle => CommState.Idle,
					ChannelState.Connecting => CommState.Connecting,
					ChannelState.Ready => CommState.Ready,
					ChannelState.TransientFailure => CommState.TransientFailure,
					ChannelState.Shutdown => CommState.Shutdown,
					_ => CommState.Shutdown,
				};
			}
		}
	}
}
