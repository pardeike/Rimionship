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
		public static readonly GameObject sacrificeEffects = assets.LoadAsset<GameObject>("sacrificeEffects");

		public static GameObject runtimeHUD;
		public static UnityEngine.UI.Text placement;
		public static RectTransform arrowTransform;
		public static UnityEngine.UI.Text placements;
		public static UnityEngine.UI.Text scores;
		public static UnityEngine.UI.Text name;
		public static UnityEngine.UI.Text score;

		public static Animator statsAnimator;
		public static Animator panelAnimator;

		public static Texture2D Blood = ContentFinder<Texture2D>.Get("Things/Mote/BattleSymbols/Blood", true);
		public static Texture2D Skull = ContentFinder<Texture2D>.Get("Things/Mote/BattleSymbols/Skull", true);
		public static Texture2D Insult = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Insult", true);

		static Assets()
		{
			runtimeHUD = Object.Instantiate(hud);
			Object.DontDestroyOnLoad(runtimeHUD);

			var tr = runtimeHUD.transform;

			var _stats = tr.Find("Stats");
			statsAnimator = _stats.GetComponent<Animator>();

			var _panel = _stats.Find("Panel");
			panelAnimator = _panel.GetComponent<Animator>();

			placement = _panel.Find("pos").GetComponent<UnityEngine.UI.Text>();
			arrowTransform = _panel.Find("arrow").GetComponent<RectTransform>();
			placements = _panel.Find("names").GetComponent<UnityEngine.UI.Text>();
			scores = _panel.Find("scores").GetComponent<UnityEngine.UI.Text>();

			var _info = tr.Find("Info");
			_info.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -95 * Prefs.UIScale);
			name = _info.Find("name").GetComponent<UnityEngine.UI.Text>();
			score = _info.Find("score").GetComponent<UnityEngine.UI.Text>();

			HUD.SetName("ArcticBathtubPirate1234");
			HUD.SetScore(2834095);

			HUD.SetPlacement(1);
			HUD.SetArrow(0);
			HUD.SetPlacements("ArcticBathtubPirate1234", "Brrainz", "edopeh");
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
