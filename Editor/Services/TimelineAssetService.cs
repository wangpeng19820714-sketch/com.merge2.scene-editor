using System.IO;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace Merge2.SceneEditor.Editor
{
    public static class TimelineAssetService
    {
        public static void EnsureTimelines(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            if (sceneConfig == null || stageConfig == null)
            {
                return;
            }

            Undo.RecordObject(stageConfig, "Ensure Stage Timelines");

            if (stageConfig.RepairTimeline == null)
            {
                stageConfig.RepairTimeline = CreateRepairTimeline(sceneConfig, stageConfig);
            }

            if (stageConfig.CameraTimeline == null)
            {
                stageConfig.CameraTimeline = CreateCameraTimeline(sceneConfig, stageConfig);
            }

            EditorUtility.SetDirty(stageConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static TimelineAsset RecreateRepairTimeline(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var timeline = CreateRepairTimeline(sceneConfig, stageConfig);
            Undo.RecordObject(stageConfig, "Recreate Repair Timeline");
            stageConfig.RepairTimeline = timeline;
            EditorUtility.SetDirty(stageConfig);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        public static TimelineAsset RecreateCameraTimeline(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var timeline = CreateCameraTimeline(sceneConfig, stageConfig);
            Undo.RecordObject(stageConfig, "Recreate Camera Timeline");
            stageConfig.CameraTimeline = timeline;
            EditorUtility.SetDirty(stageConfig);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        public static void OpenTimeline(MergeStageConfig stageConfig, bool openCameraTimeline)
        {
            if (stageConfig == null)
            {
                return;
            }

            var timeline = openCameraTimeline ? stageConfig.CameraTimeline : stageConfig.RepairTimeline;
            if (timeline == null)
            {
                return;
            }

            Selection.activeObject = timeline;
            AssetDatabase.OpenAsset(timeline);
        }

        private static TimelineAsset CreateRepairTimeline(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var timeline = CreateTimelineAsset(sceneConfig, stageConfig, "Repair");
            var repairTrack = timeline.CreateTrack<AnimationTrack>(null, "Repair Animation");
            var motionClip = CreateRepairMotionClip(sceneConfig, stageConfig);
            var timelineClip = repairTrack.CreateClip(motionClip);
            timelineClip.displayName = "Repair Move Preview";
            timelineClip.start = 0d;
            timelineClip.duration = motionClip.length;
            timeline.CreateTrack<ActivationTrack>(null, "Before After Activation");
            timeline.CreateTrack<AudioTrack>(null, "SFX");
            timeline.CreateTrack<SignalTrack>(null, "Repair Events");
            return timeline;
        }

        private static TimelineAsset CreateCameraTimeline(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var timeline = CreateTimelineAsset(sceneConfig, stageConfig, "Camera");
            timeline.CreateTrack<AnimationTrack>(null, "Camera Animation");
            timeline.CreateTrack<SignalTrack>(null, "Camera Cues");
            return timeline;
        }

        private static TimelineAsset CreateTimelineAsset(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig, string suffix)
        {
            var sceneId = MergeSceneAssetService.SanitizeIdentifier(sceneConfig.SceneId, "SC_NewScene");
            var stageId = MergeSceneAssetService.SanitizeIdentifier(stageConfig.StageId, "Stage_00");
            var sceneFolder = MergeSceneAssetService.EnsureSceneFolders(sceneId);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"TL_{sceneId}_{stageId}_{suffix}";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/Timelines/{timeline.name}.playable");
            AssetDatabase.CreateAsset(timeline, path);
            return timeline;
        }

        private static AnimationClip CreateRepairMotionClip(MergeSceneConfig sceneConfig, MergeStageConfig stageConfig)
        {
            var sceneId = MergeSceneAssetService.SanitizeIdentifier(sceneConfig.SceneId, "SC_NewScene");
            var stageId = MergeSceneAssetService.SanitizeIdentifier(stageConfig.StageId, "Stage_00");
            var sceneFolder = MergeSceneAssetService.EnsureSceneFolders(sceneId);
            EnsureFolder($"{sceneFolder}/Timelines", "Animations");

            var clip = new AnimationClip
            {
                name = $"AN_{sceneId}_{stageId}_RepairMove",
                frameRate = 30f,
                legacy = false
            };

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.x"),
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.25f, -0.12f),
                    new Keyframe(0.5f, 0.18f),
                    new Keyframe(0.75f, -0.08f),
                    new Keyframe(1f, 0f)));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y"),
                new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.25f, 0.08f),
                    new Keyframe(0.5f, -0.04f),
                    new Keyframe(0.75f, 0.06f),
                    new Keyframe(1f, 0f)));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.x"),
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.5f, 1.08f),
                    new Keyframe(1f, 1f)));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.y"),
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.5f, 1.08f),
                    new Keyframe(1f, 1f)));

            var path = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/Timelines/Animations/{clip.name}.anim");
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
