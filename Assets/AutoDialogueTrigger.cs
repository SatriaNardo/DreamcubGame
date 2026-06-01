using System.Collections;
using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    [TextArea(3, 5)]
    [SerializeField] private string[] dialogueLines;

    [Header("Settings")]
    [Tooltip("Should the player be frozen in place while reading?")]
    [SerializeField] private bool freezePlayer = true;
    
    [Tooltip("If checked, the dialogue will play EVERY time the player walks here. If unchecked, it only plays once.")]
    [SerializeField] private bool playEveryTime = false;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player walked into the zone
        if (collision.CompareTag("Player"))
        {
            // Only play if it hasn't triggered yet, OR if we set it to play every time
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
        // 1. Freeze the player (if the box is checked)
        if (freezePlayer && player != null)
        {
            player.SetCutsceneMode(true);
        }

        // 2. Open the UI and start typing the dialogue
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueLines);
        }

        // 3. Pause this script until the player taps through all the text
        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        // 4. Unfreeze the player so they can move again
        if (freezePlayer && player != null)
        {
            player.SetCutsceneMode(false);
        }
    }
}