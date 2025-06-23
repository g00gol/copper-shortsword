using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ItemDataExporter.Models;
using ItemDataExporter.Services;
using ItemDataExporter.Exporters;

namespace ItemDataExporter
{
	public class ItemDataExporter : Mod
	{
		// Keep the Mod class for mod metadata, but move the actual work to ModSystem
	}

	public class ItemDataExporterSystem : ModSystem
	{
		private bool hasExported = false;
		private RecipeService _recipeService;
		private TooltipService _tooltipService;
		private ObtainMethodService _obtainMethodService;
		private ItemProcessingService _itemProcessingService;
		private JsonExporter _jsonExporter;
		private SpriteExtractionService _spriteExtractionService;

		public override void PostAddRecipes()
		{
			if (!hasExported)
			{
				hasExported = true;
				InitializeServices();
				ExportAllItemData();
			}
		}
		private void InitializeServices()
		{
			var outputPath = Path.Combine(Main.SavePath, "ItemDataExport");
			Directory.CreateDirectory(outputPath);

			_recipeService = new RecipeService();
			_tooltipService = new TooltipService();
			_obtainMethodService = new ObtainMethodService(_recipeService);
			_itemProcessingService = new ItemProcessingService(_recipeService, _obtainMethodService, _tooltipService);
			_jsonExporter = new JsonExporter(Mod);
			_spriteExtractionService = new SpriteExtractionService(outputPath);

			// Log NPC loot cache statistics
			Mod.Logger.Info($"NPC loot cache built with {_obtainMethodService.GetNpcLootCacheCount()} NPCs and {_obtainMethodService.GetTotalCachedDrops()} total drop entries");
		}

		private void ExportAllItemData()
		{
			Mod.Logger.Info("Starting item data export using PostAddRecipes approach...");

			try
			{
				var exportData = new ItemExportData();
				var outputPath = Path.Combine(Main.SavePath, "ItemDataExport");
				Directory.CreateDirectory(outputPath);

				int processedCount = 0;
				var modCounts = new Dictionary<string, int>();

				Mod.Logger.Info($"Total item types to process: {ItemLoader.ItemCount}");

				// Process all loaded items using the working approach
				for (int type = 0; type < ItemLoader.ItemCount; type++)
				{
					try
					{
						Item item = new Item();
						item.SetDefaults(type);

						if (string.IsNullOrWhiteSpace(item.Name) || item.type <= ItemID.None)
							continue;

						string modName = item.ModItem?.Mod?.Name?.ToLower() ?? "vanilla";

						// Track items per mod
						if (!modCounts.ContainsKey(modName))
							modCounts[modName] = 0;
						modCounts[modName]++; try
						{
							_itemProcessingService.ProcessItem(item, exportData, modName);

							// Extract sprite for this item
							if (item.ModItem != null)
							{
								_spriteExtractionService.ExtractModdedItemSprite(item, modName);
							}
							else
							{
								_spriteExtractionService.ExtractItemSprite(item.type);
							}
						}
						catch (Exception ex)
						{
							Mod.Logger.Warn(ex.Message);
						}

						processedCount++;

						// Log progress every 1000 items
						if (processedCount % 1000 == 0)
						{
							Mod.Logger.Info($"Progress: {processedCount}/{ItemLoader.ItemCount} items processed...");
						}
					}
					catch (Exception ex)
					{
						Mod.Logger.Warn($"Failed to create item with ID {type}: {ex.Message}");
					}
				}

				Mod.Logger.Info($"Processed {processedCount} out of {ItemLoader.ItemCount} items");
				Mod.Logger.Info("Items by mod:");
				foreach (var kvp in modCounts.OrderByDescending(x => x.Value))
				{
					Mod.Logger.Info($"  {kvp.Key}: {kvp.Value} items");
				}
				Mod.Logger.Info($"Export counts - Weapons: {exportData.Weapons.Count}, Tools: {exportData.Tools.Count}, Armor: {exportData.Armor.Count}, Accessories: {exportData.Accessories.Count}, Misc: {exportData.Misc.Count}");

				// Extract NPC sprites
				Mod.Logger.Info("Starting NPC sprite extraction...");
				ExtractNpcSprites();

				// Export all data to JSON files
				_jsonExporter.ExportToJsonFiles(exportData, outputPath);

				Mod.Logger.Info($"Item data export completed! Files saved to: {outputPath}");
			}
			catch (Exception ex)
			{
				Mod.Logger.Error($"Error during export: {ex.Message}");
				Mod.Logger.Error($"Stack trace: {ex.StackTrace}");
			}
		}

		private void ExtractNpcSprites()
		{
			Mod.Logger.Info("Extracting NPC sprites...");
			int processedNpcs = 0;

			// Process all NPCs
			for (int npcType = 1; npcType < NPCLoader.NPCCount; npcType++)
			{
				try
				{
					var npc = new NPC();
					npc.SetDefaults(npcType);

					// Skip invalid NPCs or ones with no name
					if (string.IsNullOrEmpty(npc.FullName))
						continue;

					// Skip projectiles disguised as NPCs (usually have very low life)
					if (npc.lifeMax <= 1 && !npc.townNPC)
						continue;

					string modName = npc.ModNPC?.Mod?.Name?.ToLower() ?? "vanilla";

					// Extract sprite for this NPC
					if (npc.ModNPC != null)
					{
						_spriteExtractionService.ExtractModdedNpcSprite(npc, modName);
					}
					else
					{
						_spriteExtractionService.ExtractNpcSprite(npcType);
					}

					processedNpcs++;

					// Log progress every 100 NPCs
					if (processedNpcs % 100 == 0)
					{
						Mod.Logger.Info($"Progress: {processedNpcs} NPC sprites extracted...");
					}
				}
				catch (Exception ex)
				{
					Mod.Logger.Warn($"Failed to extract sprite for NPC {npcType}: {ex.Message}");
				}
			}

			Mod.Logger.Info($"Completed NPC sprite extraction. Processed {processedNpcs} NPCs.");
		}
	}
}
