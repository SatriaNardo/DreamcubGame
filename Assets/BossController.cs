using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 500;
    private int currentHealth;
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    private Color originalColor;

    private bool hasFightStarted = false; 
    private bool isPhaseTwo = false;
    private bool isActing = false; 

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;

    [Header("Attacks")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2.5f;
    
    [Header("Special: Cube Summon")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Transform summonPoint;

    void Start()
    {
        currentHealth = maxHealth;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        rb.gravityScale = 0f; 
        rb.isKinematic = true; 
    }

    void Update()
    {
        if (hasFightStarted && !isActing && player != null)
        {
            FlipTowardsPlayer();
        }
    }

    void FixedUpdate()
    {
        if (!hasFightStarted || isActing)
        {
            rb.linearVelocity = Vector2.zero;
            return; 
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            anim.SetBool("isWalking", true);
            Vector2 moveDirection = (player.position - transform.position).normalized;
            rb.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            anim.SetBool("isWalking", false);
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(ChooseAttackRoutine());
        }
    }

    public void StartBossFight()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        isActing = true;
        rb.isKinematic = true;
        anim.Play("Boss_Spawn");
        
        yield return new WaitForSeconds(4.2f); 
        
        rb.isKinematic = false;
        hasFightStarted = true;
        isActing = false;
    }

    private IEnumerator ChooseAttackRoutine()
    {
        isActing = true;
        anim.SetBool("isWalking", false);
        rb.linearVelocity = Vector2.zero;

        int attackChoice = Random.Range(0, 2);

        if (attackChoice == 0) // Melee
        {
            anim.SetTrigger("MeleeAttack");
            yield return new WaitForSeconds(1.0f); 
            
            if (Vector2.Distance(transform.position, player.position) <= attackRange)
            {
                player.GetComponent<PlayerController>().PlayHitAnimation();
                if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
            }
        }
        else // Cube
        {
            anim.SetTrigger("SummonCube");
            yield return new WaitForSeconds(1.6f); 
            
            if (cubePrefab != null && summonPoint != null)
            {
                GameObject cube = Instantiate(cubePrefab, summonPoint.position, Quaternion.identity);
                cube.GetComponent<BossCube>().Initialize(player, transform);
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        isActing = false;
    }

    public void TakeDamage(int damage)
    {
        if (!hasFightStarted) return; 

        currentHealth -= damage;
        StartCoroutine(TriggerHitFlash());

        if (currentHealth <= maxHealth / 2 && !isPhaseTwo)
        {
            StartCoroutine(PhaseTwoTransitionRoutine());
        }

        if (currentHealth <= 0) Die();
    }

    private IEnumerator TriggerHitFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator PhaseTwoTransitionRoutine()
    {
        isPhaseTwo = true;
        isActing = true;
        anim.Play("Boss_PhaseTransition");
        yield return new WaitForSeconds(1.5f); 
        moveSpeed += 2f; 
        attackCooldown -= 0.75f;
        isActing = false;
    }

    private void FlipTowardsPlayer()
    {
        if (player.position.x > transform.position.x) transform.rotation = Quaternion.Euler(0, 0, 0);
        else transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    private void Die()
    {
        isActing = true;
        hasFightStarted = false;
        rb.linearVelocity = Vector2.zero;
        anim.Play("Boss_Death");
        Destroy(gameObject, 2f);
    }
}