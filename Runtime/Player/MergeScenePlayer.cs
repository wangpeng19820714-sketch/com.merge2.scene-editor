using UnityEngine;

namespace Merge2.SceneEditor.Runtime
{
    public class MergeScenePlayer : MonoBehaviour
    {
        [SerializeField] private MergeSceneConfig sceneConfig;
        [SerializeField] private MergeStagePlayer stagePlayer;
        [SerializeField] private int currentStageIndex;

        public MergeSceneConfig SceneConfig
        {
            get => sceneConfig;
            set => sceneConfig = value;
        }

        public int CurrentStageIndex => currentStageIndex;

        private void Reset()
        {
            stagePlayer = GetComponentInChildren<MergeStagePlayer>();
        }

        public void LoadSceneConfig(MergeSceneConfig config)
        {
            sceneConfig = config;
            currentStageIndex = 0;
            LoadCurrentStage();
        }

        public void PlayStage(int index)
        {
            if (!IsValidStageIndex(index))
            {
                return;
            }

            currentStageIndex = index;
            LoadCurrentStage();
            stagePlayer.PlayStage();
        }

        public void PlayCurrentStage()
        {
            if (stagePlayer == null)
            {
                return;
            }

            stagePlayer.PlayStage();
        }

        public void PlayNextStage()
        {
            PlayStage(currentStageIndex + 1);
        }

        public void PlayPreviousStage()
        {
            PlayStage(currentStageIndex - 1);
        }

        public void ResetCurrentStage()
        {
            if (stagePlayer != null)
            {
                stagePlayer.ResetStage();
            }
        }

        public void CompleteCurrentStage()
        {
            if (stagePlayer != null)
            {
                stagePlayer.CompleteRepair();
            }
        }

        private void LoadCurrentStage()
        {
            if (stagePlayer == null || sceneConfig == null || !IsValidStageIndex(currentStageIndex))
            {
                return;
            }

            stagePlayer.LoadStage(sceneConfig.Stages[currentStageIndex]);
        }

        private bool IsValidStageIndex(int index)
        {
            return sceneConfig != null && index >= 0 && index < sceneConfig.Stages.Count;
        }
    }
}
