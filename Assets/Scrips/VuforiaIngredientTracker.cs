using UnityEngine;
using System.Collections.Generic;
using Vuforia;
using System;

public class VuforiaIngredientTracker : MonoBehaviour
{
    [Header("Vuforia Targets")]
    [SerializeField] private ObserverBehaviour coffeeCupTarget;
    [SerializeField] private ObserverBehaviour espressoTarget;
    [SerializeField] private ObserverBehaviour creamTarget;
    [SerializeField] private ObserverBehaviour milkTarget;
    [SerializeField] private ObserverBehaviour iceTarget;
    [SerializeField] private ObserverBehaviour pumkinTarget;
    [SerializeField] private ObserverBehaviour chocolateTarget;
    [SerializeField] private ObserverBehaviour vanillaTarget;
    [SerializeField] private ObserverBehaviour caramelTarget;
    [SerializeField] private ObserverBehaviour cinnamonTarget;

    [Header("Distance Settings")]
    [SerializeField] private float addIngredientThreshold = 0.08f; // Distance in meters

    [Header("References")]
    public LiquidController liquidController; // set in inspector

    // Events for UI communication
    public event Action OnCoffeeCupFound;
    public event Action OnCoffeeCupLost;
    public event Action OnEspressoAddedToCup;
    public event Action<string> OnIngredientAdded;
    public event Action<List<Recipe>> OnRecipeSuggestionUpdate;

    // Tracking state
    private bool isCoffeeCupDetected = false;
    private bool isEspressoDetected = false;
    private bool isEspressoAdded = false;

    // Ingredient tracking
    private List<string> addedIngredients = new List<string>();
    private Dictionary<string, string> markerToIngredient = new Dictionary<string, string>
    {
        { "cinnamon", "Cinnamon" },
        { "whipped_cream", "Whipped Cream" },
        { "pumpkin", "Pumpkin Spice Syrup" },
        { "choco_syrup", "Chocolate Syrup" },
        { "caramel", "Caramel Syrup" },
        { "vanilla", "Vanilla Syrup" },
        { "ice", "Ice" },
        { "milk", "Milk" },
        { "steamed_milk", "Steamed Milk" },
        { "hot_water", "Hot Water" }
    };

    // Flags to control when tracking is active
    private bool shouldTrackCoffeeCup = false;
    private bool shouldTrackEspresso = false;
    private bool shouldTrackIngredients = false;

    // Cooldown to prevent immediate re-detection after reset
    private float ingredientCooldown = 0f;
    private const float COOLDOWN_DURATION = 1.5f; // 1.5 seconds

    private void Start()
    {
        // Enable multiple target tracking
        VuforiaConfiguration.Instance.Vuforia.MaxSimultaneousImageTargets = 2;

        // Register Vuforia event handlers for coffee cup
        if (coffeeCupTarget != null)
        {
            coffeeCupTarget.OnTargetStatusChanged += OnCoffeeCupStatusChanged;
        }
        else
        {
            Debug.LogError("Coffee Cup Target is not assigned!");
        }

        // Register Vuforia event handlers for espresso
        if (espressoTarget != null)
        {
            espressoTarget.OnTargetStatusChanged += OnEspressoStatusChanged;
        }
        else
        {
            Debug.LogError("Espresso Target is not assigned!");
        }
    }

