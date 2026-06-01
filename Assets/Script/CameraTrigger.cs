using UnityEngine;
using Unity.Cinemachine; // Standard for Unity 6 Cinemachine

public class CameraTrigger : MonoBehaviour
{
    [Header("Room Setup")]
    // FIXED WARNING: Changed from CinemachineVirtualCamera to CinemachineCamera
    [SerializeField] private CinemachineCamera roomCamera;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (CameraManager.Instance != null && roomCamera != null)
            {
                // Tell the manager to swap views cleanly
                CameraManager.Instance.SwapCamera(roomCamera);
            }
        }
    }
}