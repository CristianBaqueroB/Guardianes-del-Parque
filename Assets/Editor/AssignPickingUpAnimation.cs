#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AssignPickingUpAnimation
{
    private const string FbxPath = "Assets/Animations/Picking Up.fbx";
    private static readonly string[] ControllerPaths = new[]
    {
        "Assets/PlayerAnimator.controller",
        "Assets/Personaje/Controlador_Personaje.controller"
    };

    [MenuItem("Tools/Assign Picking Up Animation")]
    public static void Execute()
    {
        Debug.Log("[AssignPickingUpAnimation] Starting animation assignment process...");

        // 1. Configurar ModelImporter para Picking Up.fbx como Humanoid con CreateFromThisModel
        ModelImporter importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"[AssignPickingUpAnimation] Could not find ModelImporter at {FbxPath}");
            return;
        }

        bool needReimport = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            needReimport = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            needReimport = true;
        }

        importer.autoGenerateAvatarMappingIfUnspecified = true;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.clipAnimations;
        }

        if (clips != null && clips.Length > 0)
        {
            ModelImporterClipAnimation clipAnim = clips[0];
            clipAnim.name = "Picking Up";
            clipAnim.loopTime = false;
            clipAnim.wrapMode = WrapMode.Once;
            importer.clipAnimations = new ModelImporterClipAnimation[] { clipAnim };
            needReimport = true;
        }

        if (needReimport)
        {
            importer.SaveAndReimport();
            Debug.Log("[AssignPickingUpAnimation] Reimported Picking Up.fbx as Humanoid.");
        }

        // 2. Cargar AnimationClip
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
        AnimationClip pickingClip = allAssets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        if (pickingClip == null)
        {
            Debug.LogError($"[AssignPickingUpAnimation] No valid AnimationClip found in {FbxPath}");
            return;
        }
        Debug.Log($"[AssignPickingUpAnimation] Loaded AnimationClip: '{pickingClip.name}' (Length: {pickingClip.length}s, isHumanMotion: {pickingClip.isHumanMotion})");

        // 3. Configurar los controladores
        foreach (var path in ControllerPaths)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            Debug.Log($"[AssignPickingUpAnimation] Configuring controller: {path}");

            if (!controller.parameters.Any(p => p.name == "PickUp"))
            {
                controller.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            ChildAnimatorState childState = sm.states.FirstOrDefault(s => s.state.name == "Picking Up");
            AnimatorState pickingState = childState.state;

            if (pickingState == null)
            {
                pickingState = sm.AddState("Picking Up", new Vector3(300, 230, 0));
            }

            pickingState.motion = pickingClip;
            pickingState.writeDefaultValues = true;
            pickingState.speed = 1.35f; // Velocidad óptima para recoger objetos de forma ágil

            AnimatorState defaultState = sm.defaultState;
            if (defaultState != null && defaultState != pickingState)
            {
                var existingToTransitions = defaultState.transitions.Where(t => t.destinationState == pickingState).ToList();
                foreach (var t in existingToTransitions) defaultState.RemoveTransition(t);

                var existingFromTransitions = pickingState.transitions.Where(t => t.destinationState == defaultState).ToList();
                foreach (var t in existingFromTransitions) pickingState.RemoveTransition(t);

                var transTo = defaultState.AddTransition(pickingState);
                transTo.hasExitTime = false;
                transTo.hasFixedDuration = true;
                transTo.duration = 0.1f;
                transTo.AddCondition(AnimatorConditionMode.If, 0, "PickUp");

                var transBack = pickingState.AddTransition(defaultState);
                transBack.hasExitTime = true;
                transBack.exitTime = 0.90f;
                transBack.hasFixedDuration = true;
                transBack.duration = 0.2f;
            }
            else if (defaultState == null)
            {
                sm.defaultState = pickingState;
            }

            EditorUtility.SetDirty(controller);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AssignPickingUpAnimation] SUCCESS: All controllers configured!");
    }
}
#endif
