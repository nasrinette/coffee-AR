using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject scanCupPanel;
    [SerializeField] private GameObject addEspressoPanel;
    [SerializeField] private GameObject pickIngredientPanel;
    [SerializeField] private GameObject suggestionsPanel;

    [Header("Recipe UI")]
    [SerializeField] private GameObject recipeSuggestionContent;
    [SerializeField] private GameObject recipeButtonPrefab;
    [SerializeField] private GameObject recipeDetailsContainer;
    [SerializeField] private GameObject resetButton;
    [SerializeField] private GameObject notFoundText;

    [Header("References")]
    [SerializeField] private VuforiaIngredientTracker ingredientTracker;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Show only the home panel at start
        ShowHomePanel();

        // Subscribe to ingredient tracker events
        if (ingredientTracker != null)
        {
            ingredientTracker.OnCoffeeCupFound += HandleCoffeeCupFound;
            ingredientTracker.OnCoffeeCupLost += HandleCoffeeCupLost;
            ingredientTracker.OnEspressoAddedToCup += HandleEspressoAdded;
            ingredientTracker.OnRecipeSuggestionUpdate += HandleRecipeSuggestions;
        }
        else
        {
            Debug.LogError("VuforiaIngredientTracker is not assigned!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ingredientTracker != null)
        {
            ingredientTracker.OnCoffeeCupFound -= HandleCoffeeCupFound;
            ingredientTracker.OnCoffeeCupLost -= HandleCoffeeCupLost;
            ingredientTracker.OnEspressoAddedToCup -= HandleEspressoAdded;
            ingredientTracker.OnRecipeSuggestionUpdate -= HandleRecipeSuggestions;
        }
    }

    // ===== UI PANEL MANAGEMENT =====

    public void ShowHomePanel()
    {
        HideAllPanels();
        homePanel.SetActive(true);
        
        // Disable all tracking when on home panel
        if (ingredientTracker != null)
        {
            ingredientTracker.EnableCoffeeCupTracking(false);
            ingredientTracker.EnableEspressoTracking(false);
            ingredientTracker.EnableIngredientTracking(false);
        }
    }

    public void OpenInstructionsPanel()
    {
        HideAllPanels();
        instructionPanel.SetActive(true);
    }

    public void OpenScanCupPanel()
    {
        HideAllPanels();
        scanCupPanel.SetActive(true);
        
        // Enable coffee cup tracking
        if (ingredientTracker != null)
        {
            ingredientTracker.EnableCoffeeCupTracking(true);
            ingredientTracker.EnableEspressoTracking(false);
            ingredientTracker.EnableIngredientTracking(false);
        }
    }

    public void OpenAddEspressoPanel()
    {
        HideAllPanels();
        addEspressoPanel.SetActive(true);
        
        // Enable both coffee cup and espresso tracking
        if (ingredientTracker != null)
        {
            ingredientTracker.EnableCoffeeCupTracking(true);
            ingredientTracker.EnableEspressoTracking(true);
            ingredientTracker.EnableIngredientTracking(false);
        }
    }

    public void OpenPickIngredientPanel()
    {
        HideAllPanels();
        pickIngredientPanel.SetActive(true);
        
        // Enable ingredient tracking
        if (ingredientTracker != null)
        {
            ingredientTracker.EnableCoffeeCupTracking(true);
            ingredientTracker.EnableEspressoTracking(false);
            ingredientTracker.EnableIngredientTracking(true);
        }
    }

    public void OpenSuggestionsPanel()
    {
        HideAllPanels();
        suggestionsPanel.SetActive(true);
        
        // Keep ingredient tracking enabled
        if (ingredientTracker != null)
        {
            ingredientTracker.EnableCoffeeCupTracking(true);
            ingredientTracker.EnableEspressoTracking(false);
            ingredientTracker.EnableIngredientTracking(true);
        }
    }

    private void HideAllPanels()
    {
        homePanel.SetActive(false);
        instructionPanel.SetActive(false);
        scanCupPanel.SetActive(false);
        addEspressoPanel.SetActive(false);
        pickIngredientPanel.SetActive(false);
        suggestionsPanel.SetActive(false);
    }

    // ===== EVENT HANDLERS FROM VUFORIA TRACKER =====

    private void HandleCoffeeCupFound()
    {
        // Switch to Add Espresso Panel if currently in Scan Cup Panel
        if (scanCupPanel.activeSelf)
        {
            OpenAddEspressoPanel();
        }
    }

    private void HandleCoffeeCupLost()
    {
        // Switch back to Scan Cup Panel only if in Add Espresso Panel
        if (addEspressoPanel.activeSelf)
        {
            OpenScanCupPanel();
        }
    }

    private void HandleEspressoAdded()
    {
        // Switch to Pick Ingredient Panel
        OpenPickIngredientPanel();
    }

    private void HandleRecipeSuggestions(List<Recipe> suggestedRecipes)
    {
        OpenSuggestionsPanel();
        DisplayRecipeSuggestions(suggestedRecipes);
    }

    // ===== RECIPE SUGGESTION DISPLAY =====
    private void DisplayRecipeSuggestions(List<Recipe> suggestedRecipes)
    {
        // Make sure recipe content is active
        if (recipeSuggestionContent != null)
        {
            recipeSuggestionContent.SetActive(true);
        }

        // Clear previous suggestions
        if (recipeSuggestionContent != null)
        {
            foreach (Transform child in recipeSuggestionContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Check if no recipes found
        if (suggestedRecipes.Count == 0)
        {
            if (resetButton != null) resetButton.SetActive(true);
            if (notFoundText != null) notFoundText.SetActive(true);
            if (recipeDetailsContainer != null) recipeDetailsContainer.SetActive(false);
            return;
        }
        else
        {
            // Hide reset button and not found text if previously shown
            if (resetButton != null) resetButton.SetActive(false);
            if (notFoundText != null) notFoundText.SetActive(false);
        }

        // Create buttons for each suggested recipe
        foreach (var recipe in suggestedRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipeSuggestionContent.transform);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = recipe.name;
            
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    ShowRecipeDetails(recipe);
                });
            }
        }
    }

    private void ShowRecipeDetails(Recipe recipe)
    {
        if (recipeDetailsContainer != null)
        {
            recipeDetailsContainer.SetActive(true);
            var detailsText = recipeDetailsContainer.GetComponentInChildren<TextMeshProUGUI>();
            if (detailsText != null)
            {
                detailsText.text = $"{string.Join("\n ", recipe.ingredients)}";
            }
        }
    }

    // ===== BUTTON ACTIONS =====

    public void ResetToPickIngredient()
    {
        // Reset tracking state
        if (ingredientTracker != null)
        {
            ingredientTracker.ResetToEspressoAdded();
        }

        // Clear recipe UI
        ClearRecipeSuggestions();
        
    }

    public void GoHomePanel()
    {
        // Reset tracking state
        if (ingredientTracker != null)
        {
            ingredientTracker.ResetTracking();
        }

        // Clear recipe UI
        ClearRecipeSuggestions();

        // Show home panel
        ShowHomePanel();
    }

    private void ClearRecipeSuggestions()
    {
        // Destroy all recipe button children
        if (recipeSuggestionContent != null)
        {
            foreach (Transform child in recipeSuggestionContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear recipe details text and hide container
        if (recipeDetailsContainer != null)
        {
            recipeDetailsContainer.SetActive(false);
            var detailsText = recipeDetailsContainer.GetComponentInChildren<TextMeshProUGUI>();
            if (detailsText != null)
            {
                detailsText.text = "";
            }
        }

        // Hide reset button and not found text
        if (resetButton != null) resetButton.SetActive(false);
        if (notFoundText != null) notFoundText.SetActive(false);
    }
}