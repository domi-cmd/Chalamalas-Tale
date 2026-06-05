using UnityEngine;

public class VictoryMenu : MonoBehaviour
{
    public GameObject victoryUI;

    private void OnEnable()
    {
        GameManager.OnVictory += ShowVictory;
    }

    private void OnDisable()
    {
        GameManager.OnVictory -= ShowVictory;
    }

    public void ShowVictory()
    {
        victoryUI.SetActive(true);
        Time.timeScale = 0f;
    }
}