using UnityEngine;

public class Chalamala : MonoBehaviour
{
    public Canvas info;   // to show interaction key
    public Canvas dial; // to control the dialogue
    public Transform player;
    public float showDistance = 2f; // how close the player needs to be for the text to appear

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Load the player dynamically, since he enters this scene from an already existing scene
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        // at the beginning we don't see the dialogue
        info.enabled = false;
        dial.gameObject.SetActive(false);   // since it has its own script, it need to be completely blocked
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;
        //shows the message of the key to press when in range
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < showDistance)
        {
            info.enabled = true;
            if (Input.GetKeyDown(KeyCode.E)){
                dial.gameObject.SetActive(true);

                //stop player movement
                PlayerController playerController = player.GetComponent<PlayerController>();
                playerController.canMove = false;
                
                Dialogue dialogueScript = dial.GetComponentInChildren<Dialogue>(true);
                dialogueScript.StartDialogue();
            }
        }
        else
        {
            info.enabled = false;
        }
    }

    
}
