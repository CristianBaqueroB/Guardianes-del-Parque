#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectFountain
{
    [MenuItem("Tools/Inspect Fountain")]
    public static void Execute()
    {
        var f = GameObject.Find("Fuente Parque de la vida");
        if (f == null) return;
        Debug.Log($"Fountain Root: Pos={f.transform.position}, Scale={f.transform.lossyScale}");
        var mfs = f.GetComponentsInChildren<MeshFilter>(true);
        var large = mfs.Where(m => m.sharedMesh != null && m.sharedMesh.vertexCount > 1000).ToList();
        Debug.Log($"Fountain: Total MFs={mfs.Length}, MFs with >1000 verts={large.Count}");

        // Calculate combined bounds
        var renderers = f.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            Debug.Log($"Fountain Combined World Bounds: Center={b.center}, Size={b.size}");
        }
    }
}
#endif
