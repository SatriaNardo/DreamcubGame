using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required to control TextMeshPro components

public class WakeManager : MonoBehaviour
{
    public static WakeManager Instance { get; private set; }

    [Header("Wake System Settings")]
    [SerializeField] private float maxWake = 100f;
    private float currentWake = 0f;

    [Header("Accumulation Rates")]
    [SerializeField] private float runRatePerSecond = 2f; 
    [SerializeField] private float actionSpike = 30f;     

    [Header("Decay Settings")]
    [SerializeField] private float decayRatePerSecond = 5f; 
    
    [Header("UI Component Link")]
    [SerializeField] private TextMeshProUGUI wakeTextDisplay; // Drag WakeText here!

    private bool isGameOver = false;
    private bool isPlayerMovingThisFrame = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        if (isGameOver)
        {
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     RestartGame();
            // }
            // return; 
        }

        if (!isPlayerMovingThisFrame && currentWake > 0f)
        {
            currentWake -= decayRatePerSecond * Time.deltaTime;
            currentWake = Mathf.Clamp(currentWake, 0f, maxWake);
            UpdateUI(); // Update UI during cooldown
        }

        isPlayerMovingThisFrame = false;
    }

    public void AddPassiveMovementWake()
    {
        if (isGameOver) return;
        
        isPlayerMovingThisFrame = true; 
        currentWake += runRatePerSecond * Time.deltaTime;
        CheckWakeStatus();
        UpdateUI(); // Update UI while running
    }

    public void AddActionSpike()
    {
        if (isGameOver) return;

        currentWake += actionSpike;
        CheckWakeStatus();
        UpdateUI(); // Update UI on jump/dash/attack
    }

    // New Function: Formats the number nicely onto your Canvas
    private void UpdateUI()
    {
        if (wakeTextDisplay != null)
        {
            // Rounds the decimal to a whole number and adds the % symbol
            wakeTextDisplay.text = Mathf.RoundToInt(currentWake).ToString() + "%";
        }
    }

    // Call this function when a parry or deflection succeeds to reward the player
    public void RewardSuccessfulParry()
    {
        if (isGameOver) return;

        // Undo the 30% action spike penalty
        currentWake -= actionSpike;
        
        // Safety check to ensure the meter doesn't drop below zero
        currentWake = Mathf.Clamp(currentWake, 0f, maxWake);
        
        Debug.Log($"Perfect Parry! Wake spike refunded. Current Wake: {Mathf.RoundToInt(currentWake)}%");
        UpdateUI();
    }

    private void CheckWakeStatus()
    {
        currentWake = Mathf.Clamp(currentWake, 0f, maxWake);

        if (currentWake >= maxWake && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        
        if (wakeTextDisplay != null)
        {
            wakeTextDisplay.text = "WOKE UP\n[R] to Restart";
        }

        Time.timeScale = 0f; 

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) player.enabled = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}