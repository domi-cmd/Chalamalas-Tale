using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public DialoguesSpeeches database;
    

    public string currentScene;
    private string sceneToUse;
    private string sceneUsed;

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
        sceneUsed = currentScene;

        // Special dialogue only once
        if (GameManager.Instance.goatDead &&
            currentScene == "Cheese" &&
            !GameManager.Instance.swissCheeseSeen)
        {
            sceneUsed = "Swiss_cheese";
            Debug.Log("cheese dialogue");
        }

        activeDialogue = database.dialogues.FindAll(d => d.scene == sceneUsed);

        if (activeDialogue == null || activeDialogue.Count == 0)
        {
            Debug.LogWarning("No dialogue found for scene: " + sceneUsed);
            EndDialogue();
            return;
        }

        StartCoroutine(TypeLine());
    }

    void EndDialogue()
    {
        // If we just finished SwissCheese,
        // mark it as seen and unlock cheese
        if (sceneUsed == "Swiss_cheese" &&
            GameManager.Instance.goatDead &&
            !GameManager.Instance.swissCheeseSeen)
        {
            GameManager.Instance.swissCheeseSeen = true;
            GameManager.Instance.hasCheese = true;
            currentScene = "Cheese";
        }
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