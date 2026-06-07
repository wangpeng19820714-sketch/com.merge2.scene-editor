using System;
using System.IO;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEngine;

namespace Merge2.SceneEditor.Editor
{
    public static class MergeSceneAssetService
    {
        public const string ContentRoot = "Assets/GameContent/MergeScenes";

        public static MergeSceneConfig CreateSceneConfig(string sceneId, string sceneName)
        {
            sceneId = SanitizeIdentifier(sceneId, "SC_NewScene");
            sceneName = string.IsNullOrWhiteSpace(sceneName) ? sceneId : sceneName.Trim();

            var sceneFolder = EnsureSceneFolders(sceneId);
            var configPath = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/Configs/MSC_{sceneId}.asset");
            var config = ScriptableObject.CreateInstance<MergeSceneConfig>();
            config.SceneId = sceneId;
            config.SceneName = sceneName;

            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;
            return config;
        }

        public static MergeStageConfig CreateStage(MergeSceneConfig sceneConfig)
        {
            if (sceneConfig == null)
            {
                throw new ArgumentNullException(nameof(sceneConfig));
            }

            var sceneId = SanitizeIdentifier(sceneConfig.SceneId, "SC_NewScene");
            var stageNumber = sceneConfig.Stages.Count + 1;
            var stageId = $"Stage_{stageNumber:00}";
            var sceneFolder = EnsureSceneFolders(sceneId);

            var dialogue = ScriptableObject.CreateInstance<DialogueSequenceConfig>();
            dialogue.DialogueId = $"DLG_{sceneId}_{stageId}";
            var dialoguePath = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/Dialogues/{dialogue.DialogueId}.asset");
            AssetDatabase.CreateAsset(dialogue, dialoguePath);

            var stage = ScriptableObject.CreateInstance<MergeStageConfig>();
            stage.StageId = stageId;
            stage.StageName = $"Stage {stageNumber:00}";
            stage.DialogueSequence = dialogue;
            stage.LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            stage.LastModifiedBy = Environment.UserName;

            var stagePath = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/Configs/{stageId}.asset");
            AssetDatabase.CreateAsset(stage, stagePath);
            TimelineAssetService.EnsureTimelines(sceneConfig, stage);

            Undo.RecordObject(sceneConfig, "Add Merge Stage");
            sceneConfig.Stages.Add(stage);
            EditorUtility.SetDirty(sceneConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = stage;
            return stage;
        }

        public static MergeStageConfig DuplicateStage(MergeSceneConfig sceneConfig, MergeStageConfig sourceStage)
        {
            if (sceneConfig == null)
            {
                throw new ArgumentNullException(nameof(sceneConfig));
            }

            if (sourceStage == null)
            {
                throw new ArgumentNullException(nameof(sourceStage));
            }

            var duplicate = CreateStage(sceneConfig);
            Undo.RecordObject(duplicate, "Duplicate Merge Stage");

            duplicate.StageName = $"{sourceStage.StageName} Copy";
            duplicate.StageDescription = sourceStage.StageDescription;
            duplicate.StageIcon = sourceStage.StageIcon;
            duplicate.BeforePrefab = sourceStage.BeforePrefab;
            duplicate.AfterPrefab = sourceStage.AfterPrefab;
            duplicate.CanSkip = sourceStage.CanSkip;
            duplicate.PlayDialogue = sourceStage.PlayDialogue;
            duplicate.PlayCameraTimeline = sourceStage.PlayCameraTimeline;
            duplicate.DefaultPreviewState = sourceStage.DefaultPreviewState;
            duplicate.ValidationState = StageValidationState.None;
            duplicate.AcceptanceDescription = sourceStage.AcceptanceDescription;
            duplicate.AcceptanceScreenshotPath = string.Empty;
            duplicate.Locked = false;
            duplicate.LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            duplicate.LastModifiedBy = Environment.UserName;

            CopyDialogue(sourceStage.DialogueSequence, duplicate.DialogueSequence);

            EditorUtility.SetDirty(duplicate);
            if (duplicate.DialogueSequence != null)
            {
                EditorUtility.SetDirty(duplicate.DialogueSequence);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = duplicate;
            return duplicate;
        }

        public static string GetValidationScreenshotPath(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var sceneId = sceneConfig != null ? SanitizeIdentifier(sceneConfig.SceneId, "SC_NewScene") : "SC_NewScene";
            var stageId = stageConfig != null ? SanitizeIdentifier(stageConfig.StageId, "Stage_00") : "Stage_00";
            EnsureSceneFolders(sceneId);
            return $"{ContentRoot}/{sceneId}/Validation/{stageId}.png";
        }

        public static string EnsureSceneFolders(string sceneId)
        {
            sceneId = SanitizeIdentifier(sceneId, "SC_NewScene");
            EnsureFolder("Assets", "GameContent");
            EnsureFolder("Assets/GameContent", "MergeScenes");

            var sceneFolder = $"{ContentRoot}/{sceneId}";
            EnsureFolder(ContentRoot, sceneId);
            EnsureFolder(sceneFolder, "Configs");
            EnsureFolder(sceneFolder, "Timelines");
            EnsureFolder(sceneFolder, "Dialogues");
            EnsureFolder(sceneFolder, "Prefabs");
            EnsureFolder(sceneFolder, "Screenshots");
            EnsureFolder(sceneFolder, "Validation");
            return sceneFolder;
        }

        public static string SanitizeIdentifier(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var trimmed = value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                trimmed = trimmed.Replace(invalid, '_');
            }

            trimmed = trimmed.Replace(' ', '_');
            return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void CopyDialogue(DialogueSequenceConfig source, DialogueSequenceConfig target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.Lines.Clear();
            foreach (var line in source.Lines)
            {
                if (line == null)
                {
                    continue;
                }

                target.Lines.Add(new DialogueLine
                {
                    speakerId = line.speakerId,
                    speakerName = line.speakerName,
                    portrait = line.portrait,
                    emotion = line.emotion,
                    text = line.text,
                    voiceKey = line.voiceKey,
                    typewriterSpeed = line.typewriterSpeed,
                    autoWaitTime = line.autoWaitTime,
                    waitForClick = line.waitForClick
                });
            }
        }
    }
}
