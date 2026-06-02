using UnityEngine;

public class BossCube : MonoBehaviour
{
    [SerializeField] private float rollSpeed = 5f;
    [SerializeField] private float deflectedSpeed = 15f;
    [SerializeField] private int damageToPlayer = 10;
    [SerializeField] private int damageToBoss = 50; // Deals heavy damage when parried!

    private Transform targetPlayer;
    private Transform originBoss;
    private bool isDeflected = false;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Transform player, Transform boss)
    {
        targetPlayer = player;
        originBoss = boss;
    }

    void Update()
    {
        if (targetPlayer == null || originBoss == null) return;

        if (!isDeflected)
        {
            // Roll towards the player
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * rollSpeed, rb.linearVelocity.y);
            
            // Optional: Make the cube visually rotate as it rolls
            transform.Rotate(0, 0, -direction.x * 10f); 
        }
        else
        {
            // Fly directly back to the boss (auto-aim)
            Vector2 directionToBoss = (originBoss.position - transform.position).normalized;
            rb.linearVelocity = directionToBoss * deflectedSpeed;
        }
    }

    // Called by the PlayerController when parried
    public void DeflectToBoss()
    {
        isDeflected = true;
        gameObject.layer = LayerMask.NameToLayer("Default"); // Stop it from hitting the player again
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDeflected && collision.CompareTag("Player"))
        {
            // Hit the player!
            collision.GetComponent<PlayerController>().PlayHitAnimation();
            Destroy(gameObject);
        }
        else if (isDeflected && collision.gameObject.transform == originBoss)
        {
            // Hit the boss!
            BossController boss = collision.GetComponent<BossController>();
            if (boss != null) boss.TakeDamage(damageToBoss);
            Destroy(gameObject);
        }
    }
}