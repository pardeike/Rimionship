using Grpc.Core;
using HarmonyLib;
using Helloworld;
using System;
using System.IO;
using System.Reflection;
using Verse;
using static Helloworld.Greeter;

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
		public static string[] dependencies = { "Google.Protobuf", "Grpc.Core", "System.Interactive.Async" };
		public static string crosspromotion = "CrossPromotion";
		public static string api = "API";

		public RimionshipMod(ModContentPack content) : base(content)
		{
			rootDir = content.RootDir;

			var harmony = new Harmony("net.pardeike.rimworld.mod.rimionship");
			harmony.PatchAll();

			LoadAPI();
			ExecuteCall();
		}

		public static void ExecuteCall()
		{
			var channel = new Channel("localhost:5000", ChannelCredentials.Insecure);
			var client = new GreeterClient(channel);
			var request = new HelloRequest
			{
				Name = "Andreas Pardeike"
			};
			var response = client.SayHello(request);
			Log.Warning("GreeterClient received response: " + response.Message);
			channel.ShutdownAsync().Wait();
		}

		public static void LoadAPI()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

			foreach (var lib in dependencies) LoadDll(lib);
			LoadDll(crosspromotion);
			LoadDll(api);

			AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
		}

		public static void LoadDll(string name)
		{
			var path = Path.Combine(rootDir, "Libs", $"{name}.dll");
			_ = Assembly.LoadFrom(path);
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
