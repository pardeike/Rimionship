using Brrainz;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Rimionship
{
	[StaticConstructorOnStartup]
	class RimionshipMod : Mod
	{
		public static Settings settings;
		public static string rootDir;
		public static string[] dependencies = { "System.Memory", "System.Numerics.Vectors", "System.Runtime.CompilerServices.Unsafe", "System.Interactive.Async", "Google.Protobuf", "Grpc.Core" };
		public static string api = "API";

		public RimionshipMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<Settings>();
			rootDir = content.RootDir;
			AsyncLogger.StartCoroutine();

			var harmony = new Harmony("net.pardeike.rimworld.mod.rimionship");
			harmony.PatchAll();
			LongEventHandler.ExecuteWhenFinished(() => Patches_Mods.Patch(harmony));

			CrossPromotion.Install(76561197973010050);

			LoadAPI();
			Communications.Start(rootDir);

			Application.wantsToQuit += () =>
			{
				ServerAPI.CancelAll();
				Communications.Stop();
				return true;
			};
		}

		public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
		public override string SettingsCategory() => Configuration.CustomSettings ? "Rimionship" : "";

		public static void LoadAPI()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
			foreach (var lib in dependencies)
				LoadDll(lib);
			LoadDll(api);
			AppDomain.CurrentDomain.AssemblyResolve -= ResolveEventHandler;
		}

		public static void LoadDll(string name)
		{
			var path = Path.Combine(rootDir, "Libs", $"{name}.dll");
			try
			{ _ = Assembly.LoadFrom(path); }
			catch { Log.Error($"Loading {name} failed"); }
		}

		public static Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
		{
			var name = args.Name;
			name = name.Substring(0, name.IndexOf(",")) + ".dll";
			var dllPath = Path.Combine(@"..\Libs", name);
			return Assembly.LoadFrom(dllPath);
		}
	}
}
