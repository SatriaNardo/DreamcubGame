using System.Collections;
using UnityEngine;
using TMPro; 
using UnityEngine.InputSystem; // NEW: Required for the New Input System!

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    
    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;

    private string[] currentSentences;
    private int currentSentenceIndex;
    private bool isTyping;

    public bool IsDialogueActive { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!IsDialogueActive) return;

        // --- FIX: Using the New Input System to detect taps/clicks globally ---
        bool advanceInputPressed = false;

        // Check for Mouse Click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            advanceInputPressed = true;
        }
        // Check for Mobile Touch
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            advanceInputPressed = true;
        }

        if (advanceInputPressed)
        {
            DisplayNextSentence();
        }
    }

    public void StartDialogue(string[] sentences)
    {
        if (IsDialogueActive) return;

        currentSentences = sentences;
        currentSentenceIndex = 0;
        IsDialogueActive = true;
        dialoguePanel.SetActive(true);
        
        StartCoroutine(TypeSentence(currentSentences[currentSentenceIndex]));
    }

    public void DisplayNextSentence()
    {
        // If the text is still typing, tapping skips to the end of the sentence instantly
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentSentences[currentSentenceIndex];
            isTyping = false;
            return;
        }

        currentSentenceIndex++; 

        if (currentSentenceIndex < currentSentences.Length)
        {
            StartCoroutine(TypeSentence(currentSentences[currentSentenceIndex]));
        }
        else
        {
            EndDialogue();
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; 
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
    }
}