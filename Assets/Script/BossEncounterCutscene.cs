using System.Collections;
using UnityEngine;

public class BossEncounterCutscene : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private GameObject bossObject;
    [SerializeField] private Animator bossAnimator;
    
    [Header("Dialogue Content")]
    [TextArea(3, 5)]
    [SerializeField] private string[] cutsceneDialogue;
    
    [Header("Timing")]
    [Tooltip("How long does the boss_run animation take before they vanish?")]
    [SerializeField] private float runAwayDuration = 1.5f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Start the cutscene only once when the player walks in
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            PlayerController player = collision.GetComponent<PlayerController>();
            
            StartCoroutine(PlayCutscene(player));
        }
    }

    private IEnumerator PlayCutscene(PlayerController player)
    {
        // 1. Freeze the player in place
        if (player != null) player.SetCutsceneMode(true);

        // 2. Start the dialogue
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(cutsceneDialogue);
        }

        // 3. WAIT INVISIBLE LOOP: This pauses the code right here until the dialogue box is closed!
        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        // 4. Play the Boss animation
        if (bossAnimator != null)
        {
            // Make sure "boss_run" is the EXACT name of the animation state in your Animator
            bossAnimator.Play("Boss_Run"); 
        }

        // 5. Wait for the boss to finish their running animation
        yield return new WaitForSeconds(runAwayDuration);

        // 6. Make the boss disappear (You could also Destroy(bossObject) here)
        if (bossObject != null) bossObject.SetActive(false);

        // 7. Unfreeze the player so they can keep playing the game!
        if (player != null) player.SetCutsceneMode(false);
    }
}