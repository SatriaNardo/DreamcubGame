using UnityEngine;

public class CombatGate : MonoBehaviour
{
    [Header("Gate Setup")]
    [Tooltip("Drag the actual solid gate/wall object here")]
    [SerializeField] private GameObject solidGateObject; 
    
    [Tooltip("Drag the specific enemies you want to track here")]
    [SerializeField] private GameObject[] targetEnemies; 

    private bool isCleared = false;

    void Start()
    {
        // 1. Gate starts CLOSED, blocking the path forward!
        if (solidGateObject != null) solidGateObject.SetActive(true);
    }

    void Update()
    {
        // 2. If the gate is already open, stop doing math
        if (isCleared) return;

        bool allEnemiesDefeated = true;
        foreach (GameObject enemy in targetEnemies)
        {
            // If even ONE enemy is still alive, the gate stays closed
            if (enemy != null && enemy.activeInHierarchy)
            {
                allEnemiesDefeated = false;
                break; 
            }
        }

        // 3. If we checked all enemies and none are active, open the door!
        if (allEnemiesDefeated)
        {
            isCleared = true;
            if (solidGateObject != null) solidGateObject.SetActive(false);
            Debug.Log("Enemies defeated! Roadblock opened.");
        }
    }

    // --- Called by WakeManager when the player dies ---
    public void ResetGate()
    {
        isCleared = false;
        
        // 4. The enemies have respawned, so lock the door again!
        if (solidGateObject != null) solidGateObject.SetActive(true);
    }
}