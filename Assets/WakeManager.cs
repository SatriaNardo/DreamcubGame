using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WakeManager : MonoBehaviour
{
    public static WakeManager Instance { get; private set; }

    [Header("Wake System Settings")]
    [SerializeField] private float maxWake = 100f;
    private float currentWake = 0f;

    [Header("Accumulation Rates")]
    [SerializeField] private float runRatePerSecond = 2f; 
    [SerializeField] private float actionSpike = 30f;     
    [SerializeField] private float enemyHitPenalty = 40f; 

    [Header("Decay Settings")]
    [SerializeField] private float decayRatePerSecond = 5f; 
    
    [Header("UI Component Link")]
    [SerializeField] private TextMeshProUGUI wakeTextDisplay; 

    private bool isPlayerMovingThisFrame = false;

    // --- Master lists for respawning ---
    private List<MeleeEnemy> allMeleeEnemies = new List<MeleeEnemy>();
    private List<ShootingEnemy> allShootingEnemies = new List<ShootingEnemy>();

    // --- Active Checkpoint Memory ---
    private Vector3 activeRespawnPosition;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Find the initial starting point of the level
        GameObject defaultSpawn = GameObject.FindGameObjectWithTag("Respawn");
        if (defaultSpawn != null)
        {
            activeRespawnPosition = defaultSpawn.transform.position;
        }

        UpdateUI();
    }

    void Update()
    {
        if (!isPlayerMovingThisFrame && currentWake > 0f)
        {
            currentWake -= decayRatePerSecond * Time.deltaTime;
            currentWake = Mathf.Clamp(currentWake, 0f, maxWake);
            UpdateUI(); 
        }

        isPlayerMovingThisFrame = false;
    }

    // --- ENEMY REGISTRATION ---
    public void RegisterMeleeEnemy(MeleeEnemy enemy)
    {
        if (!allMeleeEnemies.Contains(enemy)) allMeleeEnemies.Add(enemy);
    }

    public void RegisterShootingEnemy(ShootingEnemy enemy)
    {
        if (!allShootingEnemies.Contains(enemy)) allShootingEnemies.Add(enemy);
    }

    // --- WAKE MODIFIERS ---
    public void AddPassiveMovementWake()
    {
        isPlayerMovingThisFrame = true; 
        currentWake += runRatePerSecond * Time.deltaTime;
        CheckWakeStatus();
        UpdateUI(); 
    }

    public void AddActionSpike()
    {
        currentWake += actionSpike;
        CheckWakeStatus();
        UpdateUI(); 
    }

    public void RewardSuccessfulParry()
    {
        currentWake -= actionSpike; 
        currentWake = Mathf.Clamp(currentWake, 0f, maxWake);
        Debug.Log("Perfect Parry! Wake spike refunded.");
        UpdateUI();
    }

    public void TakeDamagePenalty()
    {
        currentWake += enemyHitPenalty;
        Debug.Log($"Ouch! Enemy hit you. Wake spiked by {enemyHitPenalty}%!");
        CheckWakeStatus();
        UpdateUI();
    }

    // Instantly maxes out the meter (Used for spikes/pits)
    public void MaxOutWakeMeter()
    {
        currentWake = maxWake;
        Debug.Log("Fell into a Dead Zone! Instant 100% Wake!");
        CheckWakeStatus();
        UpdateUI();
    }

    // --- CHECKPOINT SYSTEM ---
    public void SetRespawnPoint(Vector3 newPos)
    {
        activeRespawnPosition = newPos;
        Debug.Log("Checkpoint Saved!");
    }

    private void UpdateUI()
    {
        if (wakeTextDisplay != null)
        {
            wakeTextDisplay.text = Mathf.RoundToInt(currentWake).ToString() + "%";
        }
    }

    private void CheckWakeStatus()
    {
        currentWake = Mathf.Clamp(currentWake, 0f, maxWake);
        
        if (currentWake >= maxWake)
        {
            TriggerInstantRespawn();
        }
    }

    // --- THE MASTER RESPAWN CYCLE ---
    private void TriggerInstantRespawn()
    {
        Debug.Log("YOU WOKE UP! Warping back to sleep...");

        // 1. Respawn the player at the memorized checkpoint
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) player.RespawnAt(activeRespawnPosition);

        // 2. Clear any leftover bullets flying through the air
        EnemyProjectile[] strayBullets = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        foreach (EnemyProjectile bullet in strayBullets) Destroy(bullet.gameObject);

        // 3. Wake up all the enemies!
        foreach (MeleeEnemy melee in allMeleeEnemies) if (melee != null) melee.ResetEnemy();
        foreach (ShootingEnemy shooter in allShootingEnemies) if (shooter != null) shooter.ResetEnemy();

        // 4. Reset all Arena Gates so you don't get locked out! (FindObjectsInactive.Include prevents blind spots)
        CombatGate[] allGates = Object.FindObjectsByType<CombatGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CombatGate gate in allGates)
        {
            gate.ResetGate();
        }

        // 5. Reset the Wake Meter cleanly
        currentWake = 0f;
        UpdateUI();
    }
}