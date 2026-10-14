#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectRoots
{
    [MenuItem("Tools/Inspect Roots")]
    public static void Execute()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var renderers = r.GetComponentsInChildren<MeshRenderer>(true);
            var colliders = r.GetComponentsInChildren<Collider>(true);
            Debug.Log($"[Root] '{r.name}': Renderers={renderers.Length}, Colliders={colliders.Length}, Active={r.activeSelf}");
        }
    }
}
#endif
