using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace ItemScraper
{
  public class ItemScraperSystem : ModSystem
  {
    private Dictionary<int, List<BossDropInfo>> itemToBossDrops = new();
    private Dictionary<string, List<BossInfo>> modToBosses = new();

    public override void PostAddRecipes()
    {
      // First, collect all boss information
      CollectBossData();

      var items = new List<object>();
      for (int type = 0; type < ItemLoader.ItemCount; type++)
      {
        Item item = new();
        item.SetDefaults(type);
        if (string.IsNullOrWhiteSpace(item.Name) || item.type <= ItemID.None)
          continue;

        var itemType = ClassifyItem(item);
        var itemClass = GetItemClass(item);
        var isArmorSet = item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1;
        var recipes = GetRecipes(item.type);
        var bossDrops = GetBossDrops(item.type);

        items.Add(new
        {
          Mod = item.ModItem?.Mod?.Name ?? "Terraria",
          Name = item.Name,
          Type = item.type,
          ItemType = itemType,
          Class = itemClass,
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
          ArmorSet = isArmorSet ? GetSetBonus(item) : null,
          Recipes = recipes,
          BossDrops = bossDrops.Count > 0 ? bossDrops : null,
          ObtainMethod = DetermineObtainMethod(recipes, bossDrops)
        });
      }

      var byMod = new Dictionary<string, object>();
      foreach (var item in items)
      {
        string modName = ((dynamic)item).Mod;
        if (!byMod.ContainsKey(modName))
        {
          byMod[modName] = new
          {
            Items = new List<object>(),
            Bosses = modToBosses.ContainsKey(modName) ? modToBosses[modName] : new List<BossInfo>()
          };
        }
          ((dynamic)byMod[modName]).Items.Add(item);
      }

      string outputDir = Path.Combine(Main.SavePath, "ItemDump");
      Directory.CreateDirectory(outputDir);

      foreach (var kvp in byMod)
      {
        File.WriteAllText(Path.Combine(outputDir, $"{kvp.Key}.json"),
            JsonConvert.SerializeObject(kvp.Value, Formatting.Indented));
      }
    }

    private void CollectBossData()
    {
      itemToBossDrops.Clear();
      modToBosses.Clear();

      // Iterate through all NPCs to find bosses and their drops
      // Start from 1 to avoid negative IDs that might cause issues
      for (int npcType = 1; npcType < NPCLoader.NPCCount; npcType++)
      {
        try
        {
          NPC npc = new();
          npc.SetDefaults(npcType);

          // Skip invalid NPCs
          if (string.IsNullOrWhiteSpace(npc.FullName) || npc.type <= 0)
            continue;

          // Check if this NPC is a boss
          bool isBoss = npc.boss || IsMiniBoss(npc);

          if (isBoss)
          {
            var bossInfo = new BossInfo
            {
              Name = npc.FullName,
              Type = npc.type,
              ModName = npc.ModNPC?.Mod?.Name ?? "Terraria",
              IsBoss = npc.boss,
              IsMiniBoss = !npc.boss && isBoss,
              MaxLife = npc.lifeMax
            };

            string modName = bossInfo.ModName;
            if (!modToBosses.ContainsKey(modName))
              modToBosses[modName] = new List<BossInfo>();
            modToBosses[modName].Add(bossInfo);

            // Get drops for this boss
            var drops = GetNPCDrops(npc);
            foreach (var drop in drops)
            {
              if (drop.ItemType > 0 && drop.ItemType < ItemLoader.ItemCount)
              {
                if (!itemToBossDrops.ContainsKey(drop.ItemType))
                  itemToBossDrops[drop.ItemType] = new List<BossDropInfo>();

                itemToBossDrops[drop.ItemType].Add(new BossDropInfo
                {
                  BossName = npc.FullName,
                  BossType = npc.type,
                  BossModName = modName,
                  DropChance = drop.DropChance,
                  MinStack = drop.MinStack,
                  MaxStack = drop.MaxStack,
                  Conditions = drop.Conditions
                });
              }
            }
          }
        }
        catch (Exception)
        {
          // Skip NPCs that cause errors during initialization
          continue;
        }
      }
    }

    private bool IsMiniBoss(NPC npc)
    {
      try
      {
        // Define criteria for mini-bosses (high HP, rare spawns, etc.)
        return npc.lifeMax > 1000 &&
               (npc.rarity > 0 || npc.value > 10000) &&
               !npc.townNPC &&
               !npc.friendly;
      }
      catch
      {
        return false;
      }
    }

    private bool HasBossLoot(NPC npc)
    {
      // Check if NPC has boss-tier loot (high value drops, boss bags, etc.)
      try
      {
        var drops = GetNPCDrops(npc);
        return drops.Any(d => d.ItemType > 0 && d.ItemType < ItemLoader.ItemCount);
      }
      catch
      {
        return false;
      }
    }

    private List<DropInfo> GetNPCDrops(NPC npc)
    {
      var drops = new List<DropInfo>();

      try
      {
        // Try to get the NPC's loot rules - this may not work for all tML versions
        var npcLoot = Main.ItemDropsDB?.GetRulesForNPCID(npc.type, false);

        if (npcLoot != null)
        {
          foreach (var rule in npcLoot)
          {
            ExtractDropsFromRule(rule, drops);
          }
        }
      }
      catch (Exception ex)
      {
        // Silently handle exceptions - some NPCs may not have accessible drop data
      }

      return drops;
    }

    private void ExtractDropsFromRule(IItemDropRule rule, List<DropInfo> drops)
    {
      try
      {
        // Handle different types of drop rules
        if (rule is CommonDrop commonDrop)
        {
          drops.Add(new DropInfo
          {
            ItemType = commonDrop.itemId,
            DropChance = commonDrop.chanceDenominator > 0 ? (float)(1.0 / commonDrop.chanceDenominator) : 1.0f,
            MinStack = commonDrop.amountDroppedMinimum,
            MaxStack = commonDrop.amountDroppedMaximum,
            Conditions = GetConditionNames(rule)
          });
        }
        else if (rule is ItemDropWithConditionRule conditionalDrop)
        {
          drops.Add(new DropInfo
          {
            ItemType = conditionalDrop.itemId,
            DropChance = conditionalDrop.chanceDenominator > 0 ? (float)(1.0 / conditionalDrop.chanceDenominator) : 1.0f,
            MinStack = conditionalDrop.amountDroppedMinimum,
            MaxStack = conditionalDrop.amountDroppedMaximum,
            Conditions = GetConditionNames(rule)
          });
        }

        // Recursively handle chained rules
        if (rule.ChainedRules != null)
        {
          foreach (var chainedRule in rule.ChainedRules)
          {
            ExtractDropsFromRule(chainedRule.RuleToChain, drops);
          }
        }
      }
      catch (Exception ex)
      {
        // Handle exceptions for individual rules
      }
    }

    private List<string> GetConditionNames(IItemDropRule rule)
    {
      var conditions = new List<string>();

      // Extract condition information if available
      // This is a simplified approach - you might need to expand based on specific conditions
      if (rule.ToString().Contains("Expert"))
        conditions.Add("Expert Mode");
      if (rule.ToString().Contains("Master"))
        conditions.Add("Master Mode");

      return conditions;
    }

    private List<BossDropInfo> GetBossDrops(int itemType)
    {
      return itemToBossDrops.ContainsKey(itemType) ? itemToBossDrops[itemType] : new List<BossDropInfo>();
    }

    private string DetermineObtainMethod(List<object> recipes, List<BossDropInfo> bossDrops)
    {
      if (bossDrops.Count > 0 && recipes.Count > 0)
        return "boss_drop_and_crafted";
      else if (bossDrops.Count > 0)
        return "boss_drop";
      else if (recipes.Count > 0)
        return "crafted";
      else
        return "other"; // Found, bought, fished, etc.
    }

    private string ClassifyItem(Item item)
    {
      if (item.accessory) return "accessory";
      if (item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1) return "armor";
      if (item.damage > 0) return "weapon";
      if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return "tool";
      if (item.createTile == TileID.MythrilAnvil || item.createTile == TileID.AdamantiteForge) return "crafting station";
      if (item.type.ToString().ToLower().Contains("ore")) return "ore";
      if (item.material) return "material";
      return "misc";
    }

    private string GetItemClass(Item item)
    {
      if (item.CountsAsClass(DamageClass.Melee)) return "melee";
      if (item.CountsAsClass(DamageClass.Ranged)) return "ranged";
      if (item.CountsAsClass(DamageClass.Magic)) return "magic";
      if (item.CountsAsClass(DamageClass.Summon)) return "summon";
      if (item.ModItem?.Mod?.Name == "CalamityMod" && item.Name.Contains("rogue", StringComparison.OrdinalIgnoreCase)) return "rogue";
      return "none";
    }

    private string GetSetBonus(Item item)
    {
      var player = new Player();
      player.armor[0] = item;
      item.ModItem?.UpdateArmorSet(player);
      return player.setBonus;
    }

    private List<object> GetRecipes(int itemType)
    {
      List<object> recipes = new();
      foreach (var recipe in Main.recipe)
      {
        if (recipe?.createItem?.type == itemType)
        {
          recipes.Add(new
          {
            Ingredients = recipe.requiredItem?.ConvertAll(i => new { i.type, i.stack }),
            Tiles = recipe.requiredTile,
            ResultStack = recipe.createItem.stack
          });
        }
      }
      return recipes;
    }
  }

  public class BossInfo
  {
    public string Name { get; set; }
    public int Type { get; set; }
    public string ModName { get; set; }
    public bool IsBoss { get; set; }
    public bool IsMiniBoss { get; set; }
    public int MaxLife { get; set; }
  }

  public class BossDropInfo
  {
    public string BossName { get; set; }
    public int BossType { get; set; }
    public string BossModName { get; set; }
    public float DropChance { get; set; }
    public int MinStack { get; set; }
    public int MaxStack { get; set; }
    public List<string> Conditions { get; set; } = new();
  }

  public class DropInfo
  {
    public int ItemType { get; set; }
    public float DropChance { get; set; }
    public int MinStack { get; set; }
    public int MaxStack { get; set; }
    public List<string> Conditions { get; set; } = new();
  }
}