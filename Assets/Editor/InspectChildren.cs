#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InspectChildren
{
    [MenuItem("Tools/Inspect Children")]
    public static void Execute()
    {
        string[] targetNames = new[] { "Arboles", "Bancas", "Lamparas", "Fuente Parque de la vida", "CAFETERIA1", "MURO DE LOS ENAMORADOS", "Park-Arch2", "Monumentos" };
        foreach (var name in targetNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            Debug.Log($"--- {go.name} (Child count: {go.transform.childCount}) ---");
            for (int i = 0; i < Mathf.Min(5, go.transform.childCount); i++)
            {
                var child = go.transform.GetChild(i);
                var mf = child.GetComponent<MeshFilter>();
                var col = child.GetComponent<Collider>();
                Debug.Log($"  Child '{child.name}': MF={(mf != null ? mf.sharedMesh?.name : "null")}, Col={(col != null ? col.GetType().Name : "none")}, SubChildren={child.childCount}");
            }
        }
    }
}
#endif
