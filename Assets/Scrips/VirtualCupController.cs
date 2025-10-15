using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LiquidController : MonoBehaviour
{
    [Header("Material Reference")]
    public Material liquidMaterial;
    
    [Header("Fill Settings")]
    [Range(0f, 1f)] public float fillLevel = 0f;
    public float fillSpeed = 1f; // Speed of fill animation
    
    [Header("Ingredient Colors")]
    public Color coffeeColor = new Color(0.2f, 0.1f, 0f);
    public Color milkColor = new Color(1f, 1f, 0.9f);
    public Color chocolateColor = new Color(0.3f, 0.15f, 0.05f);
    public Color vanillaColor = new Color(1f, 0.95f, 0.8f);
    
    [Header("Recipe Colors")]
    public Color latteColor = new Color(0.76f, 0.6f, 0.42f); // Coffee + Milk
    public Color mochaColor = new Color(0.4f, 0.25f, 0.15f); // Coffee + Chocolate
    public Color hotChocolateColor = new Color(0.45f, 0.3f, 0.2f); // Milk + Chocolate
    public Color vanillaLatteColor = new Color(0.85f, 0.75f, 0.6f); // Coffee + Milk + Vanilla
    
    private HashSet<string> ingredientsAdded = new HashSet<string>();
    private bool isAnimating = false;
    
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            liquidMaterial = renderer.material;
        }
        UpdateShader();
    }
    
    public void AddIngredient(string ingredientName)
    {
        if (isAnimating) return; // Prevent adding while animating
        
        string ingredient = ingredientName.ToLower();
        ingredientsAdded.Add(ingredient);
        
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
    }
    
    private Color GetRecipeColor()
    {
        if (ingredientsAdded.Count == 0) return Color.clear;
        
        bool hasCoffee = ingredientsAdded.Contains("coffee");
        bool hasMilk = ingredientsAdded.Contains("milk");
        bool hasChocolate = ingredientsAdded.Contains("chocolate");
        bool hasVanilla = ingredientsAdded.Contains("vanilla");
        
        // Complex recipes (3+ ingredients)
        if (hasCoffee && hasMilk && hasVanilla)
            return vanillaLatteColor;
        
        if (hasCoffee && hasMilk && hasChocolate)
            return Color.Lerp(mochaColor, latteColor, 0.5f); // Chocolate mocha latte
        
        if (hasMilk && hasChocolate && hasVanilla)
            return Color.Lerp(hotChocolateColor, vanillaColor, 0.3f); // Vanilla hot chocolate
        
        // Two ingredient recipes
        if (hasCoffee && hasMilk)
            return latteColor;
        
        if (hasCoffee && hasChocolate)
            return mochaColor;
        
        if (hasMilk && hasChocolate)
            return hotChocolateColor;
        
        if (hasCoffee && hasVanilla)
            return Color.Lerp(coffeeColor, vanillaColor, 0.3f);
        
        if (hasMilk && hasVanilla)
            return Color.Lerp(milkColor, vanillaColor, 0.5f);
        
        // Single ingredients
        if (hasCoffee) return coffeeColor;
        if (hasMilk) return milkColor;
        if (hasChocolate) return chocolateColor;
        if (hasVanilla) return vanillaColor;
        
        return Color.white;
    }
    
    [ContextMenu("Add Milk")]
    public void AddMilk() => AddIngredient("milk");
    
    [ContextMenu("Add Coffee")]
    public void AddCoffee() => AddIngredient("coffee");
    
    [ContextMenu("Add Chocolate")]
    public void AddChocolate() => AddIngredient("chocolate");
    
    [ContextMenu("Add Vanilla")]
    public void AddVanilla() => AddIngredient("vanilla");
    
    [ContextMenu("Reset Cup")]
    public void ResetCup()
    {
        StopAllCoroutines();
        ingredientsAdded.Clear();
        fillLevel = 0f;
        isAnimating = false;
        UpdateShader();
    }
}