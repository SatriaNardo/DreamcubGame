using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Unlocked Abilities (Metroidvania)")]
    [Tooltip("Uncheck these to start the game without them!")]
    [SerializeField] private bool isJumpUnlocked = true;
    [SerializeField] private bool isDashUnlocked = true;
    [SerializeField] private bool isAttackUnlocked = true;

    [Header("Animation")]
    [Tooltip("Drag your player's Animator here!")]
    [SerializeField] private Animator anim;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    private Vector2 moveInput;
    private bool isFacingRight = true;
    private bool canMove = true; 

    [Header("Jumping Mechanics (Tap-Only Fixed Height)")]
    [SerializeField] private float jumpForce = 15f; 
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask platformLayer;
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private bool isGrounded;

    [Header("Snappy Gravity System")]
    [SerializeField] private float baseGravityScale = 4f;    
    [SerializeField] private float fallMultiplier = 2f;      
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private LayerMask combinedGroundMask;

    [Header("Wall Sliding & Jumping")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = 3f;                    
    [SerializeField] private Vector2 wallJumpForce = new Vector2(12f, 16f); 
    [SerializeField] private float wallJumpDuration = 0.15f;    
    [SerializeField] private float wallCoyoteTime = 0.15f;
    private float wallCoyoteTimeCounter;
    private int wallDirection; 
    
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping = false; 

    [Header("Dashing")]
    [SerializeField] private float dashForce = 24f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Platform Drop")]
    [SerializeField] private float dropDownForce = 12f;

    [Header("Combat & Attacking")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackArea = new Vector2(1.5f, 1f);
    [SerializeField] private int attackDamage = 10; 
    [SerializeField] private LayerMask projectileLayer;
    [SerializeField] private LayerMask enemyLayer;  
    [SerializeField] private float attackCooldown = 0.4f;
    private float lastAttackTime;
    private bool isAttackingGizmo = false; 

    [SerializeField] private CameraFollow cameraFollow;

    private Vector3 originalOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        
        if (anim == null) anim = GetComponent<Animator>(); 
        if (anim == null) anim = GetComponentInChildren<Animator>();

        rb.gravityScale = baseGravityScale;
        combinedGroundMask = groundLayer | platformLayer;
        Physics2D.queriesStartInColliders = true;
    }

    void Update()
    {
        if (isDashing) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, combinedGroundMask);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);

        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (isTouchingWall && !isGrounded)
        {
            wallCoyoteTimeCounter = wallCoyoteTime;
            wallDirection = isFacingRight ? 1 : -1; 
        }
        else
        {
            wallCoyoteTimeCounter -= Time.deltaTime;
        }

        if (isTouchingWall && !isGrounded && Mathf.Abs(moveInput.x) > 0.1f && canMove)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }

        if (!isWallSliding && !isWallJumping && canMove) 
        {
            Flip();
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            

            if (isWallJumping)
            {
                canMove = true; 
                isWallJumping = false; 
            } else if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                coyoteTimeCounter = 0f;
            }
        }

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        if (canMove) rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (Mathf.Abs(moveInput.x) > 0.1f && isGrounded && canMove)
        {
            if (WakeManager.Instance != null) WakeManager.Instance.AddPassiveMovementWake();
        }

        if (isWallSliding)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(moveInput.x * 0.1f, -wallSlideSpeed);
        }
        else
        {
            if (rb.linearVelocity.y < -0.01f) 
            {
                rb.gravityScale = baseGravityScale * fallMultiplier;
            }
            else 
            {
                rb.gravityScale = baseGravityScale;
            }
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallSliding", isWallSliding);
    }

    public void OnMove(InputValue value) 
    { 
        moveInput = value.Get<Vector2>(); 
    }

    public void OnJump(InputValue value)
    {
        if (!isJumpUnlocked) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (!canMove) return;

        if (value.isPressed)
        {
            if (moveInput.y < -0.1f && isGrounded) StartCoroutine(DropThroughPlatform());
            else if (wallCoyoteTimeCounter > 0f) StartCoroutine(WallJumpRoutine());
            else if (coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                coyoteTimeCounter = 0f; 
                
                if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();
            }
        }
    }

    public void OnDash(InputValue value)
    {
        if (!isDashUnlocked) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (!canMove) return;

        if (value.isPressed && canDash && !isDashing) StartCoroutine(DashRoutine());
    }

    public void OnAttack(InputValue value)
    {
        if (!isAttackUnlocked) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (!canMove) return;

        if (value.isPressed && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();
        }
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallSliding = false;
        canMove = false; 
        isWallJumping = true; 
        wallCoyoteTimeCounter = 0f; 

        if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();

        float jumpDirection = -wallDirection; 
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpForce.x, wallJumpForce.y);

        if ((wallDirection == 1 && isFacingRight) || (wallDirection == -1 && !isFacingRight))
        {
            isFacingRight = !isFacingRight;
            if (isFacingRight) transform.rotation = Quaternion.Euler(0, 0, 0);
            else transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        yield return new WaitForSeconds(wallJumpDuration);

        if (isWallJumping)
        {
            canMove = true; 
            isWallJumping = false;  
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        if (anim != null) anim.SetTrigger("Attack");

        StartCoroutine(AttackGizmoVisual());

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        Collider2D[] hitProjectiles = Physics2D.OverlapBoxAll(attackPoint.position, attackArea, 0f, projectileLayer);
        foreach (Collider2D proj in hitProjectiles)
        {
            // Standard Enemy Projectile Parry
            EnemyProjectile p = proj.GetComponent<EnemyProjectile>(); 
            if (p != null)
            {
                Vector2 deflectDirection = (mouseWorldPosition - (Vector2)proj.transform.position).normalized;
                p.Deflect(deflectDirection);
                Debug.DrawLine(proj.transform.position, mouseWorldPosition, Color.yellow, 0.5f);
                if (WakeManager.Instance != null) WakeManager.Instance.RewardSuccessfulParry();
            }

            // --- NEW: Boss Cube Parry ---
            BossCube cube = proj.GetComponent<BossCube>();
            if (cube != null)
            {
                cube.DeflectToBoss(); // Automatically flies back to the boss!
                if (WakeManager.Instance != null) WakeManager.Instance.RewardSuccessfulParry();
            }
        }

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPoint.position, attackArea, 0f, enemyLayer);
        foreach (Collider2D enemyHit in hitEnemies)
        {
            MeleeEnemy meleeScript = enemyHit.GetComponent<MeleeEnemy>();
            if (meleeScript != null) meleeScript.TakeDamage(attackDamage);

            ShootingEnemy shooterScript = enemyHit.GetComponent<ShootingEnemy>(); 
            if (shooterScript != null) shooterScript.TakeDamage(attackDamage);

            FlyingMeleeEnemy flyingScript = enemyHit.GetComponent<FlyingMeleeEnemy>();
            if (flyingScript != null) flyingScript.TakeDamage(attackDamage);
            
            // --- NEW: Standard Melee Damage against Boss ---
            BossController bossScript = enemyHit.GetComponent<BossController>();
            if (bossScript != null) bossScript.TakeDamage(attackDamage);
        }
    }

    private IEnumerator AttackGizmoVisual()
    {
        isAttackingGizmo = true;
        yield return new WaitForSeconds(0.15f); 
        isAttackingGizmo = false;
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;
        
        if (anim != null) anim.SetTrigger("Dash");

        if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();
        rb.gravityScale = 0f;
        float dashDirection = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : (isFacingRight ? 1f : -1f);
        rb.linearVelocity = new Vector2(dashDirection * dashForce, 0f);
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private IEnumerator DropThroughPlatform()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.5f, platformLayer);
        if (hit.collider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, hit.collider, true);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropDownForce);
            yield return new WaitForSeconds(0.2f);
            Physics2D.IgnoreCollision(playerCollider, hit.collider, false);
        }
    }

    public void PlayHitAnimation()
    {
        if (anim != null) anim.SetTrigger("Hit");
    }

    private void Flip()
    {
        if (isFacingRight && moveInput.x < 0f || !isFacingRight && moveInput.x > 0f)
        {
            isFacingRight = !isFacingRight; 
            
            if (isFacingRight) transform.rotation = Quaternion.Euler(0, 0, 0);
            else transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
    
    public void RespawnAt(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero; 
        
        rb.gravityScale = baseGravityScale;
        isWallSliding = false;
        canMove = true;
        isDashing = false;
        isWallJumping = false; 
    }

    public void SetCutsceneMode(bool isLocked)
    {
        if (isLocked)
        {
            canMove = false;
            isDashing = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
            if (anim != null) anim.SetFloat("Speed", 0f); 
        }
        else
        {
            canMove = true;
        }
    }
    
    public void UnlockJump() { isJumpUnlocked = true; }
    public void UnlockDash() { isDashUnlocked = true; }
    public void UnlockAttack() { isAttackUnlocked = true; }

    private void OnDrawGizmos()
    {
        Gizmos.color = isAttackingGizmo ? Color.red : Color.white;
        
        if (attackPoint != null) Gizmos.DrawWireCube(attackPoint.position, attackArea);
        
        if (groundCheck != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundCheck.position, 0.2f); }
        if (wallCheck != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(wallCheck.position, 0.2f); }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LookDown"))
        {
            originalOffset = cameraFollow.offset;
            Vector3 newOffset = cameraFollow.offset;
            newOffset.y = -4f;
            cameraFollow.offset = newOffset;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("LookDown"))
        {
            cameraFollow.offset = originalOffset;
        }
    }
}