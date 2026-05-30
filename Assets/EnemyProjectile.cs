using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    private Vector2 travelDirection;
    private bool isDeflected = false; // Tracks if the player hit it

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 direction)
    {
        travelDirection = direction.normalized;
    }

    // New Function: Called by the player's sword
    public void Deflect(Vector2 newDirection)
    {
        isDeflected = true;
        travelDirection = newDirection.normalized;
        speed *= 1.5f; // Optional: Make the bullet fly back even faster!
        
        // Optional visual flair: turn the bullet a different color when deflected
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    void Update()
    {
        transform.Translate(travelDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. If NOT deflected and hits the Player -> Hurt Player
        if (!isDeflected && collision.CompareTag("Player"))
        {
            Debug.Log("Player was hit by a projectile!");
            // (Increase Wake Parameter here later)
            Destroy(gameObject);
        }
        // 2. If DEFLECTED and hits an Enemy -> Hurt Enemy
        else if (isDeflected && collision.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit by deflected bullet!");
            Destroy(collision.gameObject); // Destroys the enemy!
            Destroy(gameObject);           // Destroys the bullet
        }
        // 3. Hits a wall/floor
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}