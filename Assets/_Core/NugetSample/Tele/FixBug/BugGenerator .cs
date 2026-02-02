using UnityEngine;

public class BugGenerator : MonoBehaviour
{

#region === RUNTIME DATA ===
public GameObject target;
#endregion

#region === DEBUG ===
[ContextMenu("🔥 Generate Bug")]
    void GenerateBug()
    {
        // target chưa gán → crash
        target.transform.position = Vector3.zero;
    }
#endregion
}
