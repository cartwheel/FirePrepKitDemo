using System;
using UnityEngine;

public class XdFileWrapper : ScriptableObject
{
    [System.NonSerialized] public string fileName; // path is relative to Assets/
    [System.NonSerialized] public bool imported;
    [System.NonSerialized] public DateTime LastImportedDate;
    [System.NonSerialized] public DateTime CurrentDate;
}