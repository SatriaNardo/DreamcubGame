using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionSceneController : MonoBehaviour
{
    [Header("Dialogue Content")]
    [SerializeField] private DialogueManager.DialogueLine[] transitionDialogue;

    [Header("Next Destination")]
    [Tooltip("Type the exact name of the gameplay level scene to load after this dialogue finishes.")]
    [SerializeField] private string nextGameplaySceneName;

    [Header("Delay")]
    [SerializeField] private float startDelay = 0.5f;

    void Start()
    {
        StartCoroutine(RunTransitionSequence());
    }

    private IEnumerator RunTransitionSequence()
    {
        // Wait a tiny bit for the scene asset loading process to settle down cleanly
        yield return new WaitForSeconds(startDelay);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(transitionDialogue);
        }
        else
        {
            Debug.LogError("No DialogueManager instance found in the TransitionScene!");
            yield break;
        }

        // Wait completely right here until the player clicks through all lines
        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        // Dialogue is finished! Load your next gameplay level scene
        if (!string.IsNullOrEmpty(nextGameplaySceneName))
        {
            SceneManager.LoadScene(nextGameplaySceneName);
        }
        else
        {
            Debug.LogWarning("Next Gameplay Scene Name is empty! Staying in transition scene.");
        }
    }
}