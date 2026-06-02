using System.Collections;
using UnityEngine;

public class BossEncounter : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Drag the Boss GameObject here so the script knows who to wake up!")]
    [SerializeField] private BossController bossController; 
    
    [Header("Dialogue Content")]
    [Tooltip("The conversation that happens right before the fight.")]
    [SerializeField] private DialogueManager.DialogueLine[] encounterDialogue;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only trigger once, and only when the Player walks into the invisible box
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            PlayerController player = collision.GetComponent<PlayerController>();
            
            StartCoroutine(EncounterSequence(player));
        }
    }

    private IEnumerator EncounterSequence(PlayerController player)
    {
        // 1. Freeze the player in place so they can't move during the chat
        if (player != null) player.SetCutsceneMode(true);

        // 2. Play the dialogue (if you added any lines)
        if (DialogueManager.Instance != null && encounterDialogue.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(encounterDialogue);
            
            // 3. Pause the script here until the player clicks through all the text
            yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);
        }

        // 4. Send the signal to the boss to play its "Boss_Spawn" animation!
        if (bossController != null)
        {
            bossController.StartBossFight();
        }

        // 5. Wait for the exact length of the Boss_Spawn animation (2 seconds).
        // This stops the player from getting cheap hits in while the boss is roaring!
        yield return new WaitForSeconds(2f);

        // 6. Unfreeze the player - LET THE FIGHT BEGIN!
        if (player != null) player.SetCutsceneMode(false);
        
        // 7. Turn off this invisible trigger box so it never accidentally fires again
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null) triggerCollider.enabled = false;
    }
}