using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 500;
    private int currentHealth;
    [SerializeField] private float moveSpeed = 3f;
    
    private bool hasFightStarted = false; 
    private bool isPhaseTwo = false;
    private bool isActing = false; 
    
    // --- NEW: Stores where the boss wants to go ---
    private float targetVelocityX = 0f; 

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;

    [Header("Attacks")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private int meleeDamage = 20;
    
    [Header("Special 1: Cube Summon")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Transform summonPoint;

    void Start()
    {
        currentHealth = maxHealth;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (!hasFightStarted || isActing || player == null) 
        {
            targetVelocityX = 0f;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        FlipTowardsPlayer();

        if (distanceToPlayer > attackRange)
        {
            anim.SetBool("isWalking", true);
            float moveDirection = (player.position.x > transform.position.x) ? 1f : -1f;
            
            // Remember the speed, but move the physics body in FixedUpdate
            targetVelocityX = moveDirection * moveSpeed; 
        }
        else
        {
            anim.SetBool("isWalking", false);
            targetVelocityX = 0f; // Stop moving
            
            StartCoroutine(ChooseAttackRoutine());
        }
    }

    // --- NEW: FixedUpdate handles the actual Rigidbody movement smoothly ---
    void FixedUpdate()
    {
        if (hasFightStarted && !isActing)
        {
            // Apply the movement
            rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        }
        else if (isActing)
        {
            // Force the boss to stop sliding if they are swinging a weapon
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    public void StartBossFight()
    {
        Debug.Log("BOSS SIGNAL RECEIVED: The cutscene told the boss to wake up!");
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        isActing = true;
        anim.Play("Boss_Spawn");
        
        yield return new WaitForSeconds(2f); 
        
        Debug.Log("SPAWN FINISHED: The boss is now actively hunting the player!");
        hasFightStarted = true;
        isActing = false;
    }

    private IEnumerator ChooseAttackRoutine()
    {
        isActing = true;
        anim.SetBool("isWalking", false);
        targetVelocityX = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 

        int attackChoice = Random.Range(0, 2);

        if (attackChoice == 0)
        {
            Debug.Log("BOSS ATTACK: Melee!");
            anim.SetTrigger("MeleeAttack");
            yield return new WaitForSeconds(0.5f); 
            if (Vector2.Distance(transform.position, player.position) <= attackRange)
            {
                player.GetComponent<PlayerController>().PlayHitAnimation();
            }
            yield return new WaitForSeconds(0.5f); 
        }
        else
        {
            Debug.Log("BOSS ATTACK: Summoning Cube!");
            anim.SetTrigger("SummonCube");
            yield return new WaitForSeconds(0.8f); 
            
            if (cubePrefab != null && summonPoint != null)
            {
                GameObject cube = Instantiate(cubePrefab, summonPoint.position, Quaternion.identity);
                cube.GetComponent<BossCube>().Initialize(player, transform);
            }
            yield return new WaitForSeconds(0.8f); 
        }

        yield return new WaitForSeconds(attackCooldown);
        isActing = false;
    }

    public void TakeDamage(int damage)
    {
        if (!hasFightStarted) return;

        currentHealth -= damage;
        anim.SetTrigger("Hit");

        if (currentHealth <= maxHealth / 2 && !isPhaseTwo)
        {
            StartCoroutine(PhaseTwoTransitionRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator PhaseTwoTransitionRoutine()
    {
        isPhaseTwo = true;
        isActing = true;
        targetVelocityX = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 

        anim.Play("Boss_PhaseTransition");
        yield return new WaitForSeconds(1.5f); 

        moveSpeed += 2f; 
        attackCooldown -= 0.75f;

        yield return new WaitForSeconds(1f); 
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
        targetVelocityX = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.Play("Boss_Death");
        Destroy(gameObject, 2f);
    }
}