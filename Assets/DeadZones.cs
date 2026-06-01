using UnityEngine;

public class DeadZones : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that touched the zone is the Player
        if (collision.CompareTag("Player"))
        {
            // Instantly spike the Wake Meter to 100%
            if (WakeManager.Instance != null)
            {
                WakeManager.Instance.MaxOutWakeMeter();
            }
        }
    }
}