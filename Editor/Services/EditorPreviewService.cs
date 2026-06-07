using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEngine;

namespace Merge2.SceneEditor.Editor
{
    public static class EditorPreviewService
    {
        private const string PreviewRootName = "__Merge2StagePreview";

        public static void PreviewStage(MergeStageConfig stage, StagePreviewState state)
        {
            ClearPreview();

            if (stage == null)
            {
                return;
            }

            var root = new GameObject(PreviewRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Merge Stage Preview");

            var prefab = state == StagePreviewState.After ? stage.AfterPrefab : stage.BeforePrefab;
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Create Merge Stage Preview Prefab");
                    instance.transform.SetParent(root.transform, false);
                }
            }

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
        }

        public static void ClearPreview()
        {
            var existing = GameObject.Find(PreviewRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }
    }
}
