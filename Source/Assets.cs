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
		public static readonly Font menuFontSmall = assets.LoadAsset<Font>("MenuSmall");
		public static readonly Font menuFontLarge = assets.LoadAsset<Font>("MenuLarge");

		public static GameObject runtimeHUD;
		public static UnityEngine.UI.Text placement;
		public static RectTransform arrowTransform;
		public static UnityEngine.UI.Text placements;
		public static UnityEngine.UI.Text scores;
		public static UnityEngine.UI.Text name;
		public static UnityEngine.UI.Text score;

		public static Animator statsAnimator;
		public static Animator panelAnimator;

		public static readonly Texture2D MainMenuLogin = ContentFinder<Texture2D>.Get("MainMenuLogin", true);
		public static readonly Texture2D MainMenuLoginOver = ContentFinder<Texture2D>.Get("MainMenuLoginOver", true);
		public static readonly Texture2D MainMenuInfo = ContentFinder<Texture2D>.Get("MainMenuInfo", true);
		public static readonly Texture2D StateOK = ContentFinder<Texture2D>.Get("StateOK", true);
		public static readonly Texture2D StateError = ContentFinder<Texture2D>.Get("StateError", true);
		public static readonly Texture2D StateWait = ContentFinder<Texture2D>.Get("StateWait", true);
		public static readonly Texture2D StateAction = ContentFinder<Texture2D>.Get("StateAction", true);
		public static readonly Texture2D CancelSpot = ContentFinder<Texture2D>.Get("CancelSpot", true);
		public static readonly Texture2D RemoveSpot = ContentFinder<Texture2D>.Get("RemoveSpot", true);
		public static readonly Texture2D Blood = ContentFinder<Texture2D>.Get("Things/Mote/BattleSymbols/Blood", true);
		public static readonly Texture2D Skull = ContentFinder<Texture2D>.Get("Things/Mote/BattleSymbols/Skull", true);
		public static readonly Texture2D Insult = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Insult", true);
		public static readonly Texture2D[] Pentas = new[]
		{
			ContentFinder<Texture2D>.Get("Penta0", true),
			ContentFinder<Texture2D>.Get("Penta1", true),
			ContentFinder<Texture2D>.Get("Penta2", true),
			ContentFinder<Texture2D>.Get("Penta3", true),
			ContentFinder<Texture2D>.Get("Penta4", true),
			ContentFinder<Texture2D>.Get("Penta5", true)
		};

		public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("Button/ButtonBG", true);
		public static readonly Texture2D ButtonBGAtlasOver = ContentFinder<Texture2D>.Get("Button/ButtonBGOver", true);
		public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("Button/ButtonBGClick", true);
		public static readonly Texture2D ButtonPattern = ContentFinder<Texture2D>.Get("Button/Pattern", true);
		public static readonly Texture2D Note = ContentFinder<Texture2D>.Get("Note", true);

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

			HUD.SetName("");
			HUD.SetScore(0);

			HUD.SetPlacement(0);
			HUD.SetArrow(0);
			HUD.SetPlacements("", "", "");
			HUD.SetScores(0, 0, 0);

			HUD.SetPanelVisble(true);
		}

		public static void UIScaleChanged()
		{
			if (runtimeHUD == null) return;
			var info = runtimeHUD.transform.Find("Info");
			if (info == null) return;
			info.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -95 * Prefs.UIScale);
		}

		public static string GameFilePath()
		{
			return Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_Rimionship.rws");
		}

		public static void SaveGameFile(byte[] data)
		{
			File.WriteAllBytes(GameFilePath(), data);
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
