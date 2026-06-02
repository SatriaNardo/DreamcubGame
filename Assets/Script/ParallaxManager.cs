using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax Strength")]
    [Tooltip("0 = doesn't move, 1 = follows camera exactly")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxX = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float parallaxY = 0.5f;

    private Vector3 startBackgroundPosition;
    private Vector3 startCameraPosition;

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        startBackgroundPosition = transform.position;
        startCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = cameraTransform.position - startCameraPosition;

        float x = startBackgroundPosition.x + cameraDelta.x * parallaxX;
        float y = startBackgroundPosition.y + cameraDelta.y * parallaxY;

        transform.position = new Vector3(
            x,
            y,
            startBackgroundPosition.z
        );
    }
}