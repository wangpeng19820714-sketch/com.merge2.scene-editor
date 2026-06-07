using System.Collections.Generic;
using UnityEngine;

namespace Merge2.SceneEditor.Runtime
{
    [CreateAssetMenu(menuName = "Merge-2 Scene Editor/Scene Config", fileName = "MSC_NewScene")]
    public sealed class MergeSceneConfig : ScriptableObject
    {
        [SerializeField] private string sceneId = "SC_NewScene";
        [SerializeField] private string sceneName = "New Scene";
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private GameObject sceneRootPrefab;
        [Header("Dialogue UI")]
        [SerializeField] private GameObject dialogueInterfacePrefab;
        [SerializeField] private GameObject defaultDialogueItemPrefab;
        [SerializeField] private Sprite defaultAvatarFrame;
        [SerializeField] private Sprite defaultDialogueBackground;
        [SerializeField] private List<MergeStageConfig> stages = new();

        public string SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        public string SceneName
        {
            get => sceneName;
            set => sceneName = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }

        public GameObject SceneRootPrefab
        {
            get => sceneRootPrefab;
            set => sceneRootPrefab = value;
        }

        public GameObject DialogueInterfacePrefab
        {
            get => dialogueInterfacePrefab;
            set => dialogueInterfacePrefab = value;
        }

        public GameObject DefaultDialogueItemPrefab
        {
            get => defaultDialogueItemPrefab;
            set => defaultDialogueItemPrefab = value;
        }

        public Sprite DefaultAvatarFrame
        {
            get => defaultAvatarFrame;
            set => defaultAvatarFrame = value;
        }

        public Sprite DefaultDialogueBackground
        {
            get => defaultDialogueBackground;
            set => defaultDialogueBackground = value;
        }

        public List<MergeStageConfig> Stages => stages;
    }
}
