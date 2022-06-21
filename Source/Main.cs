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
	/*[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
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
	}*/

	/*[HarmonyPatch(typeof(TraitSet), nameof(TraitSet.GainTrait))]
	static class TraitSet_GainTrait_Patch
	{
		static void Postfix(TraitSet __instance, Trait trait)
		{
			Log.Warning($"{__instance.pawn.LabelShortCap} gained trait {trait.def.defName} with {trait.degree}");
		}
	}*/

	/*[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn))]
	[HarmonyPatch(new[] { typeof(PawnGenerationRequest) })]
	static class PawnGenerator_GeneratePawn_Patch
	{
		static void Prefix(PawnGenerationRequest request)
		{
			Log.TryOpenLogWindow();
			Log.Warning($"Generate pawn ({request.KindDef.defName}) for faction {request.Faction.name} {(request.MustBeCapableOfViolence ? "(must do violence)" : "")}) {(request.AllowAddictions ? "(allow addictions)" : "")}) {(request.ForcedTraits != null ? $"(forced: {request.ForcedTraits.Join(t => t.defName)})" : "")}) {(request.ProhibitedTraits != null ? $"(prohibited: {request.ProhibitedTraits.Join(t => t.defName)})" : "")})");
		}
	}*/

	public static class State
	{
		public static API.APIClient client;
		public static string id;
	}

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

			var harmony = new Harmony("net.pardeike.rimworld.mod.rimionship");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);

			LoadAPI();
			CreateClient();
			CreateModId();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Rimionship";
		}

		public static void CreateClient()
		{
			var caRoots = File.ReadAllText(Path.Combine(rootDir, "Resources", "ca.pem"));
			var credentials = new SslCredentials(caRoots);
			var channel = new Channel("mod.rimionship.com:443", credentials);
			State.client = new API.APIClient(channel);
		}

		public static void CreateModId()
		{
			var id = PlayerPrefs.GetString("rimionship-id") ?? "";
			var oldId = id;
			var request = new HelloRequest() { Id = id };
			try
			{
				id = State.client.Hello(request).Id;
				PlayerPrefs.SetString("rimionship-id", id);
				var url = $"https://mod.rimionship.com?id={id}";
				if (oldId != id)
					LongEventHandler.ExecuteWhenFinished(() => Application.OpenURL(url));
			}
			catch (RpcException e)
			{
				Log.Error("gRPC error: " + e);
			}
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
