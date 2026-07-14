using UnityEngine;

public class VictoryMenu : MonoBehaviour
{
    public GameObject victoryUI;




    private void OnEnable()
    {
        Debug.Log("VictoryMenu subscribed");
        GameManager.OnVictory += ShowVictory;
    }

    private void OnDisable()
    {
        Debug.Log("VictoryMenu unsubscribed");
        GameManager.OnVictory -= ShowVictory;
    }

    public void ShowVictory()
    {
        Debug.Log("ShowVictory called");
        victoryUI.SetActive(true);
    }
}