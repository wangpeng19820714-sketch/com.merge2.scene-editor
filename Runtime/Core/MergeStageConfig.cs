using UnityEngine;
using UnityEngine.Playables;

namespace Merge2.SceneEditor.Runtime
{
    [CreateAssetMenu(menuName = "Merge-2 Scene Editor/Stage Config", fileName = "Stage_01")]
    public sealed class MergeStageConfig : ScriptableObject
    {
        [SerializeField] private string stageId = "Stage_01";
        [SerializeField] private string stageName = "New Stage";
        [TextArea]
        [SerializeField] private string stageDescription;
        [SerializeField] private Sprite stageIcon;

        [Header("Repair Assets")]
        [SerializeField] private GameObject beforePrefab;
        [SerializeField] private GameObject afterPrefab;
        [SerializeField] private PlayableAsset repairTimeline;
        [SerializeField] private PlayableAsset cameraTimeline;
        [SerializeField] private DialogueSequenceConfig dialogueSequence;

        [Header("Stage Settings")]
        [SerializeField] private bool canSkip;
        [SerializeField] private bool playDialogue = true;
        [SerializeField] private bool playCameraTimeline = true;
        [SerializeField] private StagePreviewState defaultPreviewState = StagePreviewState.Before;
        [SerializeField] private StageValidationState validationState = StageValidationState.None;

        [Header("Acceptance")]
        [SerializeField] private string acceptanceDescription;
        [SerializeField] private string acceptanceScreenshotPath;
        [SerializeField] private bool locked;
        [SerializeField] private string lastModifiedBy;
        [SerializeField] private string lastModifiedAt;

        public string StageId { get => stageId; set => stageId = value; }
        public string StageName { get => stageName; set => stageName = value; }
        public string StageDescription { get => stageDescription; set => stageDescription = value; }
        public Sprite StageIcon { get => stageIcon; set => stageIcon = value; }
        public GameObject BeforePrefab { get => beforePrefab; set => beforePrefab = value; }
        public GameObject AfterPrefab { get => afterPrefab; set => afterPrefab = value; }
        public PlayableAsset RepairTimeline { get => repairTimeline; set => repairTimeline = value; }
        public PlayableAsset CameraTimeline { get => cameraTimeline; set => cameraTimeline = value; }
        public DialogueSequenceConfig DialogueSequence { get => dialogueSequence; set => dialogueSequence = value; }
        public bool CanSkip { get => canSkip; set => canSkip = value; }
        public bool PlayDialogue { get => playDialogue; set => playDialogue = value; }
        public bool PlayCameraTimeline { get => playCameraTimeline; set => playCameraTimeline = value; }
        public StagePreviewState DefaultPreviewState { get => defaultPreviewState; set => defaultPreviewState = value; }
        public StageValidationState ValidationState { get => validationState; set => validationState = value; }
        public string AcceptanceDescription { get => acceptanceDescription; set => acceptanceDescription = value; }
        public string AcceptanceScreenshotPath { get => acceptanceScreenshotPath; set => acceptanceScreenshotPath = value; }
        public bool Locked { get => locked; set => locked = value; }
        public string LastModifiedBy { get => lastModifiedBy; set => lastModifiedBy = value; }
        public string LastModifiedAt { get => lastModifiedAt; set => lastModifiedAt = value; }
    }

    public enum StagePreviewState
    {
        Before,
        After,
        Runtime
    }

    public enum StageValidationState
    {
        None,
        Pending,
        Approved,
        Rejected,
        Locked
    }
}
