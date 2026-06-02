using System.Collections;
using UnityEngine;
using UnityEngine.UI; // --- REQUIRED: For the speaker portrait UI image ---
using TMPro; 
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // --- THE FIX: This structure must live here inside DialogueManager ---
    [System.Serializable]
    public struct DialogueLine
    {
        [TextArea(2, 4)]
        public string sentence;
        [Tooltip("Optional portrait sprite. Leave blank for narrator text!")]
        public Sprite speakerPortrait; 
    }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    
    // --- NEW: Drag your UI Image for the character portrait here ---
    [SerializeField] private Image portraitDisplay;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;

    private DialogueLine[] currentLines; // Swapped string[] for DialogueLine[]
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
        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsDialogueActive) return;

        bool advanceInputPressed = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            advanceInputPressed = true;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            advanceInputPressed = true;
        }

        if (advanceInputPressed)
        {
            DisplayNextSentence();
        }
    }

    // --- CHANGED: Now accepts the new DialogueLine[] structure ---
    public void StartDialogue(DialogueLine[] lines)
    {
        if (IsDialogueActive) return;

        currentLines = lines;
        currentSentenceIndex = 0;
        IsDialogueActive = true;
        dialoguePanel.SetActive(true);
        
        SetupDialogueView(currentLines[currentSentenceIndex]);
    }

    public void DisplayNextSentence()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentLines[currentSentenceIndex].sentence;
            isTyping = false;
            return;
        }

        currentSentenceIndex++; 

        if (currentSentenceIndex < currentLines.Length)
        {
            SetupDialogueView(currentLines[currentSentenceIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    // --- NEW: Handles updating the sprite portrait layout cleanly ---
    private void SetupDialogueView(DialogueLine currentLine)
    {
        if (portraitDisplay != null)
        {
            if (currentLine.speakerPortrait != null)
            {
                portraitDisplay.sprite = currentLine.speakerPortrait;
                portraitDisplay.gameObject.SetActive(true);
            }
            else
            {
                // If no sprite is selected, hide the avatar window frame
                portraitDisplay.gameObject.SetActive(false);
            }
        }

        StartCoroutine(TypeSentence(currentLine.sentence));
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
        if (portraitDisplay != null) portraitDisplay.gameObject.SetActive(false);
    }
}