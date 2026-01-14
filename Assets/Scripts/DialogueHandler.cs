using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueHandler : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private float textSpeed = 0.03f;
    [SerializeField] private string[] Dialogues;

    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private string[] _activeDialogues;
    private int _index;
    private bool _isTyping;
    private Coroutine _typingRoutine;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ForceClose();
    }

    public void Interact()
    {
        Interact(Dialogues);
    }

    public void Interact(string[] dialogues)
    {

        if (panel == null || !panel.activeSelf)
        {
            StartDialogue(dialogues);
            return;
        }

        SkipOrNext();
    }

    private void StartDialogue(string[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) return;

        _activeDialogues = dialogues;
        _index = 0;

        if (panel != null) panel.SetActive(true);

        StartCurrentLine();
    }

    private void SkipOrNext()
    {
        if (_activeDialogues == null || _activeDialogues.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (_isTyping)
        {
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            _typingRoutine = null;
            _isTyping = false;

            if (dialogueText != null)
                dialogueText.text = _activeDialogues[_index];

            return;
        }

        _index++;

        if (_index >= _activeDialogues.Length)
        {
            EndDialogue();
            return;
        }

        StartCurrentLine();
    }

    private void StartCurrentLine()
    {
        if (_activeDialogues == null || _activeDialogues.Length == 0)
        {
            EndDialogue();
            return;
        }

        if (_index < 0 || _index >= _activeDialogues.Length)
        {
            EndDialogue();
            return;
        }

        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = StartCoroutine(TypeLine(_activeDialogues[_index]));
    }

    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        float delay = Mathf.Max(0.0001f, textSpeed);

        for (int i = 0; i < line.Length; i++)
        {
            if (dialogueText != null)
                dialogueText.text += line[i];

            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
        _typingRoutine = null;
    }

    private void EndDialogue()
    {
        _activeDialogues = null;
        _index = 0;
        _isTyping = false;

        if (_typingRoutine != null) StopCoroutine(_typingRoutine);
        _typingRoutine = null;

        if (panel != null) panel.SetActive(false);
    }

    public void ForceClose() => EndDialogue();

    public bool IsOpen => panel != null && panel.activeSelf;
    public bool IsTyping => _isTyping;
}
