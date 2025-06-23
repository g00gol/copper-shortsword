using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using ReLogic.Content;

namespace ItemDataExporter.Services
{
  public class SpriteExtractionService
  {
    private readonly string _spritesDirectory;

    public SpriteExtractionService(string baseOutputDirectory)
    {
      _spritesDirectory = Path.Combine(baseOutputDirectory, "sprites");
      Directory.CreateDirectory(_spritesDirectory);
    }
    public void ExtractItemSprite(int itemId)
    {
      try
      {
        string fileName = $"item_{itemId}.png";
        string filePath = Path.Combine(_spritesDirectory, fileName);

        // Don't overwrite existing files
        if (File.Exists(filePath))
        {
          return;
        }

        // Skip invalid IDs
        if (itemId <= 0 || itemId >= ItemLoader.ItemCount)
        {
          return;
        }

        // Force load the item first to ensure textures are available
        Item item = new Item();
        item.SetDefaults(itemId);

        // Skip invalid items
        if (string.IsNullOrEmpty(item.Name) || item.type <= 0)
        {
          return;
        }        // Always ensure we're on the main thread for texture operations
        Main.RunOnMainThread(() => ExtractItemSpriteInternal(itemId, fileName, item)).GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract sprite for item {itemId}: {ex.Message}");
      }
    }

    private void ExtractItemSpriteInternal(int itemId, string fileName, Item item)
    {
      try
      {
        Texture2D texture = null;

        // For modded items, use ModContent directly
        if (item.ModItem != null)
        {
          try
          {
            if (!string.IsNullOrEmpty(item.ModItem.Texture))
            {
              var modTexture = ModContent.Request<Texture2D>(item.ModItem.Texture, AssetRequestMode.ImmediateLoad);
              if (modTexture != null && modTexture.IsLoaded)
              {
                texture = modTexture.Value;
              }
            }
          }
          catch
          {
            // Ignore and try next approach
          }
        }

        // For vanilla items, ensure texture is loaded before accessing
        if (texture == null && itemId >= 0 && itemId < TextureAssets.Item.Length)
        {
          try
          {
            // Force load the texture asset
            Main.instance.LoadItem(itemId);

            var asset = TextureAssets.Item[itemId];
            if (asset != null)
            {
              if (!asset.IsLoaded)
              {
                // Force load and wait
                asset.Wait();
              }

              if (asset.IsLoaded)
              {
                texture = asset.Value;
              }
            }
          }
          catch
          {
            // Ignore and try next approach
          }
        }

        // Try using the content manager directly as fallback
        if (texture == null)
        {
          try
          {
            string texturePath = $"Images/Item_{itemId}";
            var textureAsset = Main.Assets.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null)
            {
              textureAsset.Wait();
              if (textureAsset.IsLoaded)
              {
                texture = textureAsset.Value;
              }
            }
          }
          catch
          {
            // Final fallback failed
          }
        }

        // Validate texture before saving (we're already on main thread)
        if (texture != null && IsValidTexture(texture))
        {
          SaveTextureToFile(texture, Path.Combine(_spritesDirectory, fileName));
        }
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract sprite for item {itemId}: {ex.Message}");
      }
    }

    private bool IsValidTexture(Texture2D texture)
    {
      try
      {
        return texture != null &&
               texture.Width > 0 &&
               texture.Height > 0 &&
               texture.Width <= 4096 &&
               texture.Height <= 4096 &&
               !texture.IsDisposed;
      }
      catch
      {
        return false;
      }
    }
    private void SaveTextureToFile(Texture2D texture, string filePath)
    {
      try
      {
        // Double-check we don't overwrite
        if (File.Exists(filePath))
        {
          return;
        }

        // Validate texture one more time before saving
        if (!IsValidTexture(texture))
        {
          return;
        }

        // Create a temporary file first to prevent corruption
        string tempPath = filePath + ".tmp";

        try
        {
          using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
          {
            texture.SaveAsPng(fileStream, texture.Width, texture.Height);
            fileStream.Flush();
          }

          // Only move to final location if write was successful
          if (File.Exists(tempPath))
          {
            File.Move(tempPath, filePath);
          }
        }
        catch
        {
          // Clean up temp file if something went wrong
          if (File.Exists(tempPath))
          {
            try { File.Delete(tempPath); } catch { }
          }
          throw;
        }
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Error($"Failed to save texture to {filePath}: {ex.Message}");
        throw;
      }
    }
    public void ExtractNpcSprite(int npcId)
    {
      try
      {
        string fileName = $"npc_{npcId}.png";
        string filePath = Path.Combine(_spritesDirectory, fileName);

        // Don't overwrite existing files
        if (File.Exists(filePath))
        {
          return;
        }

        // Skip invalid IDs
        if (npcId <= 0 || npcId >= NPCLoader.NPCCount)
        {
          return;
        }

        // Create NPC instance to validate
        NPC npc = new NPC();
        npc.SetDefaults(npcId);

        // Skip invalid NPCs
        if (string.IsNullOrEmpty(npc.TypeName))
        {
          return;
        }        // Always ensure we're on the main thread for texture operations
        Main.RunOnMainThread(() => ExtractNpcSpriteInternal(npcId, fileName, npc)).GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract sprite for NPC {npcId}: {ex.Message}");
      }
    }

    private void ExtractNpcSpriteInternal(int npcId, string fileName, NPC npc)
    {
      try
      {
        Texture2D texture = null;

        // For modded NPCs, use ModContent directly
        if (npc.ModNPC != null)
        {
          try
          {
            if (!string.IsNullOrEmpty(npc.ModNPC.Texture))
            {
              var modTexture = ModContent.Request<Texture2D>(npc.ModNPC.Texture, AssetRequestMode.ImmediateLoad);
              if (modTexture != null && modTexture.IsLoaded)
              {
                texture = modTexture.Value;
              }
            }
          }
          catch
          {
            // Ignore and try next approach
          }
        }

        // For vanilla NPCs, ensure texture is loaded before accessing
        if (texture == null && npcId >= 0 && npcId < TextureAssets.Npc.Length)
        {
          try
          {
            // Force load the texture asset
            Main.instance.LoadNPC(npcId);

            var asset = TextureAssets.Npc[npcId];
            if (asset != null)
            {
              if (!asset.IsLoaded)
              {
                asset.Wait();
              }

              if (asset.IsLoaded)
              {
                texture = asset.Value;
              }
            }
          }
          catch
          {
            // Ignore and try next approach
          }
        }

        // Try using the content manager directly as fallback
        if (texture == null)
        {
          try
          {
            string texturePath = $"Images/NPC_{npcId}";
            var textureAsset = Main.Assets.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null)
            {
              textureAsset.Wait();
              if (textureAsset.IsLoaded)
              {
                texture = textureAsset.Value;
              }
            }
          }
          catch
          {
            // Final fallback failed
          }
        }

        // Validate texture before saving (we're already on main thread)
        if (texture != null && IsValidTexture(texture))
        {
          SaveTextureToFile(texture, Path.Combine(_spritesDirectory, fileName));
        }
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract sprite for NPC {npcId}: {ex.Message}");
      }
    }
    public void ExtractModdedItemSprite(Item item, string modName)
    {
      // Always run modded sprite extraction on main thread
      Main.RunOnMainThread(() => ExtractModdedItemSpriteInternal(item, modName)).GetAwaiter().GetResult();
    }

    private void ExtractModdedItemSpriteInternal(Item item, string modName)
    {
      try
      {
        string fileName = $"item_{item.type}.png";
        string filePath = Path.Combine(_spritesDirectory, fileName);

        // Don't overwrite existing files
        if (File.Exists(filePath))
        {
          return;
        }

        var modItem = item.ModItem;
        if (modItem == null)
        {
          return;
        }

        Texture2D texture = null;

        // Try to get texture from ModContent using the texture path
        try
        {
          if (!string.IsNullOrEmpty(modItem.Texture))
          {
            var textureAsset = ModContent.Request<Texture2D>(modItem.Texture, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null && textureAsset.IsLoaded)
            {
              texture = textureAsset.Value;
            }
          }
        }
        catch
        {
          // Try alternative approaches
        }

        // Fallback: try constructing texture path from class name
        if (texture == null)
        {
          try
          {
            var texturePath = $"{modName}/Items/{modItem.GetType().Name}";
            var textureAsset = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null && textureAsset.IsLoaded)
            {
              texture = textureAsset.Value;
            }
          }
          catch
          {
            // Final fallback ignored
          }
        }

        if (texture != null && IsValidTexture(texture))
        {
          SaveTextureToFile(texture, Path.Combine(_spritesDirectory, fileName));
        }
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract modded sprite for item {item.type} from mod {modName}: {ex.Message}");
      }
    }

    public void ExtractModdedNpcSprite(NPC npc, string modName)
    {
      // Always run modded sprite extraction on main thread
      Main.RunOnMainThread(() => ExtractModdedNpcSpriteInternal(npc, modName)).GetAwaiter().GetResult();
    }

    private void ExtractModdedNpcSpriteInternal(NPC npc, string modName)
    {
      try
      {
        string fileName = $"npc_{npc.type}.png";
        string filePath = Path.Combine(_spritesDirectory, fileName);

        // Don't overwrite existing files
        if (File.Exists(filePath))
        {
          return;
        }

        var modNpc = npc.ModNPC;
        if (modNpc == null)
        {
          return;
        }

        Texture2D texture = null;

        // Try to get texture from ModContent using the texture path
        try
        {
          if (!string.IsNullOrEmpty(modNpc.Texture))
          {
            var textureAsset = ModContent.Request<Texture2D>(modNpc.Texture, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null && textureAsset.IsLoaded)
            {
              texture = textureAsset.Value;
            }
          }
        }
        catch
        {
          // Try alternative approaches
        }

        // Fallback: try constructing texture path from class name
        if (texture == null)
        {
          try
          {
            var texturePath = $"{modName}/NPCs/{modNpc.GetType().Name}";
            var textureAsset = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad);
            if (textureAsset != null && textureAsset.IsLoaded)
            {
              texture = textureAsset.Value;
            }
          }
          catch
          {
            // Final fallback ignored
          }
        }

        if (texture != null && IsValidTexture(texture))
        {
          SaveTextureToFile(texture, Path.Combine(_spritesDirectory, fileName));
        }
      }
      catch (Exception ex)
      {
        ModContent.GetInstance<ItemDataExporter>().Logger.Debug($"Failed to extract modded sprite for NPC {npc.type} from mod {modName}: {ex.Message}");
      }
    }
    public void ExtractAllVanillaItemSprites()
    {
      ModContent.GetInstance<ItemDataExporter>().Logger.Info("Starting vanilla item sprite extraction...");

      int extracted = 0;
      int total = 0;

      for (int i = 1; i < ItemLoader.ItemCount; i++)
      {
        total++;

        // Check if this is a vanilla item (no ModItem)
        var item = new Item();
        item.SetDefaults(i);

        if (item.ModItem == null && !string.IsNullOrEmpty(item.Name))
        {
          ExtractItemSprite(i);
          extracted++;

          if (extracted % 100 == 0)
          {
            ModContent.GetInstance<ItemDataExporter>().Logger.Info($"Extracted {extracted} item sprites so far...");
          }
        }
      }

      ModContent.GetInstance<ItemDataExporter>().Logger.Info($"Finished extracting {extracted} vanilla item sprites out of {total} total items.");
    }

    public void ExtractAllVanillaNpcSprites()
    {
      ModContent.GetInstance<ItemDataExporter>().Logger.Info("Starting vanilla NPC sprite extraction...");

      int extracted = 0;
      int total = 0;

      for (int i = 1; i < NPCLoader.NPCCount; i++)
      {
        total++;

        // Check if this is a vanilla NPC (no ModNPC)
        var npc = new NPC();
        npc.SetDefaults(i);

        if (npc.ModNPC == null && !string.IsNullOrEmpty(npc.TypeName))
        {
          ExtractNpcSprite(i);
          extracted++;

          if (extracted % 50 == 0)
          {
            ModContent.GetInstance<ItemDataExporter>().Logger.Info($"Extracted {extracted} NPC sprites so far...");
          }
        }
      }

      ModContent.GetInstance<ItemDataExporter>().Logger.Info($"Finished extracting {extracted} vanilla NPC sprites out of {total} total NPCs.");
    }

    // Helper methods to ensure content is properly loaded
    private void LoadItem(int type)
    {
      if (type > 0 && type < ItemLoader.ItemCount)
      {
        Main.instance.LoadItem(type);
      }
    }

    private void LoadNPC(int type)
    {
      if (type > 0 && type < NPCLoader.NPCCount)
      {
        Main.instance.LoadNPC(type);
      }
    }
  }
}
