using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTuto : MonoBehaviour
{
public string SceneToLoad;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            SceneManager.LoadScene(SceneToLoad);
        }
        
    }
}