using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Merge2.SceneEditor.Runtime
{
    public class DialoguePlayer : MonoBehaviour
    {
        private GameObject dialogueOverlay;
        private TextMesh speakerText;
        private TextMesh lineText;

        public IEnumerator Play(DialogueSequenceConfig sequence)
        {
            if (sequence == null)
            {
                yield break;
            }

            foreach (var line in sequence.Lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    continue;
                }

                Debug.Log($"[Merge-2 Dialogue] {line.speakerName}: {line.text}", this);
                ShowLine(line);

                var elapsed = 0f;
                var ignoreClickUntil = Time.unscaledTime + 0.15f;
                while (true)
                {
                    if (Time.unscaledTime >= ignoreClickUntil && WasAdvancePressed())
                    {
                        break;
                    }

                    if (!line.waitForClick && line.autoWaitTime > 0f)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        if (elapsed >= line.autoWaitTime)
                        {
                            break;
                        }
                    }

                    yield return null;
                }
            }

            HideOverlay();
        }

        private void ShowLine(DialogueLine line)
        {
            EnsureOverlay();

            if (speakerText != null)
            {
                speakerText.text = string.IsNullOrWhiteSpace(line.speakerName) ? "角色" : line.speakerName;
            }

            if (lineText != null)
            {
                lineText.text = WrapDialogueText(line.text, 14);
            }
        }

        private void EnsureOverlay()
        {
            if (dialogueOverlay != null)
            {
                return;
            }

            dialogueOverlay = new GameObject("Runtime Dialogue Overlay");
            dialogueOverlay.transform.SetParent(transform, false);
            dialogueOverlay.transform.localPosition = new Vector3(0f, -2.32f, -0.2f);

            AddOverlayBlock(dialogueOverlay.transform, "Dialogue Box", new Color32(20, 24, 30, 228), new Vector3(0f, 0f, 0f), new Vector3(3.05f, 0.68f, 1f));
            AddOverlayBlock(dialogueOverlay.transform, "Name Plate", new Color32(238, 184, 76, 255), new Vector3(-1.05f, 0.36f, -0.01f), new Vector3(0.78f, 0.2f, 1f));
            speakerText = AddOverlayText(dialogueOverlay.transform, "Speaker", string.Empty, new Vector3(-1.05f, 0.36f, -0.03f), 0.046f, Color.black);
            lineText = AddOverlayText(dialogueOverlay.transform, "Line", string.Empty, new Vector3(0f, -0.06f, -0.03f), 0.044f, Color.white);
            AddOverlayText(dialogueOverlay.transform, "Next Hint", "点击任意位置继续", new Vector3(0.98f, -0.34f, -0.03f), 0.026f, new Color32(190, 198, 205, 255));
        }

        private void HideOverlay()
        {
            if (dialogueOverlay != null)
            {
                Destroy(dialogueOverlay);
            }

            dialogueOverlay = null;
            speakerText = null;
            lineText = null;
        }

        private static bool WasAdvancePressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private static void AddOverlayBlock(Transform parent, string name, Color32 color, Vector3 localPosition, Vector3 localScale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Quad);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;

            var renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = color
            };

            var collider = block.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static TextMesh AddOverlayText(Transform parent, string name, string text, Vector3 localPosition, float characterSize, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;

            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = characterSize;
            mesh.fontSize = 34;
            mesh.color = color;
            return mesh;
        }

        private static string WrapDialogueText(string text, int maxCharactersPerLine)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxCharactersPerLine)
            {
                return text;
            }

            var wrapped = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % maxCharactersPerLine == 0)
                {
                    wrapped += "\n";
                }

                wrapped += text[i];
            }

            return wrapped;
        }
    }
}
