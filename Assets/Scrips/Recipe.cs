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
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}

[System.Serializable]
public class Ingredient
{
    public string name;
    public ColorData color;
}

[System.Serializable]
public class Recipe
{
    public string name;
    public List<string> ingredients;
    // No color field anymore - will be calculated!
}

[System.Serializable]
public class RecipeList
{
    public List<Ingredient> ingredients;  // Changed from List<string>
    public List<Recipe> recipes;
}