using UnityEngine;
using System.Collections.Generic;
using Vuforia;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject scanCupPanel;
    [SerializeField] private GameObject addEspressoPanel;
    [SerializeField] private GameObject pickIngredientPanel;

    [Header("Vuforia Targets")]
    [SerializeField] private ObserverBehaviour coffeeCupTarget;
    [SerializeField] private ObserverBehaviour espressoTarget;

    [Header("Distance Settings")]
    [SerializeField] private float addIngredientThreshold = 0.15f; // Distance in meters (15cm)

    // Boolean to track detections
    private bool isCoffeeCupDetected = false;
    private bool isEspressoDetected = false;
    private bool isEspressoAdded = false;

    // List of added ingredients
    private List<string> addedIngredients = new List<string>();

    public bool IsCoffeeCupDetected => isCoffeeCupDetected;
    public bool IsEspressoDetected => isEspressoDetected;
    public List<string> AddedIngredients => addedIngredients;

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
        // Check distance between coffee cup and espresso when both are detected
        if (isCoffeeCupDetected && isEspressoDetected && !isEspressoAdded && addEspressoPanel.activeSelf)
        {
            CheckIngredientDistance();
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

    // ===== UI PANEL MANAGEMENT =====

    // Show Home Panel
    public void ShowHomePanel()
    {
        HideAllPanels();
        homePanel.SetActive(true);
    }

    // Open Instructions Panel (called from Instructions button on Home Panel)
    public void OpenInstructionsPanel()
    {
        HideAllPanels();
        instructionPanel.SetActive(true);
    }

    // Open Scan Cup Panel (called from Start button on Instructions Panel)
    public void OpenScanCupPanel()
    {
        HideAllPanels();
        scanCupPanel.SetActive(true);
    }

    // Open Add Espresso Panel (will be called after cup is scanned)
    public void OpenAddEspressoPanel()
    {
        HideAllPanels();
        addEspressoPanel.SetActive(true);
    }

    // Open Pick Ingredient Panel (called after ingredient is added)
    public void OpenPickIngredientPanel()
    {
        HideAllPanels();
        pickIngredientPanel.SetActive(true);
    }

    // Helper method to hide all panels
    private void HideAllPanels()
    {
        homePanel.SetActive(false);
        instructionPanel.SetActive(false);
        scanCupPanel.SetActive(false);
        addEspressoPanel.SetActive(false);
        pickIngredientPanel.SetActive(false);
    }

    // ===== VUFORIA COFFEE CUP TRACKING =====

    // Called when coffee cup target tracking status changes
    private void OnCoffeeCupStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        // Only track when scan cup panel or add espresso panel is active
        if (!scanCupPanel.activeSelf && !addEspressoPanel.activeSelf)
            return;

        if (targetStatus.Status == Status.TRACKED || 
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            OnCoffeeCupFound();
        }
        else
        {
            OnCoffeeCupLost();
        }
    }

    // Called when coffee cup target is found and tracked
    private void OnCoffeeCupFound()
    {
        isCoffeeCupDetected = true;
        
        // Log that target is found
        Debug.Log("Coffee Cup Target FOUND!");
        
        // Log position
        Vector3 position = coffeeCupTarget.transform.position;
        Debug.Log($"Coffee Cup Position: {position}");
        
        // Switch to Add Espresso Panel if currently in Scan Cup Panel
        if (scanCupPanel.activeSelf)
        {
            OpenAddEspressoPanel();
        }
    }

    // Called when coffee cup target is lost
    private void OnCoffeeCupLost()
    {
        isCoffeeCupDetected = false;
        
        Debug.Log("Coffee Cup Target LOST!");
        
        // Switch back to Scan Cup Panel only if in Add Espresso Panel and espresso not added yet
        if (addEspressoPanel.activeSelf && !isEspressoAdded)
        {
            OpenScanCupPanel();
        }
    }

    // ===== VUFORIA ESPRESSO TRACKING =====

    // Called when espresso target tracking status changes
    private void OnEspressoStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        // Only track when add espresso panel is active
        if (!addEspressoPanel.activeSelf)
            return;

        if (targetStatus.Status == Status.TRACKED || 
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            OnEspressoFound();
        }
        else
        {
            OnEspressoLost();
        }
    }

    // Called when espresso target is found and tracked
    private void OnEspressoFound()
    {
        isEspressoDetected = true;
        
        // Log that target is found
        Debug.Log("Espresso Target FOUND!");
        
        // Log position
        Vector3 position = espressoTarget.transform.position;
        Debug.Log($"Espresso Position: {position}");

        // Stay in Add Espresso Panel (no panel change)
        Debug.Log("Both Coffee Cup and Espresso are now visible!");
    }

    // Called when espresso target is lost
    private void OnEspressoLost()
    {
        isEspressoDetected = false;
        
        Debug.Log("Espresso Target LOST!");

        // Stay in Add Espresso Panel (no panel change)
    }

    // ===== INGREDIENT DISTANCE CHECKING =====

    private void CheckIngredientDistance()
    {
        // Calculate distance between coffee cup and espresso
        float distance = Vector3.Distance(coffeeCupTarget.transform.position, espressoTarget.transform.position);
        
        Debug.Log($"Distance between Cup and Espresso: {distance:F3}m");

        // Check if espresso is close enough to the cup
        if (distance <= addIngredientThreshold)
        {
            AddEspresso();
        }
    }

    private void AddEspresso()
    {
        if (isEspressoAdded)
            return;

        isEspressoAdded = true;
        
        // Add espresso to the ingredients list
        addedIngredients.Add("Espresso");
        
        Debug.Log("✓ ESPRESSO ADDED to the cup!");
        Debug.Log($"Current Ingredients: {string.Join(", ", addedIngredients)}");

        // Switch to Pick Ingredient Panel
        OpenPickIngredientPanel();
    }

    // ===== PUBLIC HELPER METHODS =====

    // Public method to check if cup is currently detected
    public bool IsCupInView()
    {
        return isCoffeeCupDetected;
    }

    // Public method to check if espresso is currently detected
    public bool IsEspressoInView()
    {
        return isEspressoDetected;
    }

    // Public method to get the list of added ingredients
    public void PrintAddedIngredients()
    {
        Debug.Log($"Added Ingredients ({addedIngredients.Count}): {string.Join(", ", addedIngredients)}");
    }
}