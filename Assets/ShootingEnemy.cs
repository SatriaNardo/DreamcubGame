using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float shootingRange = 10f;
    [SerializeField] private float fireRate = 2f; // Seconds between shots
    private float fireCooldownTimer;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; // Where the bullet spawns

    private Transform playerTransform;

    void Start()
    {
        // Find the player automatically using their Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Update the reload timer
        fireCooldownTimer += Time.deltaTime;

        // If the player is close enough, aim and fire
        if (distanceToPlayer <= shootingRange)
        {
            // Simple visual flip to face the player direction
            LookAtPlayer();

            if (fireCooldownTimer >= fireRate)
            {
                Shoot();
                fireCooldownTimer = 0f; // Reset cooldown
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // 1. Calculate direction vector from enemy to player
        Vector2 direction = (playerTransform.position - firePoint.position);

        // 2. Spawn the projectile prefab at the firePoint position
        GameObject newBullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // 3. Send the calculated vector direction over to the projectile's movement script
        Projectile projectileScript = newBullet.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDirection(direction);
        }
    }

    private void LookAtPlayer()
    {
        // Flip the enemy graphic depending on whether the player is to their left or right
        if (playerTransform.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f); // Facing Left
        }
        else
        {
            transform.localScale = new Vector3(1f, 1f, 1f);  // Facing Right
        }
    }

    // Draws a helper visual circle in the editor scene view to show the enemy's vision range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}