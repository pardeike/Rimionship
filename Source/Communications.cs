using Api;
using Grpc.Core;
using System.IO;

namespace Rimionship
{
	public class Communications
	{
		public static readonly string hostName = "mod.rimionship.com";

		static object _channel;
		static Channel Channel
		{
			get => (Channel)_channel;
			set => _channel = value;
		}

		static object _client;
		public static API.APIClient Client
		{
			get => (API.APIClient)_client;
			set => _client = value;
		}

		public static void Start(string rootDir)
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			Channel = new Channel($"{hostName}:443", new SslCredentials(caRoots));
			Client = new API.APIClient(Channel);
			ServerAPI.StartConnecting();
			ServerAPI.StartSyncing();
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
