using System.Collections;
using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Health & Stats")]
    [SerializeField] private int maxHealth = 20; 
    private int currentHealth;

    [Header("Targeting")]
    [SerializeField] private float shootingRange = 10f;
    [SerializeField] private float fireRate = 2f; 
    private float fireCooldownTimer;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; 

    private Transform playerTransform;
    private SpriteRenderer sr;
    
    private Vector3 startPosition; 
    private Vector3 baseScale; // NEW: Memorizes your custom scale in the editor!

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        startPosition = transform.position; 
        
        // Memorize whatever scale you set in the Unity Inspector!
        baseScale = transform.localScale;
        
        // Register this enemy to the WakeManager master list
        if (WakeManager.Instance != null) WakeManager.Instance.RegisterShootingEnemy(this);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        fireCooldownTimer += Time.deltaTime;

        if (distanceToPlayer <= shootingRange)
        {
            LookAtPlayer();

            if (fireCooldownTimer >= fireRate)
            {
                Shoot();
                fireCooldownTimer = 0f; 
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 direction = (playerTransform.position - firePoint.position);
        GameObject newBullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        EnemyProjectile projectileScript = newBullet.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDirection(direction);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator DamageFlash()
    {
        if (sr != null) 
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        
        // Reset position, stats, and scale!
        transform.position = startPosition;
        transform.localScale = baseScale; // FIX: Ensure scale is correct on respawn
        currentHealth = maxHealth;
        fireCooldownTimer = 0f;
        
        // Fix visuals
        if (sr != null) sr.color = Color.white;
        
        // Wake back up!
        gameObject.SetActive(true);
    }

    private void LookAtPlayer()
    {
        // FIX: Now uses your original size and just adds a negative sign when flipping!
        if (playerTransform.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z); 
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);  
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}