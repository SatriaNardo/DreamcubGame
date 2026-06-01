using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool isActivated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If the player touches this checkpoint and it hasn't been used yet...
        if (collision.CompareTag("Player") && !isActivated)
        {
            isActivated = true; // Mark as used so we don't spam the manager
            
            // Tell the WakeManager to save this exact position
            if (WakeManager.Instance != null)
            {
                WakeManager.Instance.SetRespawnPoint(transform.position);
            }
        }
    }

    // Draws a blue circle in the editor so you can see where players will respawn
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}