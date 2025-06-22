using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Terraria.ModLoader;
using ItemDataExporter.Models;
using ItemDataExporter.Services;

namespace ItemDataExporter.Exporters
{
  public class JsonExporter
  {
    private readonly Mod _mod;

    public JsonExporter(Mod mod)
    {
      _mod = mod;
    }

    public void ExportToJsonFiles(ItemExportData data, string outputPath)
    {
      _mod.Logger.Info($"Starting JSON export to: {outputPath}");
      _mod.Logger.Info($"Data to export - Weapons: {data.Weapons.Count}, Tools: {data.Tools.Count}, Armor: {data.Armor.Count}, Accessories: {data.Accessories.Count}, Misc: {data.Misc.Count}, Sprites: {data.Sprites.Count}");

      var settings = new JsonSerializerSettings
      {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
      };

      try
      {
        File.WriteAllText(Path.Combine(outputPath, "Weapons.json"),
            JsonConvert.SerializeObject(data.Weapons, settings));
        _mod.Logger.Info($"Exported {data.Weapons.Count} weapons to Weapons.json");

        File.WriteAllText(Path.Combine(outputPath, "Tools.json"),
            JsonConvert.SerializeObject(data.Tools, settings));
        _mod.Logger.Info($"Exported {data.Tools.Count} tools to Tools.json");

        File.WriteAllText(Path.Combine(outputPath, "Armor.json"),
            JsonConvert.SerializeObject(data.Armor, settings));
        _mod.Logger.Info($"Exported {data.Armor.Count} armor pieces to Armor.json");

        File.WriteAllText(Path.Combine(outputPath, "Accessories.json"),
            JsonConvert.SerializeObject(data.Accessories, settings));
        _mod.Logger.Info($"Exported {data.Accessories.Count} accessories to Accessories.json");

        File.WriteAllText(Path.Combine(outputPath, "Misc.json"),
            JsonConvert.SerializeObject(data.Misc, settings));
        _mod.Logger.Info($"Exported {data.Misc.Count} misc items to Misc.json");

        File.WriteAllText(Path.Combine(outputPath, "Sprites.json"),
            JsonConvert.SerializeObject(data.Sprites, settings));
        _mod.Logger.Info($"Exported {data.Sprites.Count} sprites to Sprites.json");

        // Generate armor sets (simplified - groups armor by name prefix)
        var armorSets = GenerateArmorSets(data.Armor);
        File.WriteAllText(Path.Combine(outputPath, "ArmorSets.json"),
            JsonConvert.SerializeObject(armorSets, settings));
        _mod.Logger.Info($"Exported {armorSets.Count} armor sets to ArmorSets.json");        // Generate entity data
        _mod.Logger.Info("Processing entities...");
        var entityService = new EntityProcessingService();
        var entities = entityService.ProcessAllEntities();
        File.WriteAllText(Path.Combine(outputPath, "Entities.json"),
            JsonConvert.SerializeObject(entities, settings));
        _mod.Logger.Info($"Exported {entities.Count} entities to Entities.json");

        _mod.Logger.Info("JSON export completed successfully!");
      }
      catch (Exception ex)
      {
        _mod.Logger.Error($"Error during JSON export: {ex.Message}");
        _mod.Logger.Error($"Stack trace: {ex.StackTrace}");
      }
    }

    private List<ArmorSetData> GenerateArmorSets(List<ArmorData> armorItems)
    {
      var sets = new List<ArmorSetData>();
      var armorGroups = armorItems
          .GroupBy(a => GetArmorSetName(a.Name))
          .Where(g => g.Count() >= 2);

      foreach (var group in armorGroups)
      {
        var armorPieces = group.ToList();
        sets.Add(new ArmorSetData
        {
          ID = armorPieces.First().ID,
          Name = group.Key,
          TotalDefense = armorPieces.Sum(a => a.Defense),
          SetBonus = "Unknown", // Would need more complex analysis
          Rarity = armorPieces.Max(a => a.Rarity),
          Description = $"{group.Key} armor set",
          Mod = armorPieces.First().Mod
        });
      }

      return sets;
    }

    private string GetArmorSetName(string armorName)
    {
      // Simple logic to extract set name by removing common suffixes
      var suffixes = new[] { " Helmet", " Hat", " Mask", " Breastplate", " Chestplate", " Shirt", " Leggings", " Greaves", " Pants" };

      foreach (var suffix in suffixes)
      {
        if (armorName.EndsWith(suffix))
        {
          return armorName.Substring(0, armorName.Length - suffix.Length);
        }
      }

      return armorName;
    }
  }
}
