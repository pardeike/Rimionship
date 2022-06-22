using System.IO;
using UnityEngine;
using Verse;

namespace Rimionship
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static readonly AssetBundle assets = LoadAssetBundle();
		public static readonly GameObject hud = assets.LoadAsset<GameObject>("HUD");
		//public static readonly Font robotoCondRegular = assets.LoadAsset<Font>("RobotoCondensed-Regular");
		//public static readonly Font robotoCondLight = assets.LoadAsset<Font>("RobotoCondensed-Light");
		//public static readonly Font robotoCondBold = assets.LoadAsset<Font>("RobotoCondensed-Bold");

		static Assets()
		{
			var runtimeHUD = Object.Instantiate(hud);
			Object.DontDestroyOnLoad(runtimeHUD);
		}

		public static string GetModRootDirectory()
		{
			var me = LoadedModManager.GetMod<RimionshipMod>();
			return me.Content.RootDir;
		}

		public static AssetBundle LoadAssetBundle()
		{
			var platform = "win";
			if (Application.platform == RuntimePlatform.OSXPlayer)
				platform = "mac";
			if (Application.platform == RuntimePlatform.LinuxPlayer)
				platform = "linux";

			var path = Path.Combine(GetModRootDirectory(), "Resources", "rimionship-" + platform);
			return AssetBundle.LoadFromFile(path);
		}
	}
}
