#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to add precise MeshColliders to all environment objects,
/// removing any box collider logic.
/// </summary>
public static class AddCollidersToObjects
{
    // Mapping of object name (partial) → MeshCollider for all objects
    private static readonly Dictionary<string, System.Type> ColliderMap = new()
    {
        { "Lampara",                 typeof(MeshCollider) },
        { "Park-Lamp",               typeof(MeshCollider) },
        { "Arboles",                 typeof(MeshCollider) },
        { "deciduous tree",          typeof(MeshCollider) },
        { "bonsai tree",             typeof(MeshCollider) },
        { "Monumentos",              typeof(MeshCollider) },
        { "MURO DE LOS ENAMORADOS",  typeof(MeshCollider) },
        { "CAFETERIA1",              typeof(MeshCollider) },
        { "Park-Arch2",              typeof(MeshCollider) },
        { "Bancas",                  typeof(MeshCollider) },
        { "Banco",                   typeof(MeshCollider) },
        { "Basura",                  typeof(MeshCollider) },
        { "mesh_TrashBin",           typeof(MeshCollider) },
        { "carpet red",              typeof(MeshCollider) },
        { "Mesh_Bag1",               typeof(MeshCollider) },
        { "Mesh_Pile1",              typeof(MeshCollider) },
        { "Mesh_WatterBottle_L",     typeof(MeshCollider) },
        { "Mesh_SodaBottle_L",       typeof(MeshCollider) },
        { "Fuente Parque de la vida",typeof(MeshCollider) }
    };

    [MenuItem("Tools/Add Colliders to Environment Objects")]
    private static void AddColliders()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int addedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            foreach (var kvp in ColliderMap)
            {
                if (prefab.name.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                    if (prefabRoot == null) continue;

                    if (!prefabRoot.GetComponent(kvp.Value))
                    {
                        Component collider = prefabRoot.AddComponent(kvp.Value);
                        ConfigureCollider(prefabRoot, collider);

                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                        addedCount++;
                        Debug.Log($"[AddColliders] Added MeshCollider to prefab '{prefab.name}'.");
                    }
                    else
                    {
                        PrefabUtility.UnloadPrefabContents(PrefabUtility.LoadPrefabContents(assetPath));
                    }
                }
            }
        }

        GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in sceneRoots)
        {
            AddColliderRecursively(root);
        }

        Debug.Log($"[AddColliders] Finished. Added MeshColliders to {addedCount} prefabs (scene objects processed separately).");
    }

    private static void AddColliderRecursively(GameObject obj)
    {
        foreach (var kvp in ColliderMap)
        {
            if (obj.name.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!obj.GetComponent(kvp.Value))
                {
                    Component col = obj.AddComponent(kvp.Value);
                    ConfigureCollider(obj, col);

                    Debug.Log($"[AddColliders] Added MeshCollider to scene object '{obj.name}'.");
                }
                break;
            }
        }

        foreach (Transform child in obj.transform)
        {
            AddColliderRecursively(child.gameObject);
        }
    }

    private static void ConfigureCollider(GameObject targetObj, Component collider)
    {
        if (collider is MeshCollider meshCol)
        {
            MeshFilter mf = targetObj.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = targetObj.GetComponentInChildren<MeshFilter>();
            }

            if (mf != null)
            {
                meshCol.sharedMesh = mf.sharedMesh;
            }
            
            meshCol.convex = true;
        }
    }
}
#endif