using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoom : MonoBehaviour
{
    private BoxCollider2D roomCollider;

    void Start()
    {
        roomCollider = GetComponent<BoxCollider2D>();
        
        // Safety check to ensure it's always a trigger!
        roomCollider.isTrigger = true; 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // When the player walks into this room zone...
        if (collision.CompareTag("Player"))
        {
            // Find the Main Camera and give it this room's exact dimensions
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            
            if (camFollow != null)
            {
                camFollow.SetBounds(roomCollider.bounds);
            }
        }
    }
}