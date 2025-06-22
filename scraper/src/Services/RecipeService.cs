using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace ItemDataExporter.Services
{
  public class RecipeService
  {
    public List<object> GetRecipes(int itemType)
    {
      List<object> recipes = new List<object>();
      foreach (var recipe in Main.recipe)
      {
        if (recipe?.createItem?.type == itemType)
        {
          var ingredients = new List<object>();
          if (recipe.requiredItem != null)
          {
            foreach (var ingredient in recipe.requiredItem)
            {
              if (ingredient != null && ingredient.type > 0)
              {
                ingredients.Add(new
                {
                  type = ingredient.type,
                  stack = ingredient.stack,
                  name = ingredient.Name
                });
              }
            }
          }

          recipes.Add(new
          {
            Ingredients = ingredients,
            Tiles = recipe.requiredTile?.Where(t => t > 0).ToList(),
            ResultStack = recipe.createItem.stack
          });
        }
      }
      return recipes;
    }
  }
}
