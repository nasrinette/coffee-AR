using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ColorData
{
    public int r;
    public int g;
    public int b;

    public Color ToColor()
    {
        // Convert 0–255 to 0–1 range for Unity
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}

[System.Serializable]
public class Recipe
{
    public string name;
    public List<string> ingredients;
    public ColorData color;
}

[System.Serializable]
public class RecipeList
{
    public List<string> coreIngredients;
    public List<Recipe> recipes;
}
