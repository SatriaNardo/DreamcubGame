using UnityEngine;
using UnityEngine.InputSystem; // Since you use the new Input System

public class InteractableNPC : MonoBehaviour
{
    [Header("Dialogue Content")]
    [TextArea(3, 10)] // Makes the text box bigger in the Unity Inspector!
    [SerializeField] private string[] dialogueLines;
    
    [Header("Visuals")]
    [SerializeField] private GameObject interactPrompt; // E.g., an arrow or "!" above their head

    private bool isPlayerNearby = false;

    void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update()
    {
        // If the player is standing here, and the dialogue hasn't started yet...
        if (isPlayerNearby && !DialogueManager.Instance.IsDialogueActive)
        {
            // If they press 'E' on the keyboard (or you can map this to a UI button/New Input Action)
            if (Keyboard.current.eKey.wasPressedThisFrame) 
            {
                DialogueManager.Instance.StartDialogue(dialogueLines);
                if (interactPrompt != null) interactPrompt.SetActive(false); // Hide the "!" bubble
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactPrompt != null) interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}