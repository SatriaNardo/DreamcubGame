using System.Collections;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f; 
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attacking & Telegraphing")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private SpriteRenderer visualIndicator; // Drag the AttackIndicator here
    [SerializeField] private float attackHitboxRadius = 1f;
    [SerializeField] private float windUpTime = 0.5f; 
    [SerializeField] private float attackActiveTime = 0.15f; 
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Stun Settings")]
    [SerializeField] private float stunDuration = 2f;

    private Transform playerTransform;
    private PlayerController playerScript;
    private Rigidbody2D rb;
    
    private bool isFacingRight = true;
    private bool isStunned = false;
    private bool isAttacking = false;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerScript = player.GetComponent<PlayerController>();
        }

        // Ensure the indicator is safely hidden at launch
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

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        
        // Stop the enemy from moving horizontally while swinging their weapon
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 

        if (visualIndicator != null)
        {
            // 1. WIND UP PHASE: Turn on and animate the expanding yellow ring
            visualIndicator.gameObject.SetActive(true);
            visualIndicator.color = new Color(1f, 0.92f, 0.016f, 0.4f); // Semi-transparent Yellow
            
            float elapsed = 0f;
            Vector3 targetScale = new Vector3(attackHitboxRadius * 2, attackHitboxRadius * 2, 1f);
            
            // Smoothly grow the ring over the duration of the wind-up time
            while (elapsed < windUpTime)
            {
                elapsed += Time.deltaTime;
                visualIndicator.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / windUpTime);
                yield return null;
            }

            // 2. STRIKE PHASE: Hitbox becomes dangerous (Snap color to Red)
            visualIndicator.color = new Color(1f, 0f, 0f, 0.6f); // Semi-transparent Red
        }

        // --- PARRY / CLASH DETECTION SYSTEM ---
        // If the player timed their strike perfectly and is currently attacking on this exact frame:
        if (playerScript != null && playerScript.IsCurrentlyAttacking)
        {
            // Reward the player by refunding the 30% action spike penalty from the Wake Meter
            if (WakeManager.Instance != null)
            {
                WakeManager.Instance.RewardSuccessfulParry();
            }

            // Interrupt the enemy's attack execution and force them into a stunned state
            StartCoroutine(StunRoutine());
            yield break; // Exit the coroutine early so the player doesn't get hurt
        }

        // --- DAMAGE CALCULATION ---
        // If the player did not parry, create an overlap circle to check if they are standing in the blast zone
        Collider2D playerHit = Physics2D.OverlapCircle(attackPoint.position, attackHitboxRadius, LayerMask.GetMask("Default")); 
        
        if (playerHit != null && playerHit.CompareTag("Player"))
        {
            Debug.Log("Player missed the parry window and was hit by the enemy slash!");
            // TODO: Subtract player health or increase Wake Parameter heavily here!
        }

        // Leave the red active damage hitbox lingering in the world for a fraction of a second
        yield return new WaitForSeconds(attackActiveTime);

        // 3. RESET PHASE: Clean up variables and hide sprites for the next loop
        ResetAttackVisuals();
    }

    private IEnumerator StunRoutine()
    {
        Debug.Log("CLASH! Enemy is STUNNED!");
        isStunned = true;
        ResetAttackVisuals();
        rb.linearVelocity = Vector2.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.blue;

        yield return new WaitForSeconds(stunDuration);

        if (sr != null) sr.color = Color.white;
        isStunned = false;
        lastAttackTime = Time.time; 
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