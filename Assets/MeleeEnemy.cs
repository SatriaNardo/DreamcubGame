using System.Collections;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    [Header("Health & Stats")]
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f; 
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attacking & Telegraphing")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private SpriteRenderer visualIndicator; 
    [SerializeField] private float attackHitboxRadius = 1f;
    [SerializeField] private float windUpTime = 0.5f; 
    [SerializeField] private float attackActiveTime = 0.15f; 
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 2f;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    
    private bool isFacingRight = true;
    private bool isStunned = false;
    private bool isAttacking = false;
    private float lastAttackTime;
    
    private Vector3 startPosition; // NEW: Memorizes where it spawns

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        startPosition = transform.position; // Save spawn point
        
        // Register this enemy to the WakeManager master list
        if (WakeManager.Instance != null) WakeManager.Instance.RegisterMeleeEnemy(this);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (visualIndicator != null)
        {
            visualIndicator.gameObject.SetActive(false);
            visualIndicator.transform.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        if (playerTransform == null || isStunned || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        HandleFacing();

        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (distanceToPlayer <= chaseRange && distanceToPlayer > attackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void MoveTowardsPlayer()
    {
        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    public void TakeDamage(int baseDamage)
    {
        int finalDamage = baseDamage;

        if (isAttacking)
        {
            finalDamage *= 2; 
            StopAllCoroutines(); 
            isAttacking = false;
            
            if (WakeManager.Instance != null) WakeManager.Instance.RewardSuccessfulParry();
            StartCoroutine(StunRoutine());
            Debug.Log($"CRITICAL PARRY! Enemy takes {finalDamage} damage!");
        }

        currentHealth -= finalDamage;
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 

        if (visualIndicator != null)
        {
            visualIndicator.gameObject.SetActive(true);
            visualIndicator.color = new Color(1f, 0.92f, 0.016f, 0.4f); 
            
            float elapsed = 0f;
            Vector3 targetScale = new Vector3(attackHitboxRadius * 2, attackHitboxRadius * 2, 1f);
            
            while (elapsed < windUpTime)
            {
                elapsed += Time.deltaTime;
                visualIndicator.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / windUpTime);
                yield return null;
            }
            visualIndicator.color = new Color(1f, 0f, 0f, 0.6f); 
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackHitboxRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
                break; 
            }
        }

        yield return new WaitForSeconds(attackActiveTime);
        ResetAttackVisuals();
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        ResetAttackVisuals();
        rb.linearVelocity = Vector2.zero;

        if (sr != null) sr.color = Color.blue;

        yield return new WaitForSeconds(stunDuration);

        if (sr != null) sr.color = Color.white;
        isStunned = false;
        lastAttackTime = Time.time; 
    }

    private IEnumerator DamageFlash()
    {
        if (sr != null && !isStunned) 
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
    }

    // --- UPDATED DEATH LOGIC ---
    private void Die()
    {
        // Deactivate instead of Destroy!
        gameObject.SetActive(false);
    }

    // --- NEW RESPAWN LOGIC ---
    public void ResetEnemy()
    {
        StopAllCoroutines();
        
        // Reset position and stats
        transform.position = startPosition;
        currentHealth = maxHealth;
        isStunned = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;
        
        // Fix visuals
        if (sr != null) sr.color = Color.white;
        ResetAttackVisuals();
        
        // Wake back up!
        gameObject.SetActive(true);
    }

    private void ResetAttackVisuals()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        if (visualIndicator != null)
        {
            visualIndicator.gameObject.SetActive(false);
            visualIndicator.transform.localScale = Vector3.zero;
        }
    }

    private void HandleFacing()
    {
        if (playerTransform.position.x < transform.position.x && isFacingRight)
        {
            isFacingRight = false;
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (playerTransform.position.x > transform.position.x && !isFacingRight)
        {
            isFacingRight = true;
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}