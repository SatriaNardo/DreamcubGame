using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Drag the empty GameObjects you want the platform to move between here.")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float waitTimeAtWaypoint = 1f;

    [Header("Behavior Settings")]
    [Tooltip("If checked, the lift will stop permanently when it reaches the last waypoint.")]
    [SerializeField] private bool isOneWay = false;
    
    [Tooltip("If checked, the lift will sleep until the player physically lands on it.")]
    [SerializeField] private bool startOnPlayerStep = false;

    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    
    // Internal locks for our new behavior
    private bool isActivated = true; 
    private bool hasFinished = false; 

    // Tracking variables for smooth, parent-free movement
    private Vector3 previousPosition;
    private Transform playerOnPlatform;

    void Start()
    {
        // Snap to the first waypoint
        if (waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }
        
        previousPosition = transform.position; 

        // If the lift requires the player to step on it, start it deactivated!
        if (startOnPlayerStep)
        {
            isActivated = false;
        }
    }

    void Update()
    {
        // Stop moving if: no waypoints, currently waiting, hasn't been stepped on, or reached the end of a 1-way trip
        if (waypoints.Length == 0 || isWaiting || !isActivated || hasFinished) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // Move the platform toward the target point
        transform.position = Vector2.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

        // Check if we have reached the target
        if (Vector2.Distance(transform.position, targetWaypoint.position) < 0.01f)
        {
            StartCoroutine(WaitBeforeMoving());
        }
    }

    void LateUpdate()
    {
        // 1. Calculate exactly how far the platform moved this frame
        Vector3 distanceMoved = transform.position - previousPosition;

        // 2. If the player is standing on us, manually push them by that exact same distance!
        if (playerOnPlatform != null)
        {
            playerOnPlatform.position += distanceMoved;
        }

        // 3. Save our current position so we can do the math again next frame
        previousPosition = transform.position;
    }

    private IEnumerator WaitBeforeMoving()
    {
        isWaiting = true;
        
        // Wait at the stop for a moment
        yield return new WaitForSeconds(waitTimeAtWaypoint);
        
        // Move to the next waypoint in the list
        currentWaypointIndex++;
        
        // Check if we reached the end of the line
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (isOneWay)
            {
                // Shut down the lift permanently
                hasFinished = true;
                isWaiting = false; 
                yield break; // Exit the coroutine early so it never loops
            }
            else
            {
                // Loop back to the beginning
                currentWaypointIndex = 0;
            }
        }

        isWaiting = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Remember the player when they land on us (No parenting!)
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = collision.transform;

            // WAKE UP! If we were waiting for the player to step on us, turn the engine on.
            if (startOnPlayerStep && !isActivated)
            {
                isActivated = true;
                Debug.Log("Player stepped on the lift. Engine started!");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Forget the player when they jump off
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = null;
        }
    }
}