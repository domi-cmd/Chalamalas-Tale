using UnityEngine;

public class Chalamala : MonoBehaviour
{
    public Canvas info;   // to show interaction key
    public Canvas dial; // to control the dialogue
    public Transform player;
    public float showDistance = 5f; // how close the player needs to be for the text to appear

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // at the beginning we don't see the dialogue
        info.enabled = false;
        dial.gameObject.SetActive(false);   // since it has its own script, it need to be completely blocked
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > showDistance)
        {
            info.enabled = true;
            if (Input.GetKeyDown(KeyCode.E)){
                dial.gameObject.SetActive(true);
                Dialogue dialogueScript = dial.GetComponent<Dialogue>();
                dialogueScript.StartDialogue();
            }
        }
        else
        {
            info.enabled = false;
        }
    }

    
}
