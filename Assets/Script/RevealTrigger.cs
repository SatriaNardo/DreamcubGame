using UnityEngine;

public class RevealTrigger : MonoBehaviour
{
    [Header("Reveal Settings")]
    [Tooltip("Drag the object you want to unhide into this slot.")]
    [SerializeField] private GameObject objectToUnhide;
    
    [Tooltip("If checked, it will only trigger once. If unchecked, it will trigger every time the player walks through.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        // Safety feature: Automatically hide the object when the game starts!
        if (objectToUnhide != null)
        {
            objectToUnhide.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check if the player stepped into the invisible trigger zone
        if (collision.CompareTag("Player"))
        {
            // 2. Stop if it's already triggered and we only want it to happen once
            if (triggerOnlyOnce && hasTriggered) return;

            // 3. Unhide the object!
            if (objectToUnhide != null)
            {
                objectToUnhide.SetActive(true);
                hasTriggered = true;
                Debug.Log(objectToUnhide.name + " has been revealed!");
            }
        }
    }
}