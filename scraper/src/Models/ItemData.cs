using System.Collections.Generic;
using Newtonsoft.Json;

namespace ItemDataExporter.Models
{
  public class ItemExportData
  {
    public List<WeaponData> Weapons { get; set; } = new List<WeaponData>();
    public List<ToolData> Tools { get; set; } = new List<ToolData>();
    public List<ArmorData> Armor { get; set; } = new List<ArmorData>();
    public List<ArmorSetData> ArmorSets { get; set; } = new List<ArmorSetData>();
    public List<AccessoryData> Accessories { get; set; } = new List<AccessoryData>();
    public List<EntityData> Entities { get; set; } = new List<EntityData>();
    public List<MiscData> Misc { get; set; } = new List<MiscData>();
    public List<SpriteData> Sprites { get; set; } = new List<SpriteData>();
  }

  public class WeaponData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Damage { get; set; }
    public float Knockback { get; set; }
    [JsonProperty("Critical chance")]
    public int CriticalChance { get; set; }
    [JsonProperty("Use time")]
    public int UseTime { get; set; }
    public string Tooltip { get; set; }
    public int Rarity { get; set; }
    public int Sprite { get; set; }
    public string Description { get; set; }
    public ObtainedByData ObtainedBy { get; set; }
    public List<object> Recipes { get; set; }
    public string Type { get; set; }
    public string Mod { get; set; }
  }

  public class ToolData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Rarity { get; set; }
    public int Sprite { get; set; }
    public List<string> Types { get; set; }
    public string Description { get; set; }
    public ObtainedByData ObtainedBy { get; set; }
    public List<object> Recipes { get; set; }
    public string Mod { get; set; }
  }

  public class ArmorData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Defense { get; set; }
    public int Rarity { get; set; }
    public int Sprite { get; set; }
    public string BodySlot { get; set; }
    public string Description { get; set; }
    public ObtainedByData ObtainedBy { get; set; }
    public List<object> Recipes { get; set; }
    public string Mod { get; set; }
  }

  public class ArmorSetData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int TotalDefense { get; set; }
    public string SetBonus { get; set; }
    public int Rarity { get; set; }
    public string Description { get; set; }
    public string Mod { get; set; }
  }

  public class AccessoryData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Sprite { get; set; }
    public int Rarity { get; set; }
    public string Description { get; set; }
    public ObtainedByData ObtainedBy { get; set; }
    public List<object> Recipes { get; set; }
    public string Mod { get; set; }
  }
  public class EntityData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Sprite { get; set; }
    public string Type { get; set; }
    public int Life { get; set; }
    public int Rarity { get; set; }
    public bool IsBoss { get; set; }
    public bool IsTownNPC { get; set; }
    public List<LootDrop> LootTable { get; set; }
    public string Mod { get; set; }
  }

  public class LootDrop
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public float DropChance { get; set; }
    public int MinStack { get; set; }
    public int MaxStack { get; set; }
  }

  public class MiscData
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public int Sprite { get; set; }
    public ObtainedByData ObtainedBy { get; set; }
    public List<object> Recipes { get; set; }
    public string Mod { get; set; }
  }

  public class SpriteData
  {
    public int ID { get; set; }
    public string Mod { get; set; }
  }

  public class ObtainedByData
  {
    public string Method { get; set; }
    public int ID { get; set; }
  }
}
