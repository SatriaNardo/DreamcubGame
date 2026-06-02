using System.Collections;
using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    [Header("NPC Dialogue Content")]
    // --- CHANGE THIS LINE TO MATCH BELOW ---
    [SerializeField] private DialogueManager.DialogueLine[] npcDialogue;

    // ... Keep any other variables you have here (like detection ranges or animator references) ...

    // This is likely your interaction method where the error is triggering
    public void Interact(PlayerController player)
    {
        if (DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive)
        {
            // --- FIXED: Now passing the correct struct array type ---
            DialogueManager.Instance.StartDialogue(npcDialogue);
            StartCoroutine(HandleInteractionFreeze(player));
        }
    }

    private IEnumerator HandleInteractionFreeze(PlayerController player)
    {
        if (player != null) player.SetCutsceneMode(true);

        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        if (player != null) player.SetCutsceneMode(false);
    }
}