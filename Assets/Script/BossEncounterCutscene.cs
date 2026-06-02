using System.Collections;
using UnityEngine;
using UnityEngine.UI; // --- NEW: Required to control UI Images ---
using KinoGlitch; 

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

    [Header("Glitch Effect Camera")]
    [Tooltip("Drag the Main Camera here. If left blank, it will automatically find Camera.main at runtime.")]
    [SerializeField] private Camera targetCamera;

    [Header("Glitch Effect Timing")]
    [Tooltip("How long does it take for the glitch to slowly reach MAXIMUM intensity?")]
    [SerializeField] private float glitchBuildUpDuration = 1.5f;
    [Tooltip("Once at maximum, how long does the glitch stay on screen before disappearing?")]
    [SerializeField] private float glitchHoldDuration = 0.2f;
    
    [Header("Analog Glitch Maximum Spikes")]
    [SerializeField] private float analogJitterSpike = 0.8f;
    [SerializeField] private float analogColorDriftSpike = 0.5f;

    [Header("Digital Glitch Maximum Spikes")]
    [SerializeField] private float digitalIntensitySpike = 0.4f;

    [Header("Cinematic Ending")]
    [Tooltip("Drag a full-screen black UI Image here.")]
    [SerializeField] private Image blackScreen; // --- NEW: The black screen element ---

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

        // --- CHANGED: Use 'yield return' so the script pauses here until the glitch is fully done! ---
        yield return StartCoroutine(GlitchCameraRoutine());

        // NOTE: The player is still in Cutscene Mode here because the screen is black!
        // If you are loading a new scene after this, do it here. 
        // If you want them to run around in the dark, uncomment the line below:
        // if (player != null) player.SetCutsceneMode(false);
    }

    private IEnumerator GlitchCameraRoutine()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        
        if (targetCamera != null)
        {
            AnalogGlitchController analogGlitch = targetCamera.GetComponent<AnalogGlitchController>();
            DigitalGlitchController digitalGlitch = targetCamera.GetComponent<DigitalGlitchController>();

            // Memorize original values
            float origJitter = analogGlitch != null ? analogGlitch.ScanLineJitter : 0f;
            float origColor = analogGlitch != null ? analogGlitch.ColorDrift : 0f;
            float origIntensity = digitalGlitch != null ? digitalGlitch.Intensity : 0f;

            // --- THE LERP PHASE (Building up the glitch smoothly) ---
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

            // --- THE HOLD PHASE (Keep it at maximum glitch for a brief moment) ---
            if (glitchHoldDuration > 0f)
            {
                yield return new WaitForSeconds(glitchHoldDuration);
            }

            // --- NEW: SNAP TO BLACK SCREEN! ---
            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(true);
                blackScreen.color = new Color(0f, 0f, 0f, 1f); // Ensure it is pure, opaque black
            }
                
            // --- THE RESET PHASE (Fix the camera behind the black screen) ---
            if (analogGlitch != null)
            {
                analogGlitch.ScanLineJitter = origJitter;
                analogGlitch.ColorDrift = origColor;
            }

            if (digitalGlitch != null)
            {
                digitalGlitch.Intensity = origIntensity;
            }
        }
    }
}