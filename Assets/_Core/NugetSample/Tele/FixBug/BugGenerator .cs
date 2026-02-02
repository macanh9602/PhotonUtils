using UnityEngine;

public class BugGenerator : MonoBehaviour
{
    public GameObject target;

    [ContextMenu("🔥 Generate Bug")]
    void GenerateBug()
    {
        // target chưa gán → crash
        target.transform.position = Vector3.zero;
    }
}
