using Api;
using Brrainz;
using Grpc.Core;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using Verse;

namespace Rimionship
{
	/*
	[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
	static class UIRoot_Entry_Init_Patch
	{
		static void Postfix()
		{
			Log.TryOpenLogWindow();

			Authentication authentication = new Authentication { tokenCallback = TokenCallback };
			LongEventHandler.ExecuteWhenFinished(authentication.InitiateTwitchAuth);
		}

		static void TokenCallback(string token)
		{
			Log.Error($"TOKEN: {token}");
		}
	}
	*/

	[StaticConstructorOnStartup]
	class RimionshipMod : Mod
	{
		public static string rootDir;
		public static string[] dependencies = { "System.Memory", "System.Numerics.Vectors", "System.Runtime.CompilerServices.Unsafe", "System.Interactive.Async", "Google.Protobuf", "Grpc.Core" };
		public static string api = "API";

		public RimionshipMod(ModContentPack content) : base(content)
		{
			rootDir = content.RootDir;

			var harmony = new Harmony("net.pardeike.rimworld.mod.rimionship");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);

			LoadAPI();
			ExecuteCall();
		}

		public static void ExecuteCall()
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			var credentials = new SslCredentials(caRoots);
			var channel = new Channel("localserver.local:5001", credentials);
			var client = new API.APIClient(channel);

			var request = new HelloRequest();
			try
			{
				var response = client.Hello(request);
				var transactionId = response.Id;
				Log.Warning($"Our transaction ID is {transactionId}");
			}
			catch (RpcException e)
			{
				Log.Warning("GreeterClient received error: " + e);
			}

			channel.ShutdownAsync().Wait();
		}

		public static void LoadAPI()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
			foreach (var lib in dependencies) LoadDll(lib);
			LoadDll(api);
			AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
		}

		public static void LoadDll(string name)
		{
			var path = Path.Combine(rootDir, "Libs", $"{name}.dll");
			try
			{
				_ = Assembly.LoadFrom(path);
			}
			catch
			{
				Log.Warning($"Loading {name} failed");
			}
		}

		public static Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
		{
			var name = args.Name;
			var idx = name.IndexOf(",");
			name = name.Substring(0, idx) + ".dll";
			var dllPath = Path.Combine(@"..\Libs", name);
			var asm = Assembly.LoadFrom(dllPath);
			return asm;
		}
	}
}
