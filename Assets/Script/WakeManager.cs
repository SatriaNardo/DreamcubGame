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
    
    // NEW: List for Flying Enemies!
    private List<FlyingMeleeEnemy> allFlyingEnemies = new List<FlyingMeleeEnemy>();

    // --- Active Checkpoint Memory ---
    private Vector3 activeRespawnPosition;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
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

    // NEW: Registration for Flying Enemies!
    public void RegisterFlyingEnemy(FlyingMeleeEnemy enemy)
    {
        if (!allFlyingEnemies.Contains(enemy)) allFlyingEnemies.Add(enemy);
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

    public void MaxOutWakeMeter()
    {
        currentWake = maxWake;
        Debug.Log("Fell into a Dead Zone! Instant 100% Wake!");
        CheckWakeStatus();
        UpdateUI();
    }

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

        if (WakeBarUI.Instance != null)
        {
            WakeBarUI.Instance.UpdateWakeBar(currentWake, maxWake);
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

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) player.RespawnAt(activeRespawnPosition);

        EnemyProjectile[] strayBullets = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        foreach (EnemyProjectile bullet in strayBullets) Destroy(bullet.gameObject);

        // Wake up all the enemies!
        foreach (MeleeEnemy melee in allMeleeEnemies) if (melee != null) melee.ResetEnemy();
        foreach (ShootingEnemy shooter in allShootingEnemies) if (shooter != null) shooter.ResetEnemy();
        
        // NEW: Wake up all the flying enemies!
        foreach (FlyingMeleeEnemy flyer in allFlyingEnemies) if (flyer != null) flyer.ResetEnemy();

        CombatGate[] allGates = Object.FindObjectsByType<CombatGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CombatGate gate in allGates)
        {
            gate.ResetGate();
        }

        currentWake = 0f;
        UpdateUI();
    }
}