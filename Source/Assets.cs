using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Verse;

namespace Rimionship
{
	[StaticConstructorOnStartup]
	public static class Assets
	{
		static readonly AssetBundle assets = LoadAssetBundle();
		static readonly GameObject hud = assets.LoadAsset<GameObject>("HUD");
		public static readonly GameObject sacrificeEffects = assets.LoadAsset<GameObject>("SacrificeEffects");
		public static readonly Font menuFontSmall = assets.LoadAsset<Font>("MenuSmall");
		public static readonly Font menuFontLarge = assets.LoadAsset<Font>("MenuLarge");

		public static GameObject runtimeHUD;
		public static Transform scorePanel;
		public static UnityEngine.UI.Text placement;
		public static RectTransform arrowTransform;
		public static UnityEngine.UI.Text placements;
		public static UnityEngine.UI.Text scores;
		public static UnityEngine.UI.Image infoBackground;
		public static UnityEngine.UI.Text infoName;
		public static UnityEngine.UI.Text infoScore;

		public static Vector3 arrowAnchoredPosition3D;

		public static Animator statsAnimator;
		public static Animator panelAnimator;
		public static float infoWidth, infoHeight;
		public static RectTransform infoRectTransform;

		public static readonly Texture2D Rimionship = ContentFinder<Texture2D>.Get("Rimionship", true);
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
			Tools.LoadTexture("Penta0"),
			Tools.LoadTexture("Penta1"),
			Tools.LoadTexture("Penta2"),
			Tools.LoadTexture("Penta3"),
			Tools.LoadTexture("Penta4"),
			Tools.LoadTexture("Penta5")
		};

		public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("Button/ButtonBG", true);
		public static readonly Texture2D ButtonBGAtlasOver = ContentFinder<Texture2D>.Get("Button/ButtonBGOver", true);
		public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("Button/ButtonBGClick", true);
		public static readonly Texture2D ButtonPattern = ContentFinder<Texture2D>.Get("Button/Pattern", true);
		public static readonly Texture2D Note = ContentFinder<Texture2D>.Get("Note", true);

		public static bool catchMouseEvents = false;

		public class StatsScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
		{
#pragma warning disable CA1822
			public void Update()
			{
				var infoTop = 95 * Prefs.UIScale;
				infoRectTransform.anchoredPosition = new Vector2(0, -infoTop);

				var infoRect = new Rect(
					UI.screenWidth / 2 - infoWidth / 2,
					UI.screenHeight - (infoTop + infoHeight) / Prefs.UIScale,
					infoWidth,
					infoHeight
				);
				var pos = UI.MousePositionOnUI;
				var dx = Mathf.Abs(infoRect.center.x - pos.x) * Prefs.UIScale - infoRect.width / 2;
				var dy = Mathf.Abs(infoRect.center.y - pos.y) * Prefs.UIScale - infoRect.height / 2;
				var fx = GenMath.LerpDoubleClamped(0, 30, 0, 1, dx);
				var fy = GenMath.LerpDoubleClamped(0, 30, 0, 1, dy);
				SetInfoAlpha(Mathf.Max(fx, fy));
			}
#pragma warning restore CA1822

			public void OnPointerEnter(PointerEventData _)
			{
				// TODO if the mouse is already down when we enter we will eat
				// the mouse up event when it happens inside us. Cannot figure out
				// why detecting a pressed mouse button does not work here though
				//
				catchMouseEvents = true;
			}

			public void OnPointerExit(PointerEventData _)
			{
				catchMouseEvents = false;
			}
		}

		static Assets()
		{
			runtimeHUD = Object.Instantiate(hud);
			Object.DontDestroyOnLoad(runtimeHUD);

			var tr = runtimeHUD.transform;

			var _stats = tr.Find("Stats");
			_ = _stats.gameObject.AddComponent<StatsScript>();

			statsAnimator = _stats.GetComponent<Animator>();

			var trigger = (EventTrigger)_stats.gameObject.AddComponent(typeof(EventTrigger));
			var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
			entry.callback.AddListener((eventData) => { eventData.Use(); });
			trigger.triggers.Add(entry);

			scorePanel = _stats.Find("Panel");
			panelAnimator = scorePanel.GetComponent<Animator>();

			placement = scorePanel.Find("pos").GetComponent<UnityEngine.UI.Text>();
			arrowTransform = scorePanel.Find("arrow").GetComponent<RectTransform>();
			placements = scorePanel.Find("names").GetComponent<UnityEngine.UI.Text>();
			scores = scorePanel.Find("scores").GetComponent<UnityEngine.UI.Text>();

			arrowAnchoredPosition3D = arrowTransform.anchoredPosition3D;

			var _info = tr.Find("Info");
			infoRectTransform = _info.GetComponent<RectTransform>();
			var infoTop = 95 * Prefs.UIScale;
			infoWidth = infoRectTransform.sizeDelta.x;
			infoHeight = infoRectTransform.sizeDelta.y;
			infoRectTransform.anchoredPosition = new Vector2(0, -infoTop);
			infoBackground = _info.Find("bg").GetComponent<UnityEngine.UI.Image>();
			infoName = _info.Find("name").GetComponent<UnityEngine.UI.Text>();
			infoScore = _info.Find("score").GetComponent<UnityEngine.UI.Text>();

			HUD.SetName(" ");
			HUD.SetScore(0);

			HUD.SetPlacement(0);
			HUD.SetArrow(-99);
			HUD.SetPlacements(" ", " ", " ");
			HUD.SetScores(0, 0, 0);

			HUD.SetPanelVisble(true);

			Tools.assetsInited = true;
		}

		public static void SetInfoAlpha(float alpha)
		{
			Color c;
			c = infoBackground.color;
			c.a = alpha;
			infoBackground.color = c;
			c = infoName.color;
			c.a = alpha;
			infoName.color = c;
			c = infoScore.color;
			c.a = alpha;
			infoScore.color = c;
		}

		public static void UIScaleChanged()
		{
			if (runtimeHUD == null)
				return;
			var info = runtimeHUD.transform.Find("Info");
			if (info == null)
				return;
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
