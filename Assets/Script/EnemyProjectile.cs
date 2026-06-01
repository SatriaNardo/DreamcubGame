using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f;

    [Header("Sprite Orientation")]
    [Tooltip("Check this if your bullet sprite points UP by default. Leave unchecked if it points RIGHT.")]
    [SerializeField] private bool spriteFacesUp = false;

    private Rigidbody2D rb;
    private bool isDeflected = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime); // Automatically destroy the bullet after a few seconds to prevent lag!
    }

    // Called by the ShootingEnemy when the bullet spawns
    public void SetDirection(Vector2 direction)
    {
        Vector2 moveDir = direction.normalized;
        rb.linearVelocity = moveDir * speed;
        
        FaceDirection(moveDir);
    }

    // Called by your PlayerController when the player attacks the bullet
    public void Deflect(Vector2 deflectDirection)
    {
        isDeflected = true;
        
        // Boost the speed of the deflected bullet to make the parry feel powerful!
        Vector2 moveDir = deflectDirection.normalized;
        rb.linearVelocity = moveDir * (speed * 1.5f); 
        
        FaceDirection(moveDir);

        // Change the bullet's color so the player knows it belongs to them now!
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    // --- THE ROTATION MATH ---
    private void FaceDirection(Vector2 dir)
    {
        // Calculate the angle in degrees
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // If your art asset was drawn pointing UP instead of RIGHT, we subtract 90 degrees to fix it
        if (spriteFacesUp)
        {
            angle -= 90f;
        }

        // Apply the rotation to the Z-axis (2D rotation)
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If the player gets hit by a normal bullet
        if (collision.CompareTag("Player") && !isDeflected)
        {
            if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
            Destroy(gameObject);
        }
        // If the bullet was deflected and hits an enemy
        else if (isDeflected && (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy")))
        {
            // Try to hurt the Melee Enemy
            MeleeEnemy melee = collision.GetComponent<MeleeEnemy>();
            if (melee != null) melee.TakeDamage(damage * 2); // Parries deal double damage!

            // Try to hurt the Shooting Enemy
            ShootingEnemy shooter = collision.GetComponent<ShootingEnemy>();
            if (shooter != null) shooter.TakeDamage(damage * 2);

            Destroy(gameObject);
        }
        // Destroy the bullet if it hits a wall/ground
        else if (collision.gameObject.layer == LayerMask.NameToLayer("SolidGround"))
        {
            Destroy(gameObject);
        }
    }
}