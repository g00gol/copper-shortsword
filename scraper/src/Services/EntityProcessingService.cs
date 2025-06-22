using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using ItemDataExporter.Models;

namespace ItemDataExporter.Services
{
  public class EntityProcessingService
  {
    public List<EntityData> ProcessAllEntities()
    {
      var entities = new List<EntityData>();

      // Iterate through all NPC types
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
            continue; string modName = npc.ModNPC?.Mod?.Name?.ToLower() ?? "vanilla";
          string entityType = DetermineEntityType(npc);
          var lootTable = BuildLootTable(npcType); entities.Add(new EntityData
          {
            ID = npcType,
            Name = npc.FullName,
            Sprite = npcType,
            Type = entityType,
            Life = npc.lifeMax,
            Rarity = npc.rarity,
            IsBoss = npc.boss,
            IsTownNPC = npc.townNPC,
            LootTable = lootTable,
            Mod = modName
          });
        }
        catch
        {
          // Skip NPCs that cause issues during SetDefaults
        }
      }

      return entities;
    }
    private List<LootDrop> BuildLootTable(int npcType)
    {
      var lootTable = new List<LootDrop>();

      try
      {
        // Get the NPC's loot rules from the drop database
        var npcLoot = Main.ItemDropsDB.GetRulesForNPCID(npcType, false);
        if (npcLoot != null)
        {
          foreach (var rule in npcLoot)
          {
            ExtractLootFromRule(rule, lootTable);
          }
        }
      }
      catch
      {
        // Skip NPCs that cause issues with loot retrieval
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
          var item = new Item();
          item.SetDefaults(commonDrop.itemId);
          if (!string.IsNullOrEmpty(item.Name))
          {
            lootTable.Add(new LootDrop
            {
              ID = commonDrop.itemId,
              Name = item.Name,
              DropChance = 1.0f / commonDrop.chanceDenominator,
              MinStack = commonDrop.amountDroppedMinimum,
              MaxStack = commonDrop.amountDroppedMaximum
            });
          }
        }
        else if (rule is ItemDropWithConditionRule conditionalDrop)
        {
          var item = new Item();
          item.SetDefaults(conditionalDrop.itemId);
          if (!string.IsNullOrEmpty(item.Name))
          {
            lootTable.Add(new LootDrop
            {
              ID = conditionalDrop.itemId,
              Name = item.Name,
              DropChance = 1.0f / conditionalDrop.chanceDenominator,
              MinStack = conditionalDrop.amountDroppedMinimum,
              MaxStack = conditionalDrop.amountDroppedMaximum
            });
          }
        }
        else if (rule is OneFromOptionsNotScaledWithLuckDropRule oneFromOptions)
        {
          float chancePerItem = 1.0f / (oneFromOptions.chanceDenominator * oneFromOptions.dropIds.Length);
          foreach (int itemId in oneFromOptions.dropIds)
          {
            var item = new Item();
            item.SetDefaults(itemId);
            if (!string.IsNullOrEmpty(item.Name))
            {
              lootTable.Add(new LootDrop
              {
                ID = itemId,
                Name = item.Name,
                DropChance = chancePerItem,
                MinStack = 1,
                MaxStack = 1
              });
            }
          }
        }
        else if (rule is OneFromOptionsDropRule oneFromOptionsRegular)
        {
          float chancePerItem = 1.0f / (oneFromOptionsRegular.chanceDenominator * oneFromOptionsRegular.dropIds.Length);
          foreach (int itemId in oneFromOptionsRegular.dropIds)
          {
            var item = new Item();
            item.SetDefaults(itemId);
            if (!string.IsNullOrEmpty(item.Name))
            {
              lootTable.Add(new LootDrop
              {
                ID = itemId,
                Name = item.Name,
                DropChance = chancePerItem,
                MinStack = 1,
                MaxStack = 1
              });
            }
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

    private string DetermineEntityType(NPC npc)
    {
      if (npc.boss)
        return "boss";
      if (npc.townNPC)
        return "npc";
      if (npc.friendly)
        return "critter";
      if (npc.lifeMax > 100 && npc.damage > 30)
        return "elite";

      return "mob";
    }
  }
}
