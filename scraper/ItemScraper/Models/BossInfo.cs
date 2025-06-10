namespace ItemScraper.Models
{
  public class BossInfo
  {
    public string Name { get; set; }
    public int Type { get; set; }
    public string ModName { get; set; }
    public bool IsBoss { get; set; }
    public bool IsMiniBoss { get; set; }
    public int MaxLife { get; set; }
  }
}