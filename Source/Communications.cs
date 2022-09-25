using Grpc.Core;
using System.IO;
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
				if (Configuration.UseLocalHost)
					return Configuration.LocalHostEndpoint;

				var endpoint = Configuration.CustomEndpoint;
				if (!string.IsNullOrEmpty(endpoint))
					return endpoint;

				return Configuration.ProductionEndpoint;
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
			var host = EndpointUri;
			if (host.Contains("localhost"))
				Channel = new Channel(host, ChannelCredentials.Insecure);
			else
			{
				var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
				Channel = new Channel(host, new SslCredentials(caRoots));
			}
			Client = new APIClient(Channel);

			if (Tools.DevMode)
			{
				Log.Warning($"SERVER: {host}");
				Log.Warning($"MOD ID: {Tools.UniqueModID}");
			}

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
