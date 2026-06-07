using UnityEngine;

namespace Merge2.SceneEditor.Runtime
{
    public sealed class StageStateController : MonoBehaviour
    {
        [SerializeField] private GameObject beforeRoot;
        [SerializeField] private GameObject afterRoot;

        public void ShowBefore()
        {
            SetState(true);
        }

        public void ShowAfter()
        {
            SetState(false);
        }

        private void SetState(bool showBefore)
        {
            if (beforeRoot != null)
            {
                beforeRoot.SetActive(showBefore);
            }

            if (afterRoot != null)
            {
                afterRoot.SetActive(!showBefore);
            }
        }
    }
}
