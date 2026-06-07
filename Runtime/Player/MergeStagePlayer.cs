using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace Merge2.SceneEditor.Runtime
{
    public class MergeStagePlayer : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private DialoguePlayer dialoguePlayer;

        private MergeStageConfig currentStage;
        private GameObject beforeInstance;
        private GameObject afterInstance;
        private Coroutine playRoutine;

        public MergeStageConfig CurrentStage => currentStage;

        private void Reset()
        {
            playableDirector = GetComponent<PlayableDirector>();
            dialoguePlayer = GetComponent<DialoguePlayer>();
        }

        public void LoadStage(MergeStageConfig stage)
        {
            currentStage = stage;
            ClearStageObjects();

            if (currentStage == null)
            {
                return;
            }

            if (currentStage.BeforePrefab != null)
            {
                beforeInstance = Instantiate(currentStage.BeforePrefab, transform);
                beforeInstance.name = currentStage.BeforePrefab.name;
            }

            if (currentStage.AfterPrefab != null)
            {
                afterInstance = Instantiate(currentStage.AfterPrefab, transform);
                afterInstance.name = currentStage.AfterPrefab.name;
                afterInstance.SetActive(currentStage.DefaultPreviewState == StagePreviewState.After);
            }

            if (beforeInstance != null)
            {
                beforeInstance.SetActive(currentStage.DefaultPreviewState != StagePreviewState.After);
            }
        }

        public void PlayStage()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            playRoutine = StartCoroutine(PlayStageRoutine());
        }

        public void ResetStage()
        {
            LoadStage(currentStage);
        }

        public void CompleteRepair()
        {
            if (beforeInstance != null)
            {
                beforeInstance.SetActive(false);
            }

            if (afterInstance != null)
            {
                afterInstance.SetActive(true);
            }
        }

        public void CompleteRepairAndPlayDialogue()
        {
            CompleteRepair();

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            playRoutine = StartCoroutine(PlayDialogueRoutine());
        }

        public void ClearStageObjects()
        {
            DestroyStageObject(beforeInstance);
            DestroyStageObject(afterInstance);
            beforeInstance = null;
            afterInstance = null;
        }

        private IEnumerator PlayStageRoutine()
        {
            if (currentStage == null)
            {
                yield break;
            }

            if (currentStage.RepairTimeline != null && playableDirector != null)
            {
                playableDirector.playableAsset = currentStage.RepairTimeline;
                BindRepairAnimationTracks();
                playableDirector.Play();

                if (playableDirector.duration > 0d)
                {
                    while (playableDirector.state == PlayState.Playing)
                    {
                        yield return null;
                    }
                }
                else
                {
                    playableDirector.Stop();
                }
            }

            yield return PlayDialogueRoutine();
        }

        private IEnumerator PlayDialogueRoutine()
        {
            if (currentStage == null)
            {
                yield break;
            }

            CompleteRepair();

            if (currentStage.PlayDialogue && currentStage.DialogueSequence != null && dialoguePlayer != null)
            {
                yield return dialoguePlayer.Play(currentStage.DialogueSequence);
            }
        }

        private static void DestroyStageObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        private void BindRepairAnimationTracks()
        {
            if (currentStage == null || currentStage.RepairTimeline == null || playableDirector == null || beforeInstance == null)
            {
                return;
            }

            var animator = beforeInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = beforeInstance.AddComponent<Animator>();
            }

            foreach (var output in currentStage.RepairTimeline.outputs)
            {
                if (output.outputTargetType == typeof(Animator))
                {
                    playableDirector.SetGenericBinding(output.sourceObject, animator);
                }
            }
        }
    }
}
