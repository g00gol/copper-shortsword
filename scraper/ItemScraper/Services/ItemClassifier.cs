using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ItemScraper.Models;

namespace ItemScraper.Services
{
  public class ItemClassifier
  {
    private readonly BossDropService _bossDropService;

    public ItemClassifier(BossDropService bossDropService)
    {
      _bossDropService = bossDropService;
    }

    public string ClassifyItem(Item item)
    {
      if (item.accessory) return "accessory";
      if (item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1) return "armor";
      if (item.damage > 0) return "weapon";
      if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return "tool";
      if (item.createTile == TileID.MythrilAnvil || item.createTile == TileID.AdamantiteForge) return "station";
      if (item.potion) return "potion";
      if (item.type.ToString().ToLower().Contains("ore")) return "ore";
      if (item.material) return "material";
      return "misc";
    }

    public string GetWeaponSubType(Item item, string itemType)
    {
      if (itemType != "weapon") return null;
      if (item.shoot > 0 && item.useAmmo == AmmoID.Bullet) return "gun";
      if (item.shoot > 0 && item.magic) return "magic gun";
      if (item.channel && item.noMelee && item.noUseGraphic && item.useStyle == ItemUseStyleID.Shoot) return "yoyo";
      if (item.whip) return "whip";
      if (item.Name.ToLower().Contains("aesthetic")) return "aesthetic";
      return "other";
    }

    public string GetItemClass(Item item)
    {
      if (item.CountsAsClass(DamageClass.Melee)) return "melee";
      if (item.CountsAsClass(DamageClass.Ranged)) return "ranged";
      if (item.CountsAsClass(DamageClass.Magic)) return "magic";
      if (item.CountsAsClass(DamageClass.Summon)) return "summon";
      if (item.ModItem?.Mod?.Name == "CalamityMod" && item.Name.ToLower().Contains("rogue")) return "rogue";
      return "none";
    }

    public string GetSetBonus(Item item)
    {
      var player = new Player();
      player.armor[0] = item;
      item.ModItem?.UpdateArmorSet(player);
      return player.setBonus;
    }

    public string DetermineObtainMethod(List<object> recipes, List<BossDropInfo> bossDrops)
    {
      if (bossDrops.Count > 0 && recipes.Count > 0) return "boss_drop_and_crafted";
      if (bossDrops.Count > 0) return "boss_drop";
      if (recipes.Count > 0) return "crafted";
      return "other";
    }
  }
}