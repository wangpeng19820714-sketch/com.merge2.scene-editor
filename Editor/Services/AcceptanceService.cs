using System;
using System.IO;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEngine;

namespace Merge2.SceneEditor.Editor
{
    public static class AcceptanceService
    {
        public static bool CaptureSceneViewScreenshot(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig, out string assetPath)
        {
            assetPath = MergeSceneAssetService.GetValidationScreenshotPath(sceneConfig, stageConfig);

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                return false;
            }

            var width = Mathf.Max(1, Mathf.RoundToInt(sceneView.position.width));
            var height = Mathf.Max(1, Mathf.RoundToInt(sceneView.position.height));
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previousTarget = sceneView.camera.targetTexture;
            var previousActive = RenderTexture.active;

            try
            {
                sceneView.camera.targetTexture = renderTexture;
                sceneView.camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                var fullPath = Path.GetFullPath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath);

                Undo.RecordObject(stageConfig, "Capture Acceptance Screenshot");
                stageConfig.AcceptanceScreenshotPath = assetPath;
                stageConfig.LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                stageConfig.LastModifiedBy = Environment.UserName;
                EditorUtility.SetDirty(stageConfig);
                AssetDatabase.SaveAssets();
                return true;
            }
            finally
            {
                sceneView.camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        public static void SetValidationState(MergeStageConfig stageConfig, StageValidationState state)
        {
            if (stageConfig == null)
            {
                return;
            }

            Undo.RecordObject(stageConfig, "Set Stage Validation State");
            stageConfig.ValidationState = state;
            stageConfig.Locked = state == StageValidationState.Locked;
            stageConfig.LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            stageConfig.LastModifiedBy = Environment.UserName;
            EditorUtility.SetDirty(stageConfig);
            AssetDatabase.SaveAssets();
        }
    }
}
