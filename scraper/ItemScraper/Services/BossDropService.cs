using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using ItemScraper.Models;

namespace ItemScraper.Services
{
  public class BossDropService
  {
    private readonly Dictionary<int, List<BossDropInfo>> itemToBossDrops = new();
    private readonly Dictionary<string, List<BossInfo>> modToBosses = new();

    public void CollectBossData()
    {
      itemToBossDrops.Clear();
      modToBosses.Clear();

      for (int npcType = 1; npcType < NPCLoader.NPCCount; npcType++)
      {
        try
        {
          NPC npc = new();
          npc.SetDefaults(npcType);

          if (string.IsNullOrWhiteSpace(npc.FullName) || npc.type <= 0) continue;

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
        catch { continue; }
      }
    }

    public bool IsMiniBoss(NPC npc)
    {
      try
      {
        return npc.lifeMax > 1000 && (npc.rarity > 0 || npc.value > 10000) && !npc.townNPC && !npc.friendly;
      }
      catch { return false; }
    }

    private List<DropInfo> GetNPCDrops(NPC npc)
    {
      var drops = new List<DropInfo>();
      try
      {
        var npcLoot = Main.ItemDropsDB?.GetRulesForNPCID(npc.type, false);
        if (npcLoot != null)
        {
          foreach (var rule in npcLoot)
            ExtractDropsFromRule(rule, drops);
        }
      }
      catch { }
      return drops;
    }

    private void ExtractDropsFromRule(IItemDropRule rule, List<DropInfo> drops)
    {
      try
      {
        if (rule is CommonDrop c)
          drops.Add(new DropInfo { ItemType = c.itemId, DropChance = 1f / c.chanceDenominator, MinStack = c.amountDroppedMinimum, MaxStack = c.amountDroppedMaximum, Conditions = GetConditionNames(rule) });
        else if (rule is ItemDropWithConditionRule cond)
          drops.Add(new DropInfo { ItemType = cond.itemId, DropChance = 1f / cond.chanceDenominator, MinStack = cond.amountDroppedMinimum, MaxStack = cond.amountDroppedMaximum, Conditions = GetConditionNames(rule) });

        foreach (var chained in rule.ChainedRules ?? new List<ChainedItemDropRule>())
          ExtractDropsFromRule(chained.RuleToChain, drops);
      }
      catch { }
    }

    private List<string> GetConditionNames(IItemDropRule rule)
    {
      var list = new List<string>();
      if (rule.ToString().Contains("Expert")) list.Add("Expert Mode");
      if (rule.ToString().Contains("Master")) list.Add("Master Mode");
      return list;
    }

    public List<BossDropInfo> GetBossDrops(int itemType) => itemToBossDrops.ContainsKey(itemType) ? itemToBossDrops[itemType] : new();
    public List<BossInfo> GetBosses(string mod) => modToBosses.ContainsKey(mod) ? modToBosses[mod] : new();
    public bool HasBossData(string mod) => modToBosses.ContainsKey(mod);
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