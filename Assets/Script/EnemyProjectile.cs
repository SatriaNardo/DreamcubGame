using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Stats")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 15;
    
    private Rigidbody2D rb;
    private bool isDeflected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called by the ShootingEnemy to fire directly at the player
    public void SetDirection(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;
    }

    // Called by the PlayerController when you successfully strike the bullet
    public void Deflect(Vector2 newDirection)
    {
        isDeflected = true;
        
        // Boost the speed slightly so deflected shots feel powerful!
        rb.linearVelocity = newDirection * (speed * 1.5f); 
        
        // Change the bullet's color to yellow to show the player it is now "friendly"
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. If it hits the player and HAS NOT been deflected yet
        if (!isDeflected && collision.CompareTag("Player"))
        {
            if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
            Destroy(gameObject);
        }
        
        // 2. If it hits an enemy and HAS been deflected by the player
        else if (isDeflected && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            MeleeEnemy melee = collision.GetComponent<MeleeEnemy>();
            if (melee != null) melee.TakeDamage(damage * 2);

            // --- RENAMED TO SHOOTING ENEMY ---
            ShootingEnemy shooter = collision.GetComponent<ShootingEnemy>();
            if (shooter != null) shooter.TakeDamage(damage * 2);

            Destroy(gameObject);
        }

        // 3. Destroy the bullet if it hits a wall or the floor
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || 
                 collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}