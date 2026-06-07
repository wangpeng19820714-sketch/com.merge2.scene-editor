using System.Collections.Generic;
using UnityEngine;

namespace Merge2.SceneEditor.Runtime
{
    [CreateAssetMenu(menuName = "Merge-2 Scene Editor/Dialogue Sequence", fileName = "DLG_NewSequence")]
    public sealed class DialogueSequenceConfig : ScriptableObject
    {
        [SerializeField] private string dialogueId = "DLG_NewSequence";
        [SerializeField] private List<DialogueLine> lines = new();

        public string DialogueId
        {
            get => dialogueId;
            set => dialogueId = value;
        }

        public List<DialogueLine> Lines => lines;
    }

    [System.Serializable]
    public sealed class DialogueLine
    {
        public string speakerId;
        public string speakerName;
        public Sprite portrait;
        public GameObject dialogueItemPrefab;
        public Sprite avatarFrame;
        public Sprite dialogueBackground;
        public Sprite talkItemContentImage;
        public string emotion;
        [TextArea]
        public string text;
        public string voiceKey;
        public float typewriterSpeed = 24f;
        public float autoWaitTime = 1.5f;
        public bool waitForClick = true;
    }
}
