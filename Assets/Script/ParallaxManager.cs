using UnityEngine;

// This makes our custom list show up beautifully in the Unity Inspector!
[System.Serializable]
public class ParallaxElement
{
    public Transform layerTransform;
    
    [Tooltip("0 = Foreground (Moves fast). 1 = Deep Background (Barely moves). (X-axis)")]
    public float parallaxEffectX = 1f;

    [Tooltip("0 = Foreground (Moves fast). 1 = Deep Background (Barely moves). (Y-axis)")]
    public float parallaxEffectY = 1f;
    
    [Tooltip("Check this if you want this layer to loop infinitely horizontally!")]
    public bool isInfiniteX = true;

    [Tooltip("Check this if you want this layer to loop infinitely vertically!")]
    public bool isInfiniteY = true;

    // We hide these from the Inspector so it stays clean, but the code still uses them
    [HideInInspector] public float startPosX;
    [HideInInspector] public float startPosY;
    [HideInInspector] public float lengthX;
    [HideInInspector] public float heightY;
}

public class ParallaxManager : MonoBehaviour
{
    [Header("Background Layers")]
    public ParallaxElement[] layers;

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;

        // Loop through every layer in our list and set up its starting math
        foreach (ParallaxElement layer in layers)
        {
            if (layer.layerTransform != null)
            {
                // Anchor the starting position based on the camera's current spot (X and Y)
                layer.startPosX = layer.layerTransform.position.x - (cam.position.x * layer.parallaxEffectX);
                layer.startPosY = layer.layerTransform.position.y - (cam.position.y * layer.parallaxEffectY);
                
                SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
                
                // Measure the sprite width for horizontal infinite scrolling
                if (layer.isInfiniteX)
                {
                    if (sr != null)
                    {
                        layer.lengthX = sr.bounds.size.x;
                    }
                    else
                    {
                        Debug.LogWarning($"No SpriteRenderer found on {layer.layerTransform.name}! Horizontal infinite scrolling won't work.");
                    }
                }

                // Measure the sprite height for vertical infinite scrolling
                if (layer.isInfiniteY)
                {
                    if (sr != null)
                    {
                        layer.heightY = sr.bounds.size.y;
                    }
                    else
                    {
                        Debug.LogWarning($"No SpriteRenderer found on {layer.layerTransform.name}! Vertical infinite scrolling won't work.");
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Move every layer in the list every frame
        foreach (ParallaxElement layer in layers)
        {
            if (layer.layerTransform == null) continue;

            // --- HORIZONTAL PARALLAX ---
            float distanceX = (cam.position.x * layer.parallaxEffectX);
            // Apply horizontal movement
            layer.layerTransform.position = new Vector3(layer.startPosX + distanceX, layer.layerTransform.position.y, layer.layerTransform.position.z);

            // --- HORIZONTAL INFINITE SCROLLING ---
            if (layer.isInfiniteX && layer.lengthX > 0)
            {
                float tempX = (cam.position.x * (1 - layer.parallaxEffectX));
                if (tempX > layer.startPosX + layer.lengthX)
                {
                    layer.startPosX += layer.lengthX;
                }
                else if (tempX < layer.startPosX - layer.lengthX)
                {
                    layer.startPosX -= layer.lengthX;
                }
            }

            // --- VERTICAL PARALLAX ---
            float distanceY = (cam.position.y * layer.parallaxEffectY);
            // Apply vertical movement
            layer.layerTransform.position = new Vector3(layer.layerTransform.position.x, layer.startPosY + distanceY, layer.layerTransform.position.z);

            // --- VERTICAL INFINITE SCROLLING ---
            if (layer.isInfiniteY && layer.heightY > 0)
            {
                float tempY = (cam.position.y * (1 - layer.parallaxEffectY));
                if (tempY > layer.startPosY + layer.heightY)
                {
                    layer.startPosY += layer.heightY;
                }
                else if (tempY < layer.startPosY - layer.heightY)
                {
                    layer.startPosY -= layer.heightY;
                }
            }
        }
    }
}