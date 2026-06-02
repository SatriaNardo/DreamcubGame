using System.Collections;
using UnityEngine;

public class FlyingMeleeEnemy : MonoBehaviour
{
    [Header("Health & Stats")]
    [SerializeField] private int maxHealth = 20;
    private int currentHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f; 
    [SerializeField] private float chaseRange = 14f;
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

    [Header("Animation Settings")]
    [Tooltip("How long does the death animation take before the body disappears?")]
    [SerializeField] private float deathAnimationLength = 1.5f;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim; 
    
    private bool isFacingRight = true;
    private bool isStunned = false;
    private bool isAttacking = false;
    private bool isDead = false; 
    private float lastAttackTime;
    
    private Vector3 startPosition; 
    private Vector3 baseScale; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); 
        
        currentHealth = maxHealth;
        startPosition = transform.position; 
        baseScale = transform.localScale; 
        
        // Force gravity to 0 so the enemy floats!
        if (rb != null) rb.gravityScale = 0f;
        
        if (WakeManager.Instance != null) WakeManager.Instance.RegisterFlyingEnemy(this);

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
        if (playerTransform == null || isStunned || isAttacking || isDead) 
        {
            // Stop mid-air if stunned or attacking
            if (!isDead && !isAttacking) rb.linearVelocity = Vector2.zero; 
            
            // --- CHANGED to isWalking ---
            if (anim != null) anim.SetBool("isWalking", false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        HandleFacing();

        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (distanceToPlayer <= chaseRange && distanceToPlayer > attackRange)
        {
            FlyTowardsPlayer();
            // --- CHANGED to isWalking ---
            if (anim != null) anim.SetBool("isWalking", true); 
        }
        else
        {
            // Stop moving if the player is too far away
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 5f);
            // --- CHANGED to isWalking ---
            if (anim != null) anim.SetBool("isWalking", false); 
        }
    }

    private void FlyTowardsPlayer()
    {
        // Get the direction to the player in both X and Y
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        
        // Apply velocity in all directions
        rb.linearVelocity = direction * moveSpeed;
    }

    public void TakeDamage(int baseDamage)
    {
        if (isDead) return; 

        int finalDamage = baseDamage;

        // Critical Parry Logic
        if (isAttacking)
        {
            finalDamage *= 2; 
            StopAllCoroutines(); 
            isAttacking = false;
            
            if (WakeManager.Instance != null) WakeManager.Instance.RewardSuccessfulParry();
            StartCoroutine(StunRoutine());
            Debug.Log($"CRITICAL PARRY! Flying Enemy takes {finalDamage} damage!");
        }

        currentHealth -= finalDamage;

        if (currentHealth <= 0) 
        {
            StopAllCoroutines();
            StartCoroutine(DeathRoutine()); 
        }
        else
        {
            StartCoroutine(DamageFlash());
        }
    }

    private IEnumerator AttackRoutine() 
    {
        isAttacking = true;
        
        // Lock position while winding up
        rb.linearVelocity = Vector2.zero; 
        
        if (anim != null) anim.SetTrigger("Attack"); 

        if (visualIndicator != null)
        {
            visualIndicator.gameObject.SetActive(true);
            visualIndicator.color = new Color(1f, 0f, 0f, 0.4f);
            
            float correctedScaleX = (attackHitboxRadius * 2) / Mathf.Abs(baseScale.x);
            float correctedScaleY = (attackHitboxRadius * 2) / Mathf.Abs(baseScale.y);
            visualIndicator.transform.localScale = new Vector3(correctedScaleX, correctedScaleY, 1f); 
        }

        yield return new WaitForSeconds(windUpTime);

        if (visualIndicator != null)
        {
            visualIndicator.color = new Color(1f, 0f, 0f, 0.8f); 
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackHitboxRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
                
                // Trigger the player's hit animation directly
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null) pc.PlayHitAnimation();
                
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
        
        // Add a tiny bit of gravity or knockback here if you want them to fall when parried!
        rb.linearVelocity = Vector2.zero;
        
        if (anim != null) 
        {
            anim.ResetTrigger("Attack"); 
            // --- CHANGED to isWalking ---
            anim.SetBool("isWalking", false);
            anim.Play("Idle"); 
        }

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

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        ResetAttackVisuals();
        
        rb.linearVelocity = Vector2.zero; 
        
        // Turn on gravity so their dead body falls out of the sky!
        rb.gravityScale = 2f; 
        
        if (anim != null)
        {
            anim.ResetTrigger("Attack"); 
            anim.SetTrigger("Die");
        }

        // Disable triggers but keep physics so they can hit the ground
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false; 

        yield return new WaitForSeconds(deathAnimationLength);
        gameObject.SetActive(false);
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        
        transform.position = startPosition;
        currentHealth = maxHealth;
        isStunned = false;
        isAttacking = false;
        isDead = false;
        
        // Reset gravity back to floating
        rb.gravityScale = 0f; 
        rb.linearVelocity = Vector2.zero;
        
        transform.localScale = baseScale;
        isFacingRight = true;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) 
        {
            col.enabled = true;
            col.isTrigger = true; 
        }

        if (sr != null) sr.color = Color.white;
        ResetAttackVisuals();
        
        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Attack");
            anim.Play("Idle"); 
        }
        
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
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else if (playerTransform.position.x > transform.position.x && !isFacingRight)
        {
            isFacingRight = true;
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}