using UnityEngine;
using TMPro;
using System.Collections;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;

    public string[] dialogueText;
    public float textSpeed;
    private int index;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textMeshPro.text = "";
        StartDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (textMeshPro.text == dialogueText[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textMeshPro.text = dialogueText[index];
            }
        }
        // print(index);
    }

    public void StartDialogue()
    {
        StopAllCoroutines(); 
        index = 0;
        textMeshPro.text = "";
        StartCoroutine(Typeline());
    }
    IEnumerator Typeline()
    {
        foreach(char c in dialogueText[index].ToCharArray())
        {
            textMeshPro.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

    }

    void NextLine()
    {
        index++;
        if(index < dialogueText.Length)
        {
            textMeshPro.text = "";
            StartCoroutine(Typeline());
        }
        else
        {
            index = 0;
            gameObject.SetActive(false);
        }
    }

}

