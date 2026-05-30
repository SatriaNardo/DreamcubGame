using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform target;        
    [SerializeField] private float smoothSpeed = 5f; // New Scale: Use values between 2 and 10 now!

    [Header("Position Tuning")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f); 

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void Update()
    {
        if (target == null) return;

        // 1. Calculate target destination position
        Vector3 desiredPosition = target.position + offset;

        // 2. Smoothly blend using Time.deltaTime for frame-rate independence
        // Using a higher smoothSpeed multiplied by deltaTime creates a flawless, organic lag
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. Apply position instantly per-frame
        transform.position = smoothedPosition;
    }
}