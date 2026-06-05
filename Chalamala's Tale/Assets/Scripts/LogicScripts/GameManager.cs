
using UnityEngine;
using System;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool goatDead = false;
    public bool hasCheese = false;
    public static Action OnVictory;

    public static void Victory()
    {
        OnVictory?.Invoke();
    }

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