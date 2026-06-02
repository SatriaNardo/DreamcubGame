using UnityEngine;

public class ElectricHazard : MonoBehaviour
{
    [Header("Platform Path")]
    [Tooltip("Drag the platform with the BoxCollider2D here.")]
    [SerializeField] private BoxCollider2D targetPlatform;
    
    [Header("Status")]
    [Tooltip("If checked, the hazard is moving and dealing damage.")]
    public bool isActive = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("True = Moves clockwise. False = Moves counter-clockwise.")]
    [SerializeField] private bool moveClockwise = true;

    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 1f;
    private float lastHitTime;

    private Vector2[] localCorners = new Vector2[4];
    private int currentTargetIndex = 0;

    void Start()
    {
        if (targetPlatform == null)
        {
            Debug.LogWarning($"No platform assigned to the Electric Hazard on {gameObject.name}!");
            return;
        }

        Vector2 size = targetPlatform.size;
        Vector2 offset = targetPlatform.offset;

        float halfX = size.x / 2f;
        float halfY = size.y / 2f;

        localCorners[0] = offset + new Vector2(-halfX, halfY);
        localCorners[1] = offset + new Vector2(halfX, halfY);
        localCorners[2] = offset + new Vector2(halfX, -halfY);
        localCorners[3] = offset + new Vector2(-halfX, -halfY);

        transform.position = targetPlatform.transform.TransformPoint(localCorners[0]);
        currentTargetIndex = moveClockwise ? 1 : 3;
    }

    void Update()
    {
        if (!isActive || targetPlatform == null) return;

        Vector3 targetPosition = targetPlatform.transform.TransformPoint(localCorners[currentTargetIndex]);
        
        RotateTowardsTarget(targetPosition);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
        {
            if (moveClockwise)
            {
                currentTargetIndex++;
                if (currentTargetIndex > 3) currentTargetIndex = 0;
            }
            else
            {
                currentTargetIndex--;
                if (currentTargetIndex < 0) currentTargetIndex = 3;
            }
        }
    }

    private void RotateTowardsTarget(Vector3 target)
    {
        Vector2 direction = (target - transform.position).normalized;
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Adjust the offset (0f) if your sprite is rotated incorrectly
            transform.rotation = Quaternion.Euler(0, 0, angle + 0f);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isActive) return;

        // ADD THIS LINE:
        Debug.Log("Hazard detected collision with: " + collision.name);

        if (collision.CompareTag("Player") && Time.time >= lastHitTime + damageCooldown)
        {
            lastHitTime = Time.time;

            if (WakeManager.Instance != null) 
            {
                WakeManager.Instance.TakeDamagePenalty();
            }

            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null) player.PlayHitAnimation();
        }
    }
}