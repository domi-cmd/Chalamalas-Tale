using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public DialoguesSpeeches database;

    public string currentScene;

    public float textSpeed;
    private int index;
    private List<DialogueLine> activeDialogue;

    void Start()
    {
        textMeshPro.text = "";
        StartDialogue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (textMeshPro.text == FormatLine(activeDialogue[index]))
                NextLine();
            else
            {
                StopAllCoroutines();
                textMeshPro.text = FormatLine(activeDialogue[index]);
            }
        }
    }

    public void StartDialogue()
    {
        StopAllCoroutines();
        index = 0;
        textMeshPro.text = "";

        activeDialogue = database.dialogues.FindAll(d => d.scene == currentScene);

        if (activeDialogue == null || activeDialogue.Count == 0)
        {
            Debug.LogWarning("No dialogue found for scene: " + currentScene);
            EndDialogue();
            return;
        }

        StartCoroutine(TypeLine());
    }

    void EndDialogue(){
    index = 0;
    textMeshPro.text = "";

    transform.parent.gameObject.SetActive(false);

    GameObject player = GameObject.FindWithTag("Player");
    if (player != null)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.UnfreezePlayerMovement();
    }}

    IEnumerator TypeLine()
    {
        string line = FormatLine(activeDialogue[index]);

        foreach (char c in line.ToCharArray())
        {
            textMeshPro.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    string FormatLine(DialogueLine line)
    {
        return $"[{line.speaker}]: {line.text}";
    }

    void NextLine()
    {
        index++;

        if (index < activeDialogue.Count)
        {
            textMeshPro.text = "";
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }
}