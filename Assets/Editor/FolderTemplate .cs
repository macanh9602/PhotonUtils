using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FolderTemplate",
    menuName = "Project/Folder Template"
)]
public class FolderTemplate : ScriptableObject
{
    public List<FolderNode> RootFolders = new();
}
