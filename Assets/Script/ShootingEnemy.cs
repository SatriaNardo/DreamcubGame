using System.Collections;
using UnityEngine;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Health & Stats")]
    [SerializeField] private int maxHealth = 20; 
    private int currentHealth;

    [Header("Targeting & Timing")]
    [SerializeField] private float shootingRange = 10f;
    [SerializeField] private float fireRate = 2f; 
    [Tooltip("How long does the enemy prepare before the bullet actually spawns?")]
    [SerializeField] private float shootWindUpTime = 0.5f; 
    private float fireCooldownTimer;

    [Header("Animation Settings")]
    [Tooltip("How long does the death animation take before the body disappears?")]
    [SerializeField] private float deathAnimationLength = 1.5f;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; 

    private Transform playerTransform;
    private SpriteRenderer sr;
    private Animator anim; 
    private Rigidbody2D rb; 
    
    private Vector3 startPosition; 
    private Vector3 baseScale; 
    
    private bool isDead = false;
    private bool isShooting = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); 
        
        currentHealth = maxHealth;
        startPosition = transform.position; 
        baseScale = transform.localScale;
        
        if (WakeManager.Instance != null) WakeManager.Instance.RegisterShootingEnemy(this);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || isDead || isShooting) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        fireCooldownTimer += Time.deltaTime;

        if (distanceToPlayer <= shootingRange)
        {
            LookAtPlayer();

            if (fireCooldownTimer >= fireRate)
            {
                StartCoroutine(ShootRoutine());
                fireCooldownTimer = 0f; 
            }
        }
    }

    private IEnumerator ShootRoutine()
    {
        isShooting = true;
        
        if (anim != null) anim.SetTrigger("Shoot");

        yield return new WaitForSeconds(shootWindUpTime);

        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 direction = (playerTransform.position - firePoint.position);
            GameObject newBullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            EnemyProjectile projectileScript = newBullet.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.SetDirection(direction);
            }
        }

        isShooting = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0) 
        {
            // Stop the wind-up if they die while preparing to shoot!
            StopAllCoroutines(); 
            StartCoroutine(DeathRoutine());
        }
        else
        {
            StartCoroutine(DamageFlash());
        }
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

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isShooting = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (anim != null)
        {
            anim.ResetTrigger("Shoot"); // Clear sticky triggers
            anim.SetTrigger("Die");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(deathAnimationLength);
        
        gameObject.SetActive(false);
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        
        transform.position = startPosition;
        transform.localScale = baseScale; 
        currentHealth = maxHealth;
        fireCooldownTimer = 0f;
        isDead = false;
        isShooting = false;
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (sr != null) sr.color = Color.white;
        
        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Shoot");
            anim.Play("Idle"); 
        }
        
        gameObject.SetActive(true);
    }

    private void LookAtPlayer()
    {
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