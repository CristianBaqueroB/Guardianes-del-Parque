#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class InspectColliders
{
    [MenuItem("Tools/Inspect Colliders")]
    public static void Execute()
    {
        var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int totalRenderers = allRenderers.Length;
        int withCollider = 0;
        int withoutCollider = 0;
        var missingList = new List<string>();

        foreach (var r in allRenderers)
        {
            var col = r.GetComponent<Collider>();
            if (col == null) col = r.GetComponentInParent<Collider>();
            if (col != null)
            {
                withCollider++;
            }
            else
            {
                withoutCollider++;
                string path = r.gameObject.name;
                Transform t = r.transform.parent;
                while (t != null)
                {
                    path = t.name + "/" + path;
                    t = t.parent;
                }
                missingList.Add(path);
            }
        }

        Debug.Log($"[InspectColliders] Total MeshRenderers: {totalRenderers}, With Collider: {withCollider}, WITHOUT Collider: {withoutCollider}");
        for (int i = 0; i < Mathf.Min(30, missingList.Count); i++)
        {
            Debug.Log($"[InspectColliders] Missing: {missingList[i]}");
        }
    }
}
#endif
