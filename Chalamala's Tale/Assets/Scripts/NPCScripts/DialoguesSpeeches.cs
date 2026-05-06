/*
defines the shape of the dialogue database (speaker, text and scene)
*/
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Database")]
public class DialoguesSpeeches : ScriptableObject
{
    public List<DialogueLine> dialogues;
    
}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    public string scene;
}

