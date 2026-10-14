#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InspectMeshes
{
    [MenuItem("Tools/Inspect Meshes")]
    public static void Execute()
    {
        string[] targetNames = new[] { "Arboles", "Bancas", "Lamparas", "Fuente Parque de la vida", "CAFETERIA1", "MURO DE LOS ENAMORADOS", "Park-Arch2", "Monumentos" };
        foreach (var name in targetNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            var mfs = go.GetComponentsInChildren<MeshFilter>(true);
            int totalTris = 0;
            int totalVerts = 0;
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                {
                    totalTris += mf.sharedMesh.triangles.Length / 3;
                    totalVerts += mf.sharedMesh.vertexCount;
                }
            }
            Debug.Log($"[MeshStats] '{go.name}': MeshFilters={mfs.Length}, Verts={totalVerts}, Tris={totalTris}");
        }
    }
}
#endif
