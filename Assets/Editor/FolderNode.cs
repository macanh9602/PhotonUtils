using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FolderNode
{
    public string Name;
    public bool Enabled = true;
    public List<FolderNode> Children = new();

    public FolderNode(string name)
    {
        Name = name;
    }
}
