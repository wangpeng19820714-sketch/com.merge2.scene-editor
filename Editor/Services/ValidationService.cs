using System.Collections.Generic;
using System.Linq;
using Merge2.SceneEditor.Runtime;

namespace Merge2.SceneEditor.Editor
{
    public static class ValidationService
    {
        public static List<ValidationResult> Validate(MergeSceneConfig sceneConfig)
        {
            var results = new List<ValidationResult>();

            if (sceneConfig == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, "SceneConfig 为空。"));
                return results;
            }

            if (string.IsNullOrWhiteSpace(sceneConfig.SceneId))
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, "SceneId 为空。"));
            }

            if (sceneConfig.Stages.Count == 0)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, "当前场景没有任何阶段。"));
            }

            var duplicateStageIds = sceneConfig.Stages
                .Where(stage => stage != null)
                .GroupBy(stage => stage.StageId)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .Select(group => group.Key);

            foreach (var stageId in duplicateStageIds)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"StageId 重复：{stageId}。"));
            }

            for (var i = 0; i < sceneConfig.Stages.Count; i++)
            {
                ValidateStage(sceneConfig.Stages[i], i, results);
            }

            if (!results.Any(result => result.Severity == ValidationSeverity.Error || result.Severity == ValidationSeverity.Warning))
            {
                results.Add(new ValidationResult(ValidationSeverity.Pass, "配置检查通过。"));
            }

            return results;
        }

        private static void ValidateStage(MergeStageConfig stage, int index, List<ValidationResult> results)
        {
            var label = stage != null && !string.IsNullOrWhiteSpace(stage.StageId) ? stage.StageId : $"阶段 {index + 1}";

            if (stage == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} 缺少 StageConfig。"));
                return;
            }

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} 缺少 StageId。"));
            }

            if (string.IsNullOrWhiteSpace(stage.StageName))
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} 缺少阶段名称。"));
            }

            if (stage.BeforePrefab == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} 缺少修复前 Prefab。"));
            }

            if (stage.AfterPrefab == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} 缺少修复后 Prefab。"));
            }

            if (stage.RepairTimeline == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} 没有绑定修复 Timeline。"));
            }

            if (stage.DialogueSequence == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} 没有绑定对话。"));
                return;
            }

            for (var i = 0; i < stage.DialogueSequence.Lines.Count; i++)
            {
                var line = stage.DialogueSequence.Lines[i];
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} 第 {i + 1} 行对话为空。"));
                }
            }
        }
    }
}
