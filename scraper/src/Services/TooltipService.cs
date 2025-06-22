using System.Collections.Generic;
using Terraria;

namespace ItemDataExporter.Services
{
  public class TooltipService
  {
    public string GetFirstTooltipLine(Item item)
    {
      try
      {
        if (item.ToolTip != null && item.ToolTip.Lines > 0)
        {
          return item.ToolTip.GetLine(0);
        }
      }
      catch
      {
        // Fallback to name if tooltip fails
      }
      return item.Name;
    }

    public string GetFullTooltip(Item item)
    {
      try
      {
        if (item.ToolTip != null && item.ToolTip.Lines > 0)
        {
          var lines = new List<string>();
          for (int i = 0; i < item.ToolTip.Lines; i++)
          {
            string line = item.ToolTip.GetLine(i);
            if (!string.IsNullOrEmpty(line))
            {
              lines.Add(line);
            }
          }
          return string.Join("\n", lines);
        }
      }
      catch
      {
        // Fallback if tooltip fails
      }
      return item.Name;
    }
  }
}
