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
    
    [HideInInspector] public List<Ingredient> ingredients = new List<Ingredient>();
    private List<Recipe> recipes = new List<Recipe>();
    private List<string> ingredientsAdded = new List<string>();
    private bool isAnimating = false;
    private string currentRecipeName = "None";
    
    // Dictionary for fast color lookup
    private Dictionary<string, Color> ingredientColors = new Dictionary<string, Color>();
    
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
            ingredients = recipeList.ingredients;
            recipes = recipeList.recipes;
            
            // Build color lookup dictionary
            ingredientColors.Clear();
            foreach (var ingredient in ingredients)
            {
                ingredientColors[ingredient.name] = ingredient.color.ToColor();
            }
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
        
        // Blend colors from all added ingredients
        Color blendedColor = BlendIngredientColors(ingredientsAdded);
        
        // Check if this matches a known recipe
        int ingredientCount = ingredientsAdded.Count;
        List<Recipe> matchingRecipes = recipes.FindAll(r => r.ingredients.Count == ingredientCount);
        
        foreach (Recipe recipe in matchingRecipes)
        {
            bool allMatch = recipe.ingredients.All(ing => ingredientsAdded.Contains(ing));
            
            if (allMatch)
            {
                currentRecipeName = recipe.name;
                return blendedColor;
            }
        }
        
        // Check for partial match
        for (int i = ingredientCount - 1; i >= 1; i--)
        {
            List<Recipe> smallerRecipes = recipes.FindAll(r => r.ingredients.Count == i);
            
            foreach (Recipe recipe in smallerRecipes)
            {
                bool allMatch = recipe.ingredients.All(ing => ingredientsAdded.Contains(ing));
                
                if (allMatch)
                {
                    currentRecipeName = recipe.name + " + extras";
                    return blendedColor;
                }
            }
        }
        
        currentRecipeName = "Custom Mix (" + ingredientCount + " ingredients)";
        return blendedColor;
    }
    
    private Color BlendIngredientColors(List<string> ingredientNames)
    {
        if (ingredientNames.Count == 0)
            return Color.clear;
        
        float r = 0f, g = 0f, b = 0f;
        int count = 0;
        
        foreach (string ingredientName in ingredientNames)
        {
            if (ingredientColors.ContainsKey(ingredientName))
            {
                Color color = ingredientColors[ingredientName];
                r += color.r;
                g += color.g;
                b += color.b;
                count++;
            }
        }
        
        if (count == 0)
            return new Color(0.8f, 0.7f, 0.6f); // Fallback
        
        return new Color(r / count, g / count, b / count);
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
        
        if (controller.recipeJsonFile != null && controller.ingredients.Count > 0)
        {
            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("Add Ingredients", UnityEditor.EditorStyles.boldLabel);
            
            foreach (var ingredient in controller.ingredients)
            {
                if (GUILayout.Button("Add " + ingredient.name))
                {
                    controller.FillIngredient(ingredient.name);
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