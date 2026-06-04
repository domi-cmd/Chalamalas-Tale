
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool goatDead = false;
    public bool hasCheese = false;
    public bool swissCheeseSeen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}