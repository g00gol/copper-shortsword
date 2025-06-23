using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using ItemDataExporter.Models;

namespace ItemDataExporter.Services
{
  public class TreasureBagService
  {
    private Dictionary<int, int> _itemToTreasureBagMap; // item ID -> treasure bag ID
    private Dictionary<int, List<LootDrop>> _treasureBagContents; // treasure bag ID -> loot table

    public TreasureBagService()
    {
      _itemToTreasureBagMap = new Dictionary<int, int>();
      _treasureBagContents = new Dictionary<int, List<LootDrop>>();
      BuildTreasureBagCache();
    }

    public bool IsFromTreasureBag(int itemId, out int treasureBagId)
    {
      // print itemId and treasureBagId for debugging
      ModContent.GetInstance<ItemDataExporter>()?.Logger?.Debug($"Checking item {itemId} for treasure bag mapping");
      var inMap = _itemToTreasureBagMap.TryGetValue(itemId, out treasureBagId);
      if (inMap)
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Debug($"Item {itemId} maps to treasure bag {treasureBagId}");
      }
      else
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Debug($"Item {itemId} does not map to any treasure bag");
      }
      return _itemToTreasureBagMap.TryGetValue(itemId, out treasureBagId);
    }

    public List<LootDrop> GetTreasureBagContents(int treasureBagId)
    {
      return _treasureBagContents.TryGetValue(treasureBagId, out var contents) ? contents : new List<LootDrop>();
    }

    public Dictionary<int, List<int>> GetAllTreasureBagMappings()
    {
      var result = new Dictionary<int, List<int>>();

      foreach (var bagId in _treasureBagContents.Keys)
      {
        var items = _itemToTreasureBagMap
            .Where(kvp => kvp.Value == bagId)
            .Select(kvp => kvp.Key)
            .ToList();

        if (items.Count > 0)
        {
          result[bagId] = items;
        }
      }

      return result;
    }

    public void LogTreasureBagContents()
    {
      try
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info("=== Treasure Bag Contents Debug ===");

        foreach (var bagKvp in _treasureBagContents)
        {
          var bagItem = new Item();
          bagItem.SetDefaults(bagKvp.Key);

          ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info($"Treasure Bag: {bagItem.Name} (ID: {bagKvp.Key})");

          foreach (var loot in bagKvp.Value)
          {
            var lootItem = new Item();
            lootItem.SetDefaults(loot.ID);
            ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info($"  - Contains: {lootItem.Name} (ID: {loot.ID}) - Chance: {loot.DropChance:P2}");
          }
        }

        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info("=== End Treasure Bag Contents ===");
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Error($"Error logging treasure bag contents: {ex.Message}");
      }
    }
    private void BuildTreasureBagCache()
    {
      try
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info("Building treasure bag cache...");

        for (int itemType = 1; itemType < ItemLoader.ItemCount; itemType++)
        {
          try
          {
            // Check if this item is a treasure bag
            if (IsTreasureBag(itemType))
            {
              var treasureBagContents = ExtractTreasureBagContents(itemType);
              _treasureBagContents[itemType] = treasureBagContents;

              // Map each item in the treasure bag back to the bag
              foreach (var lootDrop in treasureBagContents)
              {
                if (!_itemToTreasureBagMap.ContainsKey(lootDrop.ID))
                {
                  _itemToTreasureBagMap[lootDrop.ID] = itemType;
                }
              }

              // Log treasure bag info
              var item = new Item();
              item.SetDefaults(itemType);
              ModContent.GetInstance<ItemDataExporter>()?.Logger?.Debug(
                  $"Found treasure bag: {item.Name} (ID: {itemType}) with {treasureBagContents.Count} items"
              );
            }
          }
          catch
          {
            // Skip items that cause issues
          }
        }

        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Info(
            $"Treasure bag cache built: {_treasureBagContents.Count} bags, {_itemToTreasureBagMap.Count} item mappings"
        );
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>()?.Logger?.Error($"Error building treasure bag cache: {ex.Message}");
      }
    }

    private bool IsTreasureBag(int itemType)
    {
      try
      {
        // Check ItemID.Sets.BossBag first
        if (itemType < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[itemType])
        {
          return true;
        }

        // Also check by name pattern as a fallback
        var item = new Item();
        item.SetDefaults(itemType);

        if (string.IsNullOrEmpty(item.Name))
          return false;

        var itemName = item.Name.ToLower();
        return itemName.Contains("treasure bag") || itemName.Contains("boss bag");
      }
      catch
      {
        return false;
      }
    }

    private List<LootDrop> ExtractTreasureBagContents(int treasureBagType)
    {
      var lootTable = new List<LootDrop>();

      try
      {
        // Create item instance to access ModifyItemLoot
        var item = new Item();
        item.SetDefaults(treasureBagType);

        // Get the item's loot rules
        var itemLoot = Main.ItemDropsDB.GetRulesForItemID(treasureBagType);
        if (itemLoot != null)
        {
          foreach (var rule in itemLoot)
          {
            ExtractLootFromRule(rule, lootTable);
          }
        }
      }
      catch
      {
        // Skip treasure bags that cause issues
      }

      return lootTable;
    }

    private void ExtractLootFromRule(IItemDropRule rule, List<LootDrop> lootTable)
    {
      try
      {
        // Handle common drop rule types
        if (rule is CommonDrop commonDrop)
        {
          AddLootDrop(lootTable, commonDrop.itemId, 1.0f / commonDrop.chanceDenominator,
                     commonDrop.amountDroppedMinimum, commonDrop.amountDroppedMaximum);
        }
        else if (rule is ItemDropWithConditionRule conditionalDrop)
        {
          AddLootDrop(lootTable, conditionalDrop.itemId, 1.0f / conditionalDrop.chanceDenominator,
                     conditionalDrop.amountDroppedMinimum, conditionalDrop.amountDroppedMaximum);
        }
        else if (rule is OneFromOptionsNotScaledWithLuckDropRule oneFromOptions)
        {
          float chancePerItem = 1.0f / (oneFromOptions.chanceDenominator * oneFromOptions.dropIds.Length);
          foreach (int itemId in oneFromOptions.dropIds)
          {
            AddLootDrop(lootTable, itemId, chancePerItem, 1, 1);
          }
        }
        else if (rule is OneFromOptionsDropRule oneFromOptionsRegular)
        {
          float chancePerItem = 1.0f / (oneFromOptionsRegular.chanceDenominator * oneFromOptionsRegular.dropIds.Length);
          foreach (int itemId in oneFromOptionsRegular.dropIds)
          {
            AddLootDrop(lootTable, itemId, chancePerItem, 1, 1);
          }
        }
        else if (rule is DropBasedOnExpertMode expertDrop)
        {
          ExtractLootFromRule(expertDrop.ruleForNormalMode, lootTable);
          ExtractLootFromRule(expertDrop.ruleForExpertMode, lootTable);
        }
        else if (rule is DropBasedOnMasterMode masterDrop)
        {
          ExtractLootFromRule(masterDrop.ruleForDefault, lootTable);
          ExtractLootFromRule(masterDrop.ruleForMasterMode, lootTable);
        }

        // Handle chained rules
        if (rule.ChainedRules != null)
        {
          foreach (var chainedRule in rule.ChainedRules)
          {
            ExtractLootFromRule(chainedRule.RuleToChain, lootTable);
          }
        }
      }
      catch
      {
        // Skip rules that cause issues
      }
    }

    private void AddLootDrop(List<LootDrop> lootTable, int itemId, float dropChance, int minStack, int maxStack)
    {
      try
      {
        var item = new Item();
        item.SetDefaults(itemId);

        if (!string.IsNullOrEmpty(item.Name))
        {
          lootTable.Add(new LootDrop
          {
            ID = itemId,
            Name = item.Name,
            DropChance = dropChance,
            MinStack = minStack,
            MaxStack = maxStack
          });
        }
      }
      catch
      {
        // Skip items that cause issues
      }
    }
  }
}
