using UnityEngine;
using UnityEngine.UI; // You need this to talk to UI elements!

public class WakeBarUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the WakeBar_Full object here")]
    [SerializeField] private Image wakeBarFill;

    // We can make this a Singleton so your WakeManager can easily talk to it
    public static WakeBarUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this method whenever the player's Wake changes!
    /// </summary>
    /// <param name="currentWake">The player's current wake/health</param>
    /// <param name="maxWake">The maximum possible wake/health</param>
    public void UpdateWakeBar(float currentWake, float maxWake)
    {
        if (wakeBarFill == null) return;

        // Fill Amount is a percentage between 0 and 1. 
        // We get this by dividing current by max!
        float fillPercentage = currentWake / maxWake;
        
        wakeBarFill.fillAmount = fillPercentage;
    }
}