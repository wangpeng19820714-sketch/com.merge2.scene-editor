using System.Collections.Generic;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace Merge2.SceneEditor.Editor
{
    public static class EditorStagePlaybackService
    {
        private const string PlaybackRootName = "__Merge2StagePlayback";

        private static PlayableDirector activeDirector;
        private static GameObject beforeInstance;
        private static GameObject afterInstance;
        private static GameObject dialogueOverlay;
        private static GameObject dialogueUiInstance;
        private static Transform dialogueListContent;
        private static GameObject embeddedDialogueItemTemplate;
        private static UnityEngine.UI.ScrollRect dialogueScrollRect;
        private static TextMesh speakerText;
        private static TextMesh lineText;
        private static readonly List<DialogueLine> dialogueLines = new();
        private static readonly List<EditorWindow> registeredGameViews = new();
        private static int currentDialogueIndex;
        private static MergeSceneConfig activeSceneConfig;
        private static MergeStageConfig activeStage;
        private static double lastUpdateTime;
        private static double ignoreDialogueClicksUntil;
        private static bool dialogueLogged;
        private static bool mouseWasPressed;

        public static bool IsPlaying => activeDirector != null;

        public static void PlayStage(MergeStageConfig stage)
        {
            PlayStage(null, stage);
        }

        public static void PlayStage(MergeSceneConfig sceneConfig, MergeStageConfig stage)
        {
            Stop();

            if (stage == null)
            {
                return;
            }

            activeSceneConfig = sceneConfig;
            activeStage = stage;
            dialogueLogged = false;
            var root = new GameObject(PlaybackRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Merge Stage Playback");
            ConfigureMainCameraForPortraitPreview();

            beforeInstance = InstantiatePrefab(stage.BeforePrefab, root.transform);
            afterInstance = InstantiatePrefab(stage.AfterPrefab, root.transform);

            if (beforeInstance != null)
            {
                beforeInstance.SetActive(true);
            }

            if (afterInstance != null)
            {
                afterInstance.SetActive(false);
            }

            if (stage.RepairTimeline == null)
            {
                Complete();
                Selection.activeGameObject = root;
                SceneView.FrameLastActiveSceneView();
                return;
            }

            activeDirector = root.AddComponent<PlayableDirector>();
            activeDirector.playableAsset = stage.RepairTimeline;
            activeDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            activeDirector.extrapolationMode = DirectorWrapMode.Hold;
            activeDirector.time = 0d;
            BindRepairAnimationTracks(stage.RepairTimeline);
            activeDirector.Play();
            activeDirector.Evaluate();

            lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
        }

        public static void PreviewDialogueList(MergeSceneConfig sceneConfig, MergeStageConfig stage)
        {
            Stop();

            if (stage == null)
            {
                return;
            }

            activeSceneConfig = sceneConfig;
            activeStage = stage;
            dialogueLogged = false;

            var root = new GameObject(PlaybackRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Merge Dialogue Preview");
            ConfigureMainCameraForPortraitPreview();
            CreateDialogueOverlay(stage);
            Selection.activeGameObject = root;
        }

        private static void ConfigureMainCameraForPortraitPreview()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 3.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(36, 42, 48, 255);
        }

        public static void Complete()
        {
            RecoverSceneReferences();

            if (beforeInstance != null)
            {
                beforeInstance.SetActive(false);
            }

            if (afterInstance != null)
            {
                afterInstance.SetActive(true);
            }

            if (activeDirector != null)
            {
                activeDirector.Stop();
                activeDirector = null;
            }

            LogDialoguePreview(activeStage);
            EditorApplication.update -= Tick;
        }

        public static void Stop()
        {
            EditorApplication.update -= Tick;

            if (activeDirector != null)
            {
                activeDirector.Stop();
                activeDirector = null;
            }

            SceneView.duringSceneGui -= HandleSceneViewClick;
            EditorApplication.update -= PollDialogueClick;
            UnregisterGameViewClicks();
            beforeInstance = null;
            afterInstance = null;
            dialogueOverlay = null;
            dialogueUiInstance = null;
            dialogueListContent = null;
            embeddedDialogueItemTemplate = null;
            dialogueScrollRect = null;
            speakerText = null;
            lineText = null;
            dialogueLines.Clear();
            currentDialogueIndex = 0;
            mouseWasPressed = false;
            activeSceneConfig = null;
            activeStage = null;
            dialogueLogged = false;

            var existing = GameObject.Find(PlaybackRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private static void Tick()
        {
            if (activeDirector == null)
            {
                EditorApplication.update -= Tick;
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Max(0f, (float)(now - lastUpdateTime));
            lastUpdateTime = now;

            activeDirector.time += delta;
            activeDirector.Evaluate();

            var duration = activeDirector.duration;
            if (duration > 0d && activeDirector.time >= duration)
            {
                Complete();
            }
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create Merge Stage Playback Prefab");
            instance.transform.SetParent(parent, false);
            instance.name = prefab.name;
            return instance;
        }

        private static void BindRepairAnimationTracks(PlayableAsset timeline)
        {
            if (timeline == null || activeDirector == null || beforeInstance == null)
            {
                return;
            }

            var animator = beforeInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = beforeInstance.AddComponent<Animator>();
            }

            foreach (var output in timeline.outputs)
            {
                if (output.sourceObject is AnimationTrack)
                {
                    activeDirector.SetGenericBinding(output.sourceObject, animator);
                }
            }
        }

        private static void RecoverSceneReferences()
        {
            var root = GameObject.Find(PlaybackRootName);
            if (root == null)
            {
                return;
            }

            if (activeDirector == null)
            {
                activeDirector = root.GetComponent<PlayableDirector>();
            }

            if (beforeInstance == null || afterInstance == null)
            {
                foreach (Transform child in root.transform)
                {
                    if (beforeInstance == null && child.name.Contains("_Before"))
                    {
                        beforeInstance = child.gameObject;
                    }

                    if (afterInstance == null && child.name.Contains("_After"))
                    {
                        afterInstance = child.gameObject;
                    }
                }
            }
        }

        private static void LogDialoguePreview(MergeStageConfig stage)
        {
            if (dialogueLogged)
            {
                return;
            }

            dialogueLogged = true;

            if (stage == null || !stage.PlayDialogue || stage.DialogueSequence == null)
            {
                return;
            }

            CreateDialogueOverlay(stage);

            foreach (var line in stage.DialogueSequence.Lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    continue;
                }

                Debug.Log($"[Merge-2 Dialogue Preview] {line.speakerName}: {line.text}");
            }
        }

        private static void CreateDialogueOverlay(MergeStageConfig stage)
        {
            var root = GameObject.Find(PlaybackRootName);
            if (root == null)
            {
                return;
            }

            BuildDialogueLineList(stage);
            if (dialogueLines.Count == 0)
            {
                return;
            }

            if (TryCreateDialogueListOverlay(stage))
            {
                ignoreDialogueClicksUntil = EditorApplication.timeSinceStartup + 0.2d;
                mouseWasPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
                SceneView.duringSceneGui -= HandleSceneViewClick;
                SceneView.duringSceneGui += HandleSceneViewClick;
                EditorApplication.update -= PollDialogueClick;
                EditorApplication.update += PollDialogueClick;
                RegisterGameViewClicks();
                return;
            }

            if (dialogueOverlay != null)
            {
                Object.DestroyImmediate(dialogueOverlay);
            }

            currentDialogueIndex = 0;
            dialogueOverlay = new GameObject("Dialogue Overlay");
            dialogueOverlay.transform.SetParent(root.transform, false);
            dialogueOverlay.transform.localPosition = new Vector3(0f, -2.32f, -0.2f);

            AddOverlayBlock(dialogueOverlay.transform, "Dialogue Box", new Color32(20, 24, 30, 228), new Vector3(0f, 0f, 0f), new Vector3(3.05f, 0.68f, 1f));
            AddOverlayBlock(dialogueOverlay.transform, "Name Plate", new Color32(238, 184, 76, 255), new Vector3(-1.05f, 0.36f, -0.01f), new Vector3(0.78f, 0.2f, 1f));
            speakerText = AddOverlayText(dialogueOverlay.transform, "Speaker", string.Empty, new Vector3(-1.05f, 0.36f, -0.03f), 0.046f, Color.black);
            lineText = AddOverlayText(dialogueOverlay.transform, "Line", string.Empty, new Vector3(0f, -0.06f, -0.03f), 0.044f, Color.white);
            AddOverlayText(dialogueOverlay.transform, "Next Hint", "Tap anywhere", new Vector3(0.98f, -0.34f, -0.03f), 0.026f, new Color32(190, 198, 205, 255));
            ShowDialogueLine(0);
            ignoreDialogueClicksUntil = EditorApplication.timeSinceStartup + 0.2d;
            mouseWasPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            SceneView.duringSceneGui -= HandleSceneViewClick;
            SceneView.duringSceneGui += HandleSceneViewClick;
            EditorApplication.update -= PollDialogueClick;
            EditorApplication.update += PollDialogueClick;
            RegisterGameViewClicks();
        }

        private static bool TryCreateDialogueListOverlay(MergeStageConfig stage)
        {
            if (activeSceneConfig == null || activeSceneConfig.DialogueInterfacePrefab == null)
            {
                return false;
            }

            var root = GameObject.Find(PlaybackRootName);
            if (root == null)
            {
                return false;
            }

            var canvas = CreateDialogueCanvas(root.transform);
            dialogueUiInstance = InstantiatePrefab(activeSceneConfig.DialogueInterfacePrefab, canvas.transform);
            if (dialogueUiInstance == null)
            {
                return false;
            }

            dialogueOverlay = dialogueUiInstance;
            dialogueUiInstance.SetActive(true);

            var listContent = FindDialogueListContent(dialogueUiInstance.transform);
            if (listContent == null)
            {
                Debug.LogWarning("[Merge-2 Dialogue Preview] Dialogue UI is missing StoryTalkSV/Viewport/Content, so the dialogue list cannot be generated.");
                return true;
            }

            var embeddedTemplate = FindDeepChild(listContent, "SelfTalk");
            var templatePrefab = activeSceneConfig.DefaultDialogueItemPrefab;
            if (templatePrefab == null && embeddedTemplate == null)
            {
                Debug.LogWarning("[Merge-2 Dialogue Preview] No default item prefab is set, and no SelfTalk template was found under the list Content.");
                return true;
            }

            dialogueListContent = listContent;
            embeddedDialogueItemTemplate = embeddedTemplate != null ? embeddedTemplate.gameObject : null;
            dialogueScrollRect = FindDialogueScrollRect(dialogueUiInstance.transform, listContent);
            ConfigureDialogueScrollList(listContent);
            currentDialogueIndex = 0;
            ClearConfiguredDialogueItems();
            ShowConfiguredDialogueLine(currentDialogueIndex);

            return true;
        }

        private static void ShowConfiguredDialogueLine(int index)
        {
            if (dialogueListContent == null || index < 0 || index >= dialogueLines.Count)
            {
                return;
            }

            var line = dialogueLines[index];
            var lineTemplate = line.dialogueItemPrefab != null ? line.dialogueItemPrefab : activeSceneConfig.DefaultDialogueItemPrefab;
            var item = lineTemplate != null
                ? InstantiatePrefab(lineTemplate, dialogueListContent)
                : embeddedDialogueItemTemplate != null
                    ? Object.Instantiate(embeddedDialogueItemTemplate, dialogueListContent)
                    : null;

            if (item == null)
            {
                return;
            }

            item.name = string.IsNullOrWhiteSpace(line.speakerName) ? "Dialogue Item" : $"{line.speakerName} Item";
            item.SetActive(true);
            ApplyDialogueLineToItem(item, line);
            RebuildDialogueUiLayout();
        }

        private static void ClearConfiguredDialogueItems()
        {
            if (dialogueListContent == null)
            {
                return;
            }

            for (var i = dialogueListContent.childCount - 1; i >= 0; i--)
            {
                var child = dialogueListContent.GetChild(i).gameObject;
                if (embeddedDialogueItemTemplate != null && child == embeddedDialogueItemTemplate)
                {
                    child.SetActive(false);
                    continue;
                }

                Object.DestroyImmediate(child);
            }
        }

        private static void RebuildDialogueUiLayout()
        {
            var listContentRect = dialogueListContent as RectTransform;
            if (listContentRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(listContentRect);
                Canvas.ForceUpdateCanvases();

                var preferredHeight = UnityEngine.UI.LayoutUtility.GetPreferredHeight(listContentRect);
                var viewportHeight = dialogueScrollRect != null && dialogueScrollRect.viewport != null
                    ? dialogueScrollRect.viewport.rect.height
                    : 0f;
                var targetHeight = Mathf.Max(preferredHeight, viewportHeight);
                if (targetHeight > 0f)
                {
                    listContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
                }

                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(listContentRect);
            }

            var panelRect = dialogueUiInstance != null ? dialogueUiInstance.GetComponent<RectTransform>() : null;
            if (panelRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }

            Canvas.ForceUpdateCanvases();
            if (dialogueScrollRect != null)
            {
                dialogueScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private static GameObject CreateDialogueCanvas(Transform parent)
        {
            var canvasObject = new GameObject("Dialogue Preview Canvas");
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvasObject;
        }

        private static Transform FindDialogueListContent(Transform root)
        {
            var storyTalkScroll = FindDeepChild(root, "StoryTalkSV");
            var viewport = storyTalkScroll != null ? FindDeepChild(storyTalkScroll, "Viewport") : FindDeepChild(root, "Viewport");
            if (viewport != null)
            {
                var content = viewport.Find("Content");
                if (content != null)
                {
                    return content;
                }
            }

            return FindDeepChild(root, "Content");
        }

        private static UnityEngine.UI.ScrollRect FindDialogueScrollRect(Transform root, Transform listContent)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var scrollRect in root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true))
            {
                if (scrollRect.content == listContent)
                {
                    return scrollRect;
                }
            }

            return listContent != null ? listContent.GetComponentInParent<UnityEngine.UI.ScrollRect>() : null;
        }

        private static void ConfigureDialogueScrollList(Transform listContent)
        {
            if (listContent == null)
            {
                return;
            }

            var contentRect = listContent as RectTransform;
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
            }

            var layout = listContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = listContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = listContent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = listContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }

            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            if (dialogueScrollRect != null)
            {
                dialogueScrollRect.content = contentRect;
                dialogueScrollRect.horizontal = false;
                dialogueScrollRect.vertical = true;
                dialogueScrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            }
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void ApplyDialogueLineToItem(GameObject item, DialogueLine line)
        {
            ApplyTexts(item, line);
            ApplyImageByName(item, line.portrait, "portrait", "icon", "avatar");
            ApplyImageByName(item, line.avatarFrame != null ? line.avatarFrame : activeSceneConfig.DefaultAvatarFrame, "avatarframe", "frame", "head");
            ApplyImageByName(item, line.dialogueBackground != null ? line.dialogueBackground : activeSceneConfig.DefaultDialogueBackground, "dialoguebackground", "background", "bubble", "content");
            ApplyTalkItemContentImage(item, line.dialogueBackground != null
                ? line.dialogueBackground
                : activeSceneConfig.DefaultDialogueBackground);

            var itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(itemRect);
            }
        }

        private static void ApplyTexts(GameObject item, DialogueLine line)
        {
            foreach (var component in item.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !IsTmpText(component))
                {
                    continue;
                }

                var path = GetLowerPath(component.transform, item.transform);
                var value = path.Contains("head") || path.Contains("speaker") || path.Contains("name")
                    ? string.IsNullOrWhiteSpace(line.speakerName) ? "Character" : line.speakerName
                    : line.text;
                SetTextProperty(component, value);
            }

            foreach (var text in item.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                var path = GetLowerPath(text.transform, item.transform);
                text.text = path.Contains("head") || path.Contains("speaker") || path.Contains("name")
                    ? string.IsNullOrWhiteSpace(line.speakerName) ? "Character" : line.speakerName
                    : line.text;
            }
        }

        private static bool IsTmpText(Component component)
        {
            var type = component.GetType();
            while (type != null)
            {
                if (type.FullName == "TMPro.TMP_Text")
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static void SetTextProperty(Component component, string value)
        {
            var property = component.GetType().GetProperty("text");
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value, null);
            }
        }

        private static void ApplyImageByName(GameObject item, Sprite sprite, params string[] nameKeywords)
        {
            if (sprite == null)
            {
                return;
            }

            foreach (var image in item.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                var path = GetLowerPath(image.transform, item.transform);
                foreach (var keyword in nameKeywords)
                {
                    if (path.Contains(keyword))
                    {
                        image.sprite = sprite;
                        image.type = UnityEngine.UI.Image.Type.Sliced;
                        return;
                    }
                }
            }
        }

        private static void ApplyTalkItemContentImage(GameObject item, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            UnityEngine.UI.Image fallback = null;
            foreach (var image in item.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.transform.name != "Content")
                {
                    continue;
                }

                fallback ??= image;
                if (!HasTextDescendant(image.transform))
                {
                    continue;
                }

                ApplyImageSprite(image, sprite);
                return;
            }

            if (fallback != null)
            {
                ApplyImageSprite(fallback, sprite);
            }
        }

        private static bool HasTextDescendant(Transform root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                if (component is UnityEngine.UI.Text || IsTmpText(component))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyImageSprite(UnityEngine.UI.Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static string GetLowerPath(Transform transform, Transform root)
        {
            var names = new List<string>();
            var current = transform;
            while (current != null && current != root.parent)
            {
                names.Add(current.name.ToLowerInvariant());
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static void BuildDialogueLineList(MergeStageConfig stage)
        {
            dialogueLines.Clear();
            foreach (var line in stage.DialogueSequence.Lines)
            {
                if (line != null && !string.IsNullOrWhiteSpace(line.text))
                {
                    dialogueLines.Add(line);
                }
            }
        }

        private static void HandleSceneViewClick(SceneView sceneView)
        {
            var current = Event.current;
            if (current == null || current.type != EventType.MouseDown || current.button != 0 || dialogueOverlay == null)
            {
                return;
            }

            AdvanceDialogue();
            current.Use();
        }

        private static void PollDialogueClick()
        {
            if (dialogueOverlay == null)
            {
                EditorApplication.update -= PollDialogueClick;
                return;
            }

            if (EditorApplication.timeSinceStartup < ignoreDialogueClicksUntil)
            {
                return;
            }

            if (Mouse.current == null)
            {
                mouseWasPressed = false;
                return;
            }

            var mouseIsPressed = Mouse.current.leftButton.isPressed;
            if (mouseIsPressed && !mouseWasPressed)
            {
                mouseWasPressed = true;
                AdvanceDialogue();
                return;
            }

            mouseWasPressed = mouseIsPressed;
        }

        private static void AdvanceDialogue()
        {
            currentDialogueIndex++;
            if (currentDialogueIndex >= dialogueLines.Count)
            {
                CloseDialogueOverlay();
                return;
            }

            if (dialogueUiInstance != null)
            {
                ShowConfiguredDialogueLine(currentDialogueIndex);
            }
            else
            {
                ShowDialogueLine(currentDialogueIndex);
            }
        }

        private static void CloseDialogueOverlay()
        {
            if (dialogueUiInstance != null)
            {
                var canvasObject = dialogueUiInstance.transform.parent != null ? dialogueUiInstance.transform.parent.gameObject : dialogueUiInstance;
                Object.DestroyImmediate(canvasObject);
            }
            else if (dialogueOverlay != null)
            {
                Object.DestroyImmediate(dialogueOverlay);
            }

            dialogueOverlay = null;
            dialogueUiInstance = null;
            dialogueListContent = null;
            embeddedDialogueItemTemplate = null;
            dialogueScrollRect = null;
            speakerText = null;
            lineText = null;
            SceneView.duringSceneGui -= HandleSceneViewClick;
            EditorApplication.update -= PollDialogueClick;
            UnregisterGameViewClicks();
        }

        private static void RegisterGameViewClicks()
        {
            UnregisterGameViewClicks();

            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                return;
            }

            foreach (var gameViewObject in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                var gameView = gameViewObject as EditorWindow;
                if (gameView == null)
                {
                    continue;
                }

                gameView.rootVisualElement.RegisterCallback<MouseDownEvent>(HandleGameViewMouseDown, TrickleDown.TrickleDown);
                registeredGameViews.Add(gameView);
            }
        }

        private static void UnregisterGameViewClicks()
        {
            foreach (var gameView in registeredGameViews)
            {
                if (gameView == null)
                {
                    continue;
                }

                gameView.rootVisualElement.UnregisterCallback<MouseDownEvent>(HandleGameViewMouseDown, TrickleDown.TrickleDown);
            }

            registeredGameViews.Clear();
        }

        private static void HandleGameViewMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0 || dialogueOverlay == null || EditorApplication.timeSinceStartup < ignoreDialogueClicksUntil)
            {
                return;
            }

            AdvanceDialogue();
            evt.StopPropagation();
        }

        private static void ShowDialogueLine(int index)
        {
            if (index < 0 || index >= dialogueLines.Count || speakerText == null || lineText == null)
            {
                return;
            }

            var line = dialogueLines[index];
            speakerText.text = string.IsNullOrWhiteSpace(line.speakerName) ? "Character" : line.speakerName;
            lineText.text = WrapDialogueText(line.text, 14);
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
                Object.DestroyImmediate(collider);
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
