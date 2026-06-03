using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public DialoguesSpeeches database;
    

    public string currentScene;

    // NEW
    public bool cheese = false;

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

        // Decide which scene to use
        string sceneToUse = currentScene;

        // If cheese is true, use SwissCheese ONCE
        if (GameManager.Instance.goatDead && currentScene == "Cheese")
        {
            sceneToUse = "SwissCheese";
            GameManager.Instance.hasCheese = true;  // can now go and see the easter egg at the village
        }

        activeDialogue = database.dialogues.FindAll(d => d.scene == sceneToUse);

        if (activeDialogue == null || activeDialogue.Count == 0)
        {
            Debug.LogWarning("No dialogue found for scene: " + sceneToUse);
            EndDialogue();
            return;
        }

        StartCoroutine(TypeLine());
    }

    void EndDialogue()
    {
        index = 0;
        textMeshPro.text = "";

        transform.parent.gameObject.SetActive(false);

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.UnfreezePlayerMovement();
        }
    }

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