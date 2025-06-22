using System.Linq;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using ItemDataExporter.Services;

namespace ItemDataExporter.Services
{
  public class ObtainMethodService
  {
    private readonly RecipeService _recipeService;
    private Dictionary<int, List<int>> _npcLootCache;

    public ObtainMethodService(RecipeService recipeService)
    {
      _recipeService = recipeService;
      _npcLootCache = new Dictionary<int, List<int>>();
      BuildNpcLootCache();
    }
    public string DetermineObtainMethod(Item item)
    {
      var recipes = _recipeService.GetRecipes(item.type);

      // Has recipes - it's crafted
      if (recipes.Count > 0) return "crafting";

      // Check if it's likely a mob drop
      if (IsMobDrop(item)) return "mob_drop";

      // Default for items we can't categorize
      return "unknown";
    }

    public int GetMobDropId(Item item)
    {
      // Check if any NPC drops this item and return the first one found
      foreach (var kvp in _npcLootCache)
      {
        if (kvp.Value.Contains(item.type))
        {
          return kvp.Key; // Return the NPC ID
        }
      }

      return 0; // No specific mob found
    }

    private void BuildNpcLootCache()
    {
      try
      {
        // Iterate through all NPC types
        for (int npcType = 1; npcType < NPCLoader.NPCCount; npcType++)
        {
          try
          {
            var npc = new NPC();
            npc.SetDefaults(npcType);

            // Skip invalid NPCs or town NPCs
            if (string.IsNullOrEmpty(npc.FullName) || npc.townNPC || npc.friendly)
              continue;

            var itemDrops = new List<int>();

            // Get the NPC's loot rules
            var npcLoot = Main.ItemDropsDB.GetRulesForNPCID(npcType, false);
            if (npcLoot != null)
            {
              foreach (var rule in npcLoot)
              {
                ExtractItemsFromRule(rule, itemDrops);
              }
            }            // Note: For modded NPCs, we can't easily access their loot tables directly
            // without instantiating a temporary NPCLoot and calling ModifyNPCLoot
            // This is a limitation of the current approach

            if (itemDrops.Count > 0)
            {
              _npcLootCache[npcType] = itemDrops.Distinct().ToList();
            }
          }
          catch
          {
            // Skip NPCs that cause issues
          }
        }
      }
      catch (System.Exception ex)
      {
        // If there are any issues building the cache, we'll fall back to the old method
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Warn($"Error building NPC loot cache: {ex.Message}");
      }
    }

    private void ExtractItemsFromRule(IItemDropRule rule, List<int> itemDrops)
    {
      try
      {
        // Handle common drop rule types
        if (rule is CommonDrop commonDrop)
        {
          itemDrops.Add(commonDrop.itemId);
        }
        else if (rule is ItemDropWithConditionRule conditionalDrop)
        {
          itemDrops.Add(conditionalDrop.itemId);
        }
        else if (rule is OneFromOptionsNotScaledWithLuckDropRule oneFromOptions)
        {
          itemDrops.AddRange(oneFromOptions.dropIds);
        }
        else if (rule is OneFromOptionsDropRule oneFromOptionsRegular)
        {
          itemDrops.AddRange(oneFromOptionsRegular.dropIds);
        }
        else if (rule is DropBasedOnExpertMode expertDrop)
        {
          ExtractItemsFromRule(expertDrop.ruleForNormalMode, itemDrops);
          ExtractItemsFromRule(expertDrop.ruleForExpertMode, itemDrops);
        }
        else if (rule is DropBasedOnMasterMode masterDrop)
        {
          ExtractItemsFromRule(masterDrop.ruleForDefault, itemDrops);
          ExtractItemsFromRule(masterDrop.ruleForMasterMode, itemDrops);
        }

        // Handle chained rules
        if (rule.ChainedRules != null)
        {
          foreach (var chainedRule in rule.ChainedRules)
          {
            ExtractItemsFromRule(chainedRule.RuleToChain, itemDrops);
          }
        }
      }
      catch
      {
        // Skip rules that cause issues
      }
    }

    private bool IsMobDrop(Item item)
    {
      // First check if the item has no recipes
      var recipes = _recipeService.GetRecipes(item.type);
      if (recipes.Count > 0) return false;

      // Check if any NPC drops this item
      foreach (var npcLoot in _npcLootCache.Values)
      {
        if (npcLoot.Contains(item.type))
        {
          return true;
        }
      }

      // Fallback to pattern-based detection for items not in loot tables
      return IsMobDropFallback(item);
    }
    private bool IsMobDropFallback(Item item)
    {
      // Check for common mob drop patterns
      string itemName = item.Name.ToLower();
      var mobDropPatterns = new[]
      {
        "banner", "trophy", "mask", "dye", "soul", "essence",
        "scale", "horn", "fang", "claw", "eye", "heart", "brain",
        "relic", "treasure bag", "expert", "master"
      };

      if (mobDropPatterns.Any(pattern => itemName.Contains(pattern)))
        return true;

      // Items with higher rarity and no recipes are likely mob drops
      if (item.rare >= 3) return true;

      // Weapons, accessories, and armor with no recipes and decent rarity are likely drops
      if ((item.damage > 0 || item.accessory || item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1) && item.rare >= 2)
        return true;

      // Items that sell for a decent amount but have no recipes might be drops
      if (item.value >= Item.sellPrice(0, 2, 0, 0) && item.rare >= 1)
        return true;

      return false;
    }

    public List<string> GetNpcsThatDropItem(int itemType)
    {
      var npcs = new List<string>();

      foreach (var kvp in _npcLootCache)
      {
        if (kvp.Value.Contains(itemType))
        {
          try
          {
            var npc = new NPC();
            npc.SetDefaults(kvp.Key);
            if (!string.IsNullOrEmpty(npc.FullName))
            {
              npcs.Add(npc.FullName);
            }
          }
          catch
          {
            // Skip NPCs that cause issues
          }
        }
      }
      return npcs;
    }

    public int GetNpcLootCacheCount()
    {
      return _npcLootCache.Count;
    }

    public int GetTotalCachedDrops()
    {
      return _npcLootCache.Values.Sum(list => list.Count);
    }

    public int GetFirstNpcThatDropsItem(int itemType)
    {
      foreach (var kvp in _npcLootCache)
      {
        if (kvp.Value.Contains(itemType))
        {
          return kvp.Key; // Return the first NPC ID that drops this item
        }
      }

      return 0; // No NPC found that drops this item
    }
  }
}
