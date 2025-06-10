using System.Collections.Generic;

namespace ItemScraper.Models
{
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
}
