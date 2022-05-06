using HarmonyLib;
using RimWorld;
using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using Verse.Steam;

namespace Rimionship
{
	// replace New Game with Rimionship button in the main menu
	//
	[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
	class OptionListingUtility_DrawOptionListing_Patch
	{
		static void Prefix(List<ListableOption> optList)
		{
			var label = "NewColony".Translate();
			var option = optList.FirstOrDefault(opt => opt.label == label);
			if (option == null) return;
			option.label = "Rimionship";
			option.action = () =>
			{
				MainMenuDrawer.CloseMainTab();
				GameDataSaveLoader.LoadGame(Path.Combine(RimionshipMod.rootDir, "Resources", "rimionship"));
			};
		}
	}

	// open colonist configuration page after map is loaded
	//
	[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
	class Game_FinalizeInit_Patch
	{
		static void Postfix()
		{
			if (Find.TickManager.TicksGame > 5000) return;
			Find.WindowStack.Add(new ConfigurePawns());
		}
	}

	// replace Auto-sort mods button in mod configuration dialog with Load-default-rimionship
	//
	[HarmonyPatch(typeof(Page_ModsConfig), nameof(Page_ModsConfig.DoWindowContents))]
	class Page_ModsConfig_DoWindowContents_Patch
	{
		static void LoadRimionshipMods()
		{
			var modList = new ulong[] { 2009463077, 0, 818773962, 761421485, 867467808, 839005762, 2773821103, 2790250834 };

			foreach (var id in modList)
			{
				if (ModLister.AllInstalledMods.Any(mod => mod.GetPublishedFileId().m_PublishedFileId == id))
					continue;
				_ = SteamUGC.SubscribeItem(new PublishedFileId_t(id));
			}
			WorkshopItems.RebuildItemsList();

			var activeMods = modList
				.Select(id => ModLister.AllInstalledMods.FirstOrDefault(mod => mod.GetPublishedFileId().m_PublishedFileId == id))
				.OfType<ModMetaData>()
				.ToList();

			ModLister.AllInstalledMods.Do(mod => mod.Active = false);
			activeMods.Do(mod => mod.Active = true);

			ModsConfig.Save();
			ModsConfig.RecacheActiveMods();
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instruction)
		{
			var from = SymbolExtensions.GetMethodInfo(() => ModsConfig.TrySortMods());
			var to = SymbolExtensions.GetMethodInfo(() => LoadRimionshipMods());
			var list = Transpilers.MethodReplacer(instruction, from, to).ToList();

			var ldstr = list.FirstOrDefault(code => code.operand is string s && s == "ResolveModOrder");
			ldstr.operand = "LoadRimionshipMods";

			return list.AsEnumerable();
		}
	}
}
