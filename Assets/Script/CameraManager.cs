using UnityEngine;
using Unity.Cinemachine; // Standard for Unity 6 Cinemachine

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Virtual Cameras")]
    // FIXED WARNING: Changed from CinemachineVirtualCamera to CinemachineCamera
    [SerializeField] private CinemachineCamera[] allVirtualCameras;
    private CinemachineCamera currentCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Look through all cameras to find which one is active at the start
        foreach (CinemachineCamera vcam in allVirtualCameras)
        {
            if (vcam.enabled)
            {
                currentCamera = vcam;
            }
            
            // Automatically assign the camera to follow the player on level load
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                vcam.Follow = player.transform;
            }
        }
    }

    // FIXED WARNING: Changed parameter from CinemachineVirtualCamera to CinemachineCamera
    public void SwapCamera(CinemachineCamera newCam)
    {
        if (currentCamera != null)
        {
            currentCamera.enabled = false;
        }

        currentCamera = newCam;
        currentCamera.enabled = true;
    }
}