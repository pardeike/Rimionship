using Api;
using Brrainz;
using Grpc.Core;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
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

	public static class State
	{
		public static API.APIClient client;
		public static string id;
	}

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
			CreateClient();
			CreateModId();
		}

		public static void CreateClient()
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			var credentials = new SslCredentials(caRoots);
			var channel = new Channel("localserver.local:5001", credentials);
			State.client = new API.APIClient(channel);
		}

		public static void CreateModId()
		{
			var id = PlayerPrefs.GetString("rimionship-id");
			if ((id ?? "") == "")
			{
				var request = new HelloRequest();
				try
				{
					id = State.client.Hello(request).Id;
					PlayerPrefs.SetString("rimionship-id", id);
					var url = $"https://localserver.local:5001?id={id}";
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
				}
				catch (RpcException e)
				{
					Log.Warning("gRPC error: " + e);
				}
			}
			Log.Warning($"id {id}");
			State.id = id;
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
