using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform target;        
    [SerializeField] private float smoothSpeed = 5f; 

    [Header("Position Tuning")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f); 

    // --- NEW: Boundary Lock Variables ---
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private bool hasBounds = false;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // --- NEW: Rooms will call this function to hand the camera their exact size ---
    public void SetBounds(Bounds roomBounds)
    {
        // 1. Calculate exactly how tall and wide your game screen is
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // 2. Shrink the boundary box by the camera's size so the EDGE hits the wall, not the CENTER
        minBounds = new Vector3(roomBounds.min.x + camWidth, roomBounds.min.y + camHeight, -100f);
        maxBounds = new Vector3(roomBounds.max.x - camWidth, roomBounds.max.y - camHeight, 100f);

        // 3. Failsafe: If you make a room smaller than your screen, just center the camera
        if (minBounds.x > maxBounds.x) minBounds.x = maxBounds.x = roomBounds.center.x;
        if (minBounds.y > maxBounds.y) minBounds.y = maxBounds.y = roomBounds.center.y;

        hasBounds = true;
    }

    void LateUpdate() // Use LateUpdate for cameras to prevent jitter!
    {
        if (target == null) return;

        // 1. Calculate where we WANT to go
        Vector3 desiredPosition = target.position + offset;

        // 2. FORCE the camera to stay inside the room boundaries
        if (hasBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        // 3. Smoothly slide to that restricted position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}