    private void Update()
    {
        // Update cooldown timer
        if (ingredientCooldown > 0f)
        {
            ingredientCooldown -= Time.deltaTime;
        }

        // Check distance between coffee cup and espresso when both are detected
        if (shouldTrackEspresso && isCoffeeCupDetected && isEspressoDetected && !isEspressoAdded)
        {
            CheckIngredientDistance(espressoTarget);
        }

        // Check distance for other ingredients (only if cooldown has expired)
        if (shouldTrackIngredients && addedIngredients.Count >= 1 && ingredientCooldown <= 0f)
        {
            var allTargets = FindObjectsOfType<ObserverBehaviour>();
            foreach (var target in allTargets)
            {
                if (target == coffeeCupTarget || target == espressoTarget || 
                    target.TargetName == "ARCamera" || target.TargetName == "DeviceObserver")
                    continue; // skip cup and espresso and other non-ingredient targets

                // Only check if the target is actively TRACKED (visible in camera)
                if (target.TargetStatus.Status == Status.TRACKED)
                {
                    CheckIngredientDistance(target);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Unregister event handlers when destroyed
        if (coffeeCupTarget != null)
        {
            coffeeCupTarget.OnTargetStatusChanged -= OnCoffeeCupStatusChanged;
        }
        if (espressoTarget != null)
        {
            espressoTarget.OnTargetStatusChanged -= OnEspressoStatusChanged;
        }
    }

    // ===== PUBLIC METHODS FOR UI MANAGER =====

    public void EnableCoffeeCupTracking(bool enable)
    {
        shouldTrackCoffeeCup = enable;
    }

    public void EnableEspressoTracking(bool enable)
    {
        shouldTrackEspresso = enable;
    }

    public void EnableIngredientTracking(bool enable)
    {
        shouldTrackIngredients = enable;
    }

    public void ResetTracking()
    {
        isEspressoAdded = false;
        addedIngredients.Clear();
        if (liquidController != null)
            liquidController.ResetCup();
    }

    public void ResetToEspressoAdded()
    {
        addedIngredients.Clear();
        if (liquidController != null)
            liquidController.ResetCup();
        
        isEspressoAdded = false;
        
        // Activate cooldown to prevent immediate re-detection of ingredients
        ingredientCooldown = COOLDOWN_DURATION;
        
        AddEspresso();
    }

    public List<string> GetAddedIngredients()
    {
        return new List<string>(addedIngredients);
    }

    // ===== VUFORIA COFFEE CUP TRACKING =====

    private void OnCoffeeCupStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        // Only track when enabled
        if (!shouldTrackCoffeeCup)
            return;

        if (targetStatus.Status == Status.TRACKED || 
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            HandleCoffeeCupFound();
        }
        else
        {
            HandleCoffeeCupLost();
        }
    }

    private void HandleCoffeeCupFound()
    {
        isCoffeeCupDetected = true;
        
        Vector3 position = coffeeCupTarget.transform.position;
        Debug.Log($"Coffee Cup Position: {position}");
        
        OnCoffeeCupFound?.Invoke();
    }

    private void HandleCoffeeCupLost()
    {
        isCoffeeCupDetected = false;
        
        Debug.Log("Coffee Cup Target LOST!");
        
        // Only notify if espresso not added yet
        if (!isEspressoAdded)
        {
            OnCoffeeCupLost?.Invoke();
        }
    }

    // ===== VUFORIA ESPRESSO TRACKING =====

    private void OnEspressoStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        // Only track when enabled
        if (!shouldTrackEspresso)
            return;

        if (targetStatus.Status == Status.TRACKED || 
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            HandleEspressoFound();
        }
        else
        {
            HandleEspressoLost();
        }
    }

    private void HandleEspressoFound()
    {
        isEspressoDetected = true;
        Vector3 position = espressoTarget.transform.position;
        Debug.Log($"Espresso Position: {position}");
        Debug.Log("Both Coffee Cup and Espresso are now visible!");
    }

    private void HandleEspressoLost()
    {
        isEspressoDetected = false;
        Debug.Log("Espresso Target LOST!");
    }

    // ===== INGREDIENT DISTANCE CHECKING =====

    private void CheckIngredientDistance(ObserverBehaviour target)
    {
        // Calculate distance between coffee cup and target
        float distance = Vector3.Distance(coffeeCupTarget.transform.position, target.transform.position);
        
        Debug.Log($"Distance between Cup and {target}: {distance:F3}m");

        // Check if target is close enough to the cup
        if (distance <= addIngredientThreshold)
        {
            if (addedIngredients.Count == 0)
            {
                AddEspresso();
            }
            else if (markerToIngredient.TryGetValue(target.TargetName, out string ingredientName) && 
                     !addedIngredients.Contains(ingredientName))
            {
                AddIngredient(ingredientName);
            }
            else
            {
                Debug.Log("Ingredient already exists or marker not mapped");
            }
        }
    }

    private void AddIngredient(string ingredient)
    {
        addedIngredients.Add(ingredient);
        Debug.Log($"ADDED to the cup! {ingredient}");
        Debug.Log($"Current Ingredients: {string.Join(", ", addedIngredients)}");
        
        if (liquidController != null)
            liquidController.FillIngredient(ingredient);
        
        OnIngredientAdded?.Invoke(ingredient);
        
        if (ingredient != "Espresso")
        {
            SuggestRecipes();
        }
    }

    private void AddEspresso()
    {
        isEspressoAdded = true;
        AddIngredient("Espresso");
        OnEspressoAddedToCup?.Invoke();
    }

    // ===== RECIPE MATCHING =====

    private void SuggestRecipes()
    {
        var suggestedRecipes = GetMatchingRecipes();
        OnRecipeSuggestionUpdate?.Invoke(suggestedRecipes);
    }

    public List<Recipe> GetMatchingRecipes()
    {
        List<Recipe> suggestionRecipeList = new List<Recipe>();
        
        if (liquidController == null)
            return suggestionRecipeList;
            
        var recipes = liquidController.getRecipes();

        foreach (var recipe in recipes)
        {
            // Only suggest recipes that contain ALL current ingredients
            bool allMatch = true;
            foreach (var ingredient in addedIngredients)
            {
                if (!recipe.ingredients.Contains(ingredient))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
            {
                suggestionRecipeList.Add(recipe);
            }
        }
        return suggestionRecipeList;
    }
}