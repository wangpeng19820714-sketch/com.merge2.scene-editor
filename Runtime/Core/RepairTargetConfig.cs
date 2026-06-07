using UnityEngine;

namespace Merge2.SceneEditor.Runtime
{
    [System.Serializable]
    public sealed class RepairTargetConfig
    {
        public string targetId;
        public string displayName;
        public GameObject beforePrefab;
        public GameObject afterPrefab;
        public Vector2 previewPosition;
    }
}
