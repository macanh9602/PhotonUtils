using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneTemplate", menuName = "Project/Scene Template")]
public class SceneTemplate : ScriptableObject
{
    public List<SceneTemplateNode> RootObjects = new();
}