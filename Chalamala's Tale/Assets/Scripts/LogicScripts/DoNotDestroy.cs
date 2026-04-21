using UnityEditor.Search;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoNotDestroy : MonoBehaviour
{
    private static GameObject[] persistentObjects = new GameObject[10];
    public int ObjectIndex;
    void Awake()
    {
        if (persistentObjects[ObjectIndex] == null)
        {
            persistentObjects[ObjectIndex] = gameObject;
            DontDestroyOnLoad(gameObject);
        } else if(persistentObjects[ObjectIndex]!= gameObject)
        {
            Destroy(gameObject);
        }
    } 

}
