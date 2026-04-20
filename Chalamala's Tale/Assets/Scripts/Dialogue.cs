using UnityEngine;
using TMPro;
using System.Collections;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;

    public string[] firstDialogueText;
    public string[] secondDialogueText;

    private string[] activeDialogue; 

    public float textSpeed;
    private int index;

    void Start()
    {
        textMeshPro.text = "";
        StartDialogue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (textMeshPro.text == activeDialogue[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textMeshPro.text = activeDialogue[index];
            }
        }
    }

    public void StartDialogue()
    {
        StopAllCoroutines();
        index = 0;
        textMeshPro.text = "";

        
        int deaths = PlayerHealth.Instance != null ? PlayerHealth.Instance.deathCounter : 0;

        
        if (deaths == 0)
            activeDialogue = firstDialogueText;
        else
            activeDialogue = secondDialogueText;

        StartCoroutine(Typeline());
    }

    IEnumerator Typeline()
    {
        foreach (char c in activeDialogue[index].ToCharArray())
        {
            textMeshPro.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        index++;

        if (index < activeDialogue.Length)
        {
            textMeshPro.text = "";
            StartCoroutine(Typeline());
        }
        else
        {
            // end dialogue
            index = 0;
            gameObject.SetActive(false);

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    pc.canMove = true;
            }
        }
    }
}