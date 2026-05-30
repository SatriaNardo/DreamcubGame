using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    private Vector2 moveInput;
    private bool isFacingRight = true;
    private bool canMove = true; // Momentarily locks horizontal input during a wall jump

    [Header("Jumping & Snappy Gravity")]
    [SerializeField] private float jumpForce = 16f;         
    [SerializeField] private float baseGravityScale = 4f;    
    [SerializeField] private float fallMultiplier = 2f;      
    [SerializeField] private float lowJumpMultiplier = 1.5f; 
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask platformLayer;
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private bool isGrounded;
    private bool isHoldingJump;

    [Header("Wall Sliding & Jumping")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = 3f;                    // Desired descent glide speed
    [SerializeField] private Vector2 wallJumpForce = new Vector2(12f, 16f); // X pushes away, Y pushes up
    [SerializeField] private float wallJumpDuration = 0.15f;               // Control lock window duration
    private bool isTouchingWall;
    private bool isWallSliding;

    [Header("Dashing")]
    [SerializeField] private float dashForce = 24f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Platform Drop")]
    [SerializeField] private float dropDownForce = 12f;
    private Collider2D playerCollider;
    private Rigidbody2D rb;
    private LayerMask combinedGroundMask;

    [Header("Attacking")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask projectileLayer;
    [SerializeField] private float attackCooldown = 0.4f;
    private float lastAttackTime;
    private bool isAttackingGizmo = false; 

    public bool IsCurrentlyAttacking { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        
        rb.gravityScale = baseGravityScale;
        combinedGroundMask = groundLayer | platformLayer;
        
        // Ensures our overlap circles register correctly with platform colliders
        Physics2D.queriesStartInColliders = true;
    }

    void Update()
    {
        if (isDashing) return;

        // Environmental radar updates
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, combinedGroundMask);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);

        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        // Check if player is actively holding into a wall to slide
        // Check if player is holding into a wall, BUT ignore it if we are currently wall jumping!
        if (isTouchingWall && !isGrounded && Mathf.Abs(moveInput.x) > 0.1f && canMove)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }

        // Prevent flipping sprites back and forth while actively sliding down a surface
        if (!isWallSliding)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // Apply horizontal walking movement if controls aren't locked by a wall jump
        if (canMove)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }

        // Passive movement wake accumulation
        if (Mathf.Abs(moveInput.x) > 0.1f && isGrounded)
        {
            if (WakeManager.Instance != null) WakeManager.Instance.AddPassiveMovementWake();
        }

        // --- WALL ENGINE: SLIDING VS FREE FALL ---
        if (isWallSliding)
        {
            // Kill gravity forces entirely to stop physics threshold engine locks
            rb.gravityScale = 0f;

            // Apply a minor X force directly into the wall along with a locked vertical descent speed.
            // This micro-input completely prevents the random sticking glitch!
            rb.linearVelocity = new Vector2(moveInput.x * 0.1f, -wallSlideSpeed);
        }
        else
        {
            // Classic fast platformer fall gravity curves when traveling in free space
            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = baseGravityScale * fallMultiplier;
            }
            else if (rb.linearVelocity.y > 0 && !isHoldingJump)
            {
                rb.gravityScale = baseGravityScale * lowJumpMultiplier;
            }
            else
            {
                rb.gravityScale = baseGravityScale;
            }
        }
    }

    public void OnMove(InputValue value) 
    { 
        moveInput = value.Get<Vector2>(); 
    }

    public void OnJump(InputValue value)
    {
        isHoldingJump = value.isPressed;
        if (isHoldingJump)
        {
            // 1. Drop down through platforms
            if (moveInput.y < -0.1f && isGrounded)
            {
                StartCoroutine(DropThroughPlatform());
            }
            // 2. WALL JUMP: If we are sliding, execute a wall jump escape
            else if (isWallSliding)
            {
                StartCoroutine(WallJumpRoutine());
            }
            // 3. Regular ground jump
            else if (coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                coyoteTimeCounter = 0f;
                if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();
            }
        }
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallSliding = false;
        canMove = false; // Turn off player joystick authority momentarily

        // Action spike applied to your Canvas Wake meter
        if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();

        // Determine jump trajectory away from the wall
        float jumpDirection = isFacingRight ? -1f : 1f;

        // Set the velocities: force away on X, jump high on Y
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpForce.x, wallJumpForce.y);

        // Turn around to cleanly face away from the wall we just kicked off of
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;

        yield return new WaitForSeconds(wallJumpDuration);

        canMove = true; // Give direction controls back to the player
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && canDash && !isDashing) 
        {
            StartCoroutine(DashRoutine());
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            if (WakeManager.Instance != null) WakeManager.Instance.AddActionSpike();
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        StartCoroutine(AttackGizmoVisual());

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        Collider2D[] hitProjectiles = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, projectileLayer);

        foreach (Collider2D proj in hitProjectiles)
        {
            Projectile p = proj.GetComponent<Projectile>();
            if (p != null)
            {
                Vector2 deflectDirection = (mouseWorldPosition - (Vector2)proj.transform.position).normalized;
                p.Deflect(deflectDirection);
                Debug.DrawLine(proj.transform.position, mouseWorldPosition, Color.yellow, 0.5f);
                if (WakeManager.Instance != null) WakeManager.Instance.RewardSuccessfulParry();
            }
        }
    }

    private IEnumerator AttackGizmoVisual()
    {
        isAttackingGizmo = true;
        IsCurrentlyAttacking = true;  
        yield return new WaitForSeconds(0.15f); 
        isAttackingGizmo = false;
        IsCurrentlyAttacking = false; 
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

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
            Collider2D platformCollider = hit.collider;
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropDownForce);
            
            yield return new WaitForSeconds(0.2f);
            
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }

    private void Flip()
    {
        // Check if the player is moving the opposite direction they are currently facing
        if (isFacingRight && moveInput.x < 0f || !isFacingRight && moveInput.x > 0f)
        {
            isFacingRight = !isFacingRight; 
            
            // Multiply the player's X scale by -1 to visually flip the sprite and all child objects
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void OnDrawGizmos()
    {
        // Attack Ring
        Gizmos.color = isAttackingGizmo ? Color.red : Color.white;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // Ground Radar
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }

        // Wall Radar
        if (wallCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wallCheck.position, 0.2f);
        }
    }
}