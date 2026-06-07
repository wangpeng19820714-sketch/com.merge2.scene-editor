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
                results.Add(new ValidationResult(ValidationSeverity.Error, "SceneConfig is null."));
                return results;
            }

            if (string.IsNullOrWhiteSpace(sceneConfig.SceneId))
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, "SceneId is empty."));
            }

            if (sceneConfig.Stages.Count == 0)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, "The current scene has no stages."));
            }

            var duplicateStageIds = sceneConfig.Stages
                .Where(stage => stage != null)
                .GroupBy(stage => stage.StageId)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .Select(group => group.Key);

            foreach (var stageId in duplicateStageIds)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"Duplicate StageId: {stageId}."));
            }

            for (var i = 0; i < sceneConfig.Stages.Count; i++)
            {
                ValidateStage(sceneConfig.Stages[i], i, results);
            }

            if (!results.Any(result => result.Severity == ValidationSeverity.Error || result.Severity == ValidationSeverity.Warning))
            {
                results.Add(new ValidationResult(ValidationSeverity.Pass, "Config validation passed."));
            }

            return results;
        }

        private static void ValidateStage(MergeStageConfig stage, int index, List<ValidationResult> results)
        {
            var label = stage != null && !string.IsNullOrWhiteSpace(stage.StageId) ? stage.StageId : $"Stage {index + 1}";

            if (stage == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} is missing StageConfig."));
                return;
            }

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} is missing StageId."));
            }

            if (string.IsNullOrWhiteSpace(stage.StageName))
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} is missing a stage name."));
            }

            if (stage.BeforePrefab == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} is missing the before prefab."));
            }

            if (stage.AfterPrefab == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Error, $"{label} is missing the after prefab."));
            }

            if (stage.RepairTimeline == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} has no repair Timeline assigned."));
            }

            if (stage.DialogueSequence == null)
            {
                results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} has no dialogue assigned."));
                return;
            }

            for (var i = 0; i < stage.DialogueSequence.Lines.Count; i++)
            {
                var line = stage.DialogueSequence.Lines[i];
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    results.Add(new ValidationResult(ValidationSeverity.Warning, $"{label} dialogue line {i + 1} is empty."));
                }
            }
        }
    }
}
