using UnityEngine;
using Vuforia;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject instructionsPanel;
    [SerializeField] GameObject scanCupPanel;
    [SerializeField] GameObject addEspressoPanel;

    [Header("Tracking")]
    public ObserverBehaviour cupObserver;           //cup target
    [SerializeField] float lostDebounce = 0.5f;     // seconds to wait before treating as lost
    bool isCupVisible;
    float lostTimer;
    void Start() => ShowMain();
    void Update()
    {
        // Handle delayed "lost" to avoid flicker
        if (!isCupVisible && lostTimer > 0f)
        {
            lostTimer -= Time.deltaTime;
            if (lostTimer <= 0f)
                ShowScanCup();
        }
    }
    void OnEnable()
    {
        if (cupObserver != null)
            cupObserver.OnTargetStatusChanged += OnCupStatusChanged;
    }

    void OnDisable()
    {
        if (cupObserver != null)
            cupObserver.OnTargetStatusChanged -= OnCupStatusChanged;
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        instructionsPanel.SetActive(false);
    }

    public void ShowInstructions()
    {
        mainPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void ShowScanCup()
    {
        instructionsPanel.SetActive(false);
        scanCupPanel.SetActive(true);
    }

    void OnCupStatusChanged(ObserverBehaviour ob, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        if (tracked)
        {
            isCupVisible = true;
            lostTimer = 0f;
            Debug.Log("Cup found");
            addEspressoPanel.SetActive(true);
            scanCupPanel.SetActive(false);
        }
        else
        {
            isCupVisible = false;
            Debug.Log("Cup not found");
            lostTimer = lostDebounce; // start debounce countdown before showing scan UI
            addEspressoPanel.SetActive(false);
            scanCupPanel.SetActive(true); // Moved to Update() after debounce
        }
    }
}

