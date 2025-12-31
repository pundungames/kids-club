using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SourceObjects
{
    public string ID;

    public GameObject SourcePrefab;
    //If 0 will use the global object count
    public int MinNumberOfObject = 0;
    public bool AllowGrow = true;
    public bool AutoDestroy = true;
    public List<GameObject> clones;
}