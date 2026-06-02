using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KinoGlitch; 
using UnityEngine.SceneManagement; // --- NEW: Required for switching scenes ---

public class BossEncounterCutscene : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private GameObject bossObject;
    [SerializeField] private Animator bossAnimator;
    
    [Header("Dialogue Content")]
    // --- CHANGED: Updated from string[] to DialogueLine[] to support your optional images ---
    [SerializeField] private DialogueManager.DialogueLine[] cutsceneDialogue;
    
    [Header("Timing")]
    [Tooltip("How long does the boss_run animation take before they vanish?")]
    [SerializeField] private float runAwayDuration = 1.5f;

    [Header("Glitch Effect Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Glitch Effect Timing")]
    [SerializeField] private float glitchBuildUpDuration = 1.5f;
    [SerializeField] private float glitchHoldDuration = 0.2f;
    
    [Header("Analog Glitch Maximum Spikes")]
    [SerializeField] private float analogJitterSpike = 0.8f;
    [SerializeField] private float analogColorDriftSpike = 0.5f;

    [Header("Digital Glitch Maximum Spikes")]
    [SerializeField] private float digitalIntensitySpike = 0.4f;

    [Header("Cinematic Ending")]
    [SerializeField] private Image blackScreen; 

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            PlayerController player = collision.GetComponent<PlayerController>();
            
            StartCoroutine(PlayCutscene(player));
        }
    }

    private IEnumerator PlayCutscene(PlayerController player)
    {
        if (player != null) player.SetCutsceneMode(true);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(cutsceneDialogue);
        }

        yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive);

        if (bossAnimator != null)
        {
            bossAnimator.Play("Boss_Run"); 
        }

        yield return new WaitForSeconds(runAwayDuration);

        if (bossObject != null) bossObject.SetActive(false);

        yield return StartCoroutine(GlitchCameraRoutine());

        // --- NEW: Load the transition scene now that the screen is pitch black! ---
        SceneManager.LoadScene("TransitionScene");
    }

    private IEnumerator GlitchCameraRoutine()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        
        if (targetCamera != null)
        {
            AnalogGlitchController analogGlitch = targetCamera.GetComponent<AnalogGlitchController>();
            DigitalGlitchController digitalGlitch = targetCamera.GetComponent<DigitalGlitchController>();

            float origJitter = analogGlitch != null ? analogGlitch.ScanLineJitter : 0f;
            float origColor = analogGlitch != null ? analogGlitch.ColorDrift : 0f;
            float origIntensity = digitalGlitch != null ? digitalGlitch.Intensity : 0f;

            float elapsed = 0f;
            while (elapsed < glitchBuildUpDuration)
            {
                elapsed += Time.deltaTime;
                float percentComplete = elapsed / glitchBuildUpDuration;

                if (analogGlitch != null)
                {
                    analogGlitch.ScanLineJitter = Mathf.Lerp(origJitter, analogJitterSpike, percentComplete);
                    analogGlitch.ColorDrift = Mathf.Lerp(origColor, analogColorDriftSpike, percentComplete);
                }

                if (digitalGlitch != null)
                {
                    digitalGlitch.Intensity = Mathf.Lerp(origIntensity, digitalIntensitySpike, percentComplete);
                }

                yield return null;
            }

            if (glitchHoldDuration > 0f)
            {
                yield return new WaitForSeconds(glitchHoldDuration);
            }

            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(true);
                blackScreen.color = new Color(0f, 0f, 0f, 1f); 
            }
                
            if (analogGlitch != null) analogGlitch.ScanLineJitter = origJitter;
            if (analogGlitch != null) analogGlitch.ColorDrift = origColor;
            if (digitalGlitch != null) digitalGlitch.Intensity = origIntensity;
        }
    }
}