using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ItemScraper.Services
{
  public class RecipeService
  {
    public List<object> GetRecipes(int itemType)
    {
      List<object> recipes = new();
      foreach (var recipe in Main.recipe)
      {
        if (recipe?.createItem?.type == itemType)
        {
          recipes.Add(new
          {
            Ingredients = recipe.requiredItem?.ConvertAll(i => new { i.type, i.stack }),
            Tiles = recipe.requiredTile,
            ResultStack = recipe.createItem.stack
          });
        }
      }
      return recipes;
    }
  }
}