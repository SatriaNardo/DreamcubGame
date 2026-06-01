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
        if (playerTransform == null || isStunned || isAttacking || isDead) 
        {
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
            MoveTowardsPlayer();
            if (anim != null) anim.SetBool("isWalking", true); 
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("isWalking", false); 
        }
    }

    private void MoveTowardsPlayer()
    {
        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    public void TakeDamage(int baseDamage)
    {
        if (isDead) return; 

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

        if (currentHealth <= 0) 
        {
            StartCoroutine(DeathRoutine()); 
        }
    }

    private IEnumerator AttackRoutine() 
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        
        // 1. Start the animation immediately
        if (anim != null) anim.SetTrigger("Attack"); 

        // 2. Show the indicator instantly at FULL size (No yellow growing phase)
        if (visualIndicator != null)
        {
            visualIndicator.gameObject.SetActive(true);
            visualIndicator.color = new Color(1f, 0f, 0f, 0.4f); // Semi-transparent warning
            
            float correctedScaleX = (attackHitboxRadius * 2) / Mathf.Abs(baseScale.x);
            float correctedScaleY = (attackHitboxRadius * 2) / Mathf.Abs(baseScale.y);
            
            visualIndicator.transform.localScale = new Vector3(correctedScaleX, correctedScaleY, 1f); 
        }

        // 3. Wait for the animation to play out!
        yield return new WaitForSeconds(windUpTime);

        // Flash solid red right as the damage hits
        if (visualIndicator != null)
        {
            visualIndicator.color = new Color(1f, 0f, 0f, 0.8f); 
        }

        // 4. Deal damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackHitboxRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
                break; 
            }
        }

        // 5. Keep the indicator visible briefly
        yield return new WaitForSeconds(attackActiveTime);
        ResetAttackVisuals();
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        ResetAttackVisuals();
        rb.linearVelocity = Vector2.zero;
        
        // --- FIX: Tell the Animator to forget the attack command! ---
        if (anim != null) 
        {
            anim.ResetTrigger("Attack"); // Clears the sticky trigger
            anim.SetBool("isWalking", false);
            anim.Play("Idle"); // Now it will safely stay in Idle
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
        
        // FIX: Stop moving and turn off gravity!
        rb.linearVelocity = Vector2.zero; 
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        if (anim != null) anim.SetTrigger("Die"); 

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

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
        
        // FIX: Turn gravity back on!
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        
        transform.localScale = baseScale;
        isFacingRight = true;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

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