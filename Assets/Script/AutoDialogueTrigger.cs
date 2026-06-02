using System.Collections;
using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    // --- CHANGE THIS LINE TO MATCH BELOW ---
    [SerializeField] private DialogueManager.DialogueLine[] dialogueLines;

    [Header("Settings")]
    [SerializeField] private bool freezePlayer = true;
    [SerializeField] private bool playEveryTime = false;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!hasTriggered || playEveryTime)
            {
                hasTriggered = true;
                PlayerController player = collision.GetComponent<PlayerController>();
                
                StartCoroutine(RunForcedDialogue(player));
            }
        }
    }

    private IEnumerator RunForcedDialogue(PlayerController player)
    {
        if (freezePlayer && player != null)
        {
            player.SetCutsceneMode(true);
        }

        if (DialogueManager.Instance != null)
        {
            // This now passes the struct array correctly!
            DialogueManager.Instance.StartDialogue(dialogueLines);
        }

        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        if (freezePlayer && player != null)
        {
            player.SetCutsceneMode(false);
        }
    }
}