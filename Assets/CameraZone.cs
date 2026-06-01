using UnityEngine;
using Unity.Collections;

public class CameraZone : MonoBehaviour
{
    [Header("Virtual Camera Reference")]
    [SerializeField] private GameObject virtualCamera;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object entering the zone is the Player
        if (collision.CompareTag("Player"))
        {
            // Turn on this room's camera
            if (virtualCamera != null)
            {
                virtualCamera.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Turn off this room's camera when leaving
            if (virtualCamera != null)
            {
                virtualCamera.SetActive(false);
            }
        }
    }
}