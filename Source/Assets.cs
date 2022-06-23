using System.IO;
using UnityEngine;
using Verse;

namespace Rimionship
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		static readonly AssetBundle assets = LoadAssetBundle();
		static readonly GameObject hud = assets.LoadAsset<GameObject>("HUD");

		public static GameObject runtimeHUD;
		public static UnityEngine.UI.Text placement;
		public static RectTransform arrowTransform;
		public static UnityEngine.UI.Text placements;
		public static UnityEngine.UI.Text scores;

		public static Animator statsAnimator;
		public static Animator panelAnimator;

		static Assets()
		{
			runtimeHUD = Object.Instantiate(hud);
			Object.DontDestroyOnLoad(runtimeHUD);

			var tr = runtimeHUD.transform;

			var _stats = tr.Find("Stats");
			statsAnimator = _stats.GetComponent<Animator>();

			var _panel = _stats.Find("Panel");
			panelAnimator = _panel.GetComponent<Animator>();

			var _pos = _panel.Find("pos");
			placement = _pos.GetComponent<UnityEngine.UI.Text>();

			arrowTransform = _panel.Find("arrow").GetComponent<RectTransform>();

			var _names = _panel.Find("names");
			placements = _names.GetComponent<UnityEngine.UI.Text>();

			var _scores = _panel.Find("scores");
			scores = _scores.GetComponent<UnityEngine.UI.Text>();

			HUD.SetPlacement(1);
			HUD.SetArrow(0);
			HUD.SetPlacements("Andreas", "Brrainz", "pardeike");
			HUD.SetScores(2834095, 41000, 123);

			HUD.SetPanelVisble(true);
		}

		static string GetModRootDirectory()
		{
			var me = LoadedModManager.GetMod<RimionshipMod>();
			return me.Content.RootDir;
		}

		static AssetBundle LoadAssetBundle()
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
