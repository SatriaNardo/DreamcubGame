using System.Collections;
using UnityEngine;

public class BossCube : MonoBehaviour
{
    [SerializeField] private float rollSpeed = 5f;
    [SerializeField] private float deflectedSpeed = 15f;
    [SerializeField] private int damageToBoss = 50;

    private Transform targetPlayer;
    private Transform originBoss;
    private bool isDeflected = false;
    private bool isReady = false; 
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero; 
        StartCoroutine(WaitToActivate());
    }

    private IEnumerator WaitToActivate()
    {
        yield return new WaitForSeconds(2f);
        isReady = true;
    }

    public void Initialize(Transform player, Transform boss)
    {
        targetPlayer = player;
        originBoss = boss;
    }

    void Update()
    {
        if (!isReady || targetPlayer == null || originBoss == null) return;

        if (!isDeflected)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * rollSpeed, rb.linearVelocity.y);
            transform.Rotate(0, 0, -direction.x * 10f); 
        }
        else
        {
            Vector2 directionToBoss = (originBoss.position - transform.position).normalized;
            rb.linearVelocity = directionToBoss * deflectedSpeed;
        }
    }

    public void DeflectToBoss()
    {
        isDeflected = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDeflected && collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerController>().PlayHitAnimation();
            if (WakeManager.Instance != null) WakeManager.Instance.TakeDamagePenalty();
            Destroy(gameObject);
        }
        else if (isDeflected && collision.gameObject == originBoss.gameObject)
        {
            BossController boss = collision.GetComponent<BossController>();
            if (boss != null) boss.TakeDamage(damageToBoss);
            Destroy(gameObject);
        }
    }
}