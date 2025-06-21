using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using ItemScraper.Services;
using ItemScraper.Models;

namespace ItemScraper
{
  public class ItemScraperSystem : ModSystem
  {
    public override void PostAddRecipes()
    {
      var bossDropService = new BossDropService();
      bossDropService.CollectBossData();

      var recipeService = new RecipeService();
      var classifier = new ItemClassifier(bossDropService);

      var itemsByModAndType = new Dictionary<string, Dictionary<string, List<object>>>();

      for (int type = 0; type < ItemLoader.ItemCount; type++)
      {
        Item item = new();
        item.SetDefaults(type);
        if (string.IsNullOrWhiteSpace(item.Name) || item.type <= ItemID.None) continue;

        string modName = item.ModItem?.Mod?.Name ?? "Terraria";
        string category = classifier.ClassifyItem(item);
        string subcategory = classifier.GetWeaponSubType(item, category);
        bool isArmorSet = item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1;
        var recipes = recipeService.GetRecipes(item.type);
        var bossDrops = bossDropService.GetBossDrops(item.type);

        var itemObject = new
        {
          Mod = modName,
          Name = item.Name,
          Type = item.type,
          ItemType = category,
          SubType = subcategory,
          Class = classifier.GetItemClass(item),
          Tooltip = item.ToolTip?.ToString(),
          Stats = new
          {
            Damage = item.damage,
            Crit = item.crit,
            KnockBack = item.knockBack,
            UseTime = item.useTime,
            UseStyle = item.useStyle,
            AutoReuse = item.autoReuse,
            Mana = item.mana,
            Width = item.width,
            Height = item.height,
            Value = item.value,
            Rarity = item.rare
          },
          ArmorSet = isArmorSet ? classifier.GetSetBonus(item) : null,
          Recipes = recipes,
          BossDrops = bossDrops.Count > 0 ? bossDrops : null,
          ObtainMethod = classifier.DetermineObtainMethod(recipes, bossDrops)
        };

        if (!itemsByModAndType.ContainsKey(modName))
          itemsByModAndType[modName] = new();
        if (!itemsByModAndType[modName].ContainsKey(category))
          itemsByModAndType[modName][category] = new();

        itemsByModAndType[modName][category].Add(itemObject);
      }

      string outputDir = Path.Combine(Main.SavePath, "ItemDump");
      Directory.CreateDirectory(outputDir);

      foreach (var (mod, typeMap) in itemsByModAndType)
      {
        string modDir = Path.Combine(outputDir, mod);
        Directory.CreateDirectory(modDir);

        foreach (var (typeName, entries) in typeMap)
        {
          string jsonPath = Path.Combine(modDir, $"{typeName}.json");
          File.WriteAllText(jsonPath, JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        if (bossDropService.HasBossData(mod))
        {
          string bossPath = Path.Combine(modDir, "Bosses.json");
          File.WriteAllText(bossPath, JsonConvert.SerializeObject(bossDropService.GetBosses(mod), Formatting.Indented));
        }
      }
    }
  }
}
