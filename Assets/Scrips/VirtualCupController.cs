using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LiquidController : MonoBehaviour
{
    [Header("Material Reference")]
    public Material liquidMaterial;
    
    [Header("Fill Settings")]
    [Range(0f, 1f)] public float fillLevel = 0f;
    public float fillSpeed = 1f;
    
    [Header("Recipe Data")]
    public TextAsset recipeJsonFile;
    
    [HideInInspector] public List<string> coreIngredients = new List<string>();
    private List<Recipe> recipes = new List<Recipe>();
    private List<string> ingredientsAdded = new List<string>(); // Changed to List to maintain order
    private bool isAnimating = false;
    private string currentRecipeName = "None";
    
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            liquidMaterial = renderer.material;
        }
        
        LoadRecipes();
        UpdateShader();
    }
    
    void OnValidate()
    {
        LoadRecipes();
    }
    
    private void LoadRecipes()
    {
        if (recipeJsonFile == null) return;
        
        try
        {
            RecipeList recipeList = JsonUtility.FromJson<RecipeList>(recipeJsonFile.text);
            coreIngredients = recipeList.coreIngredients;
            recipes = recipeList.recipes;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load recipes: " + e.Message);
        }
    }

    public List<Recipe> getRecipes()
    {
        return recipes;
    }
    
    public void FillIngredient(string ingredientName)
    {
        if (isAnimating) return;
        
        // Don't add duplicate ingredients
        if (!ingredientsAdded.Contains(ingredientName))
        {
            ingredientsAdded.Add(ingredientName);
        }
        
        float targetFill = Mathf.Clamp01(fillLevel + 0.25f);
        StartCoroutine(AnimateFill(targetFill));
    }
    
    private IEnumerator AnimateFill(float targetFill)
    {
        isAnimating = true;
        float startFill = fillLevel;
        float elapsed = 0f;
        float duration = 0.5f / fillSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillLevel = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            UpdateShader();
            yield return null;
        }
        
        fillLevel = targetFill;
        UpdateShader();
        isAnimating = false;
    }
    
    private void UpdateShader()
    {
        if (liquidMaterial == null) return;
        
        Color currentColor = GetRecipeColor();
        liquidMaterial.SetFloat("_Fill", fillLevel);
        liquidMaterial.SetColor("_topColor", currentColor);
        liquidMaterial.SetColor("_sideColor", currentColor * 0.8f);
        
        Debug.Log($"Current Recipe: {currentRecipeName} | Ingredients: {string.Join(", ", ingredientsAdded)} | Color: RGB({currentColor.r * 255:F0}, {currentColor.g * 255:F0}, {currentColor.b * 255:F0})");
    }
    
    private Color GetRecipeColor()
    {
        if (ingredientsAdded.Count == 0)
        {
            currentRecipeName = "None";
            return Color.clear;
        }
        
        int ingredientCount = ingredientsAdded.Count;
        
        // Find recipes that match the current ingredient count
        List<Recipe> matchingRecipes = recipes.FindAll(r => r.ingredients.Count == ingredientCount);
        
        // Check for exact match with current ingredients
        foreach (Recipe recipe in matchingRecipes)
        {
            // Check if all recipe ingredients are in our added ingredients (order doesn't matter)
            bool allMatch = recipe.ingredients.All(ing => ingredientsAdded.Contains(ing));
            
            if (allMatch)
            {
                currentRecipeName = recipe.name;
                return recipe.color.ToColor();
            }
        }
        
        // No exact match found for current ingredient count
        currentRecipeName = "Custom Mix (" + ingredientCount + " ingredients)";
        
        // Fallback: try to find a recipe with fewer ingredients that matches what we have
        for (int i = ingredientCount - 1; i >= 1; i--)
        {
            List<Recipe> smallerRecipes = recipes.FindAll(r => r.ingredients.Count == i);
            
            foreach (Recipe recipe in smallerRecipes)
            {
                bool allMatch = recipe.ingredients.All(ing => ingredientsAdded.Contains(ing));
                
                if (allMatch)
                {
                    currentRecipeName = recipe.name + " + extras";
                    return recipe.color.ToColor();
                }
            }
        }
        
        // Ultimate fallback - return a neutral color
        return new Color(0.8f, 0.7f, 0.6f);
    }
    
    [ContextMenu("Reset Cup")]
    public void ResetCup()
    {
        StopAllCoroutines();
        ingredientsAdded.Clear();
        fillLevel = 0f;
        isAnimating = false;
        currentRecipeName = "None";
        UpdateShader();
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(LiquidController))]
public class LiquidControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        LiquidController controller = (LiquidController)target;
        
        if (controller.recipeJsonFile != null && controller.coreIngredients.Count > 0)
        {
            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("Add Ingredients", UnityEditor.EditorStyles.boldLabel);
            
            foreach (var ingredient in controller.coreIngredients)
            {
                if (GUILayout.Button("Add " + ingredient))
                {
                    controller.FillIngredient(ingredient);
                }
            }
            
            UnityEditor.EditorGUILayout.Space(5);
            if (GUILayout.Button("Reset Cup", GUILayout.Height(30)))
            {
                controller.ResetCup();
            }
        }
    }
}
#endif
