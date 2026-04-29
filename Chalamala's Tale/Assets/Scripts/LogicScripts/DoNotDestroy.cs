using UnityEngine;
using System.Collections.Generic;

public class DoNotDestroy : MonoBehaviour
{
    private static HashSet<string> existing = new HashSet<string>();

    void Awake()
    {
        string key = GetKey();

        if (!existing.Contains(key))
        {
            existing.Add(key);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    string GetKey()
    {
        // clé basée sur le type du GameObject
        return gameObject.name;
    }
}