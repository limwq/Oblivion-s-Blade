using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float typingSpeed = 0.02f;

    private Queue<string> sentences;
    private bool isDialogueActive = false;

    // --- NEW: Tracking the current trigger and line number ---
    private DialogueTrigger activeTrigger;
    private int currentSentenceIndex = 0;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        sentences = new Queue<string>();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    void Update() {
        if (!isDialogueActive) return;

        // SKIP logic
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) {
            DisplayNextSentence();
        }
    }

    // --- NEW: Added 'DialogueTrigger trigger' parameter to link them together ---
    public void StartDialogue(DialogueTrigger trigger, string npcName, string[] dialogueLines) {

        activeTrigger = trigger;
        currentSentenceIndex = 0; // Reset index for new conversations

        isDialogueActive = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (nameText != null) nameText.text = npcName;

        sentences.Clear();

        foreach (string line in dialogueLines) {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence() {
        if (sentences.Count == 0) {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();

        // --- NEW: Tell the trigger to play the SFX/Animation for this specific line! ---
        if (activeTrigger != null) {
            activeTrigger.PlayLineEvent(currentSentenceIndex);
        }
        currentSentenceIndex++; // Move to the next index

        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence) {
        if (dialogueText != null) dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray()) {
            if (dialogueText != null) dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialogue() {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        activeTrigger = null; // Clear the reference
    }
}