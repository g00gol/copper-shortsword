using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ItemDataExporter.Models;
using ItemDataExporter.Services;

namespace ItemDataExporter.Services
{
  public class ItemProcessingService
  {
    private readonly RecipeService _recipeService;
    private readonly ObtainMethodService _obtainMethodService;
    private readonly TooltipService _tooltipService;

    public ItemProcessingService(RecipeService recipeService, ObtainMethodService obtainMethodService, TooltipService tooltipService)
    {
      _recipeService = recipeService;
      _obtainMethodService = obtainMethodService;
      _tooltipService = tooltipService;
    }

    public void ProcessItem(Item item, ItemExportData exportData, string modName)
    {
      try
      {
        string itemType = DetermineItemType(item);
        string description = _tooltipService.GetFullTooltip(item);

        switch (itemType)
        {
          case "weapon":
            exportData.Weapons.Add(CreateWeaponData(item, modName, description));
            break;
          case "tool":
            exportData.Tools.Add(CreateToolData(item, modName, description));
            break;
          case "armor":
            exportData.Armor.Add(CreateArmorData(item, modName, description));
            break;
          case "accessory":
            exportData.Accessories.Add(CreateAccessoryData(item, modName, description));
            break;
          case "misc":
            exportData.Misc.Add(CreateMiscData(item, modName));
            break;
        }

        // Add sprite data
        exportData.Sprites.Add(new SpriteData
        {
          ID = item.type,
          Mod = modName
        });
      }
      catch (System.Exception ex)
      {
        // Logger will be passed from the calling context
        throw new System.Exception($"Failed to process item {item.type} ({item.Name}): {ex.Message}", ex);
      }
    }
    private WeaponData CreateWeaponData(Item item, string modName, string description)
    {
      var recipes = _recipeService.GetRecipes(item.type);
      var obtainMethod = _obtainMethodService.DetermineObtainMethod(item);
      var obtainId = GetObtainId(item, obtainMethod);

      return new WeaponData
      {
        ID = item.type,
        Name = item.Name,
        Damage = item.damage,
        Knockback = item.knockBack,
        CriticalChance = item.crit,
        UseTime = item.useTime,
        Tooltip = _tooltipService.GetFirstTooltipLine(item),
        Rarity = item.rare,
        Sprite = item.type,
        Description = description,
        ObtainedBy = new ObtainedByData
        {
          Method = obtainMethod,
          ID = obtainId
        },
        Recipes = recipes,
        Type = GetWeaponType(item),
        Mod = modName
      };
    }
    private int GetObtainId(Item item, string obtainMethod)
    {
      if (obtainMethod == "mob_drop" || obtainMethod == "treasure_bag")
      {
        return _obtainMethodService.GetMobDropId(item);
      }
      return 0; // For crafted items or unknown sources
    }

    private ToolData CreateToolData(Item item, string modName, string description)
    {
      var types = new List<string>();

      if (item.fishingPole > 0) types.Add("fishing");
      if (item.hammer > 0) types.Add("hammer");
      if (item.pick > 0) types.Add("pickaxe");
      if (item.axe > 0) types.Add("axe");
      if (item.tileWand > 0) types.Add("wiring");
      if (item.paint > 0) types.Add("painting");
      if (types.Count == 0) types.Add("misc");

      var recipes = _recipeService.GetRecipes(item.type);
      var obtainMethod = _obtainMethodService.DetermineObtainMethod(item);
      var obtainId = GetObtainId(item, obtainMethod);

      return new ToolData
      {
        ID = item.type,
        Name = item.Name,
        Rarity = item.rare,
        Sprite = item.type,
        Types = types,
        Description = description,
        ObtainedBy = new ObtainedByData
        {
          Method = obtainMethod,
          ID = obtainId
        },
        Recipes = recipes,
        Mod = modName
      };
    }

    private ArmorData CreateArmorData(Item item, string modName, string description)
    {
      var recipes = _recipeService.GetRecipes(item.type);
      var obtainMethod = _obtainMethodService.DetermineObtainMethod(item);
      var obtainId = GetObtainId(item, obtainMethod);

      return new ArmorData
      {
        ID = item.type,
        Name = item.Name,
        Defense = item.defense,
        Rarity = item.rare,
        Sprite = item.type,
        BodySlot = GetArmorSlot(item),
        Description = description,
        ObtainedBy = new ObtainedByData
        {
          Method = obtainMethod,
          ID = obtainId
        },
        Recipes = recipes,
        Mod = modName
      };
    }

    private AccessoryData CreateAccessoryData(Item item, string modName, string description)
    {
      var recipes = _recipeService.GetRecipes(item.type);
      var obtainMethod = _obtainMethodService.DetermineObtainMethod(item);
      var obtainId = GetObtainId(item, obtainMethod);

      return new AccessoryData
      {
        ID = item.type,
        Name = item.Name,
        Sprite = item.type,
        Rarity = item.rare,
        Description = description,
        ObtainedBy = new ObtainedByData
        {
          Method = obtainMethod,
          ID = obtainId
        },
        Recipes = recipes,
        Mod = modName
      };
    }

    private MiscData CreateMiscData(Item item, string modName)
    {
      var recipes = _recipeService.GetRecipes(item.type);
      var obtainMethod = _obtainMethodService.DetermineObtainMethod(item);
      var obtainId = GetObtainId(item, obtainMethod);

      return new MiscData
      {
        ID = item.type,
        Name = item.Name,
        Sprite = item.type,
        ObtainedBy = new ObtainedByData
        {
          Method = obtainMethod,
          ID = obtainId
        },
        Recipes = recipes,
        Mod = modName
      };
    }

    private string DetermineItemType(Item item)
    {
      // Check for weapons first - items with damage and not tools
      if (item.damage > 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0)
        return "weapon";

      // Check for tools
      if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.fishingPole > 0)
        return "tool";

      // Check for armor
      if (item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1)
        return "armor";

      // Check for accessories
      if (item.accessory)
        return "accessory";

      return "misc";
    }

    private string GetWeaponType(Item item)
    {
      if (item.DamageType == DamageClass.Melee) return "melee";
      if (item.DamageType == DamageClass.Ranged) return "ranged";
      if (item.DamageType == DamageClass.Magic) return "magic";
      if (item.DamageType == DamageClass.Summon || item.DamageType.Name.Contains("Summon")) return "summoner";
      // Check for throwing/rogue damage class from mods like Calamity
      if (item.DamageType.Name.ToLower().Contains("rogue") || item.DamageType.Name.ToLower().Contains("throwing")) return "rogue";
      return "melee";
    }
    private string GetArmorSlot(Item item)
    {
      if (item.headSlot > -1) return "helmet";
      if (item.bodySlot > -1) return "shirt";
      if (item.legSlot > -1) return "pants";
      return "unknown";
    }
  }
}
