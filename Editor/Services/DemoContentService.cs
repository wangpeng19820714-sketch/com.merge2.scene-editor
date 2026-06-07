using System;
using System.IO;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Merge2.SceneEditor.Editor
{
    public static class DemoContentService
    {
        private const string SceneId = "SC_RestaurantInterior";
        private const string SceneName = "Food Truck Interior";

        [MenuItem("Tools/Merge-2/Create Restaurant Demo")]
        public static void CreateRestaurantDemoFromMenu()
        {
            var sceneFolder = $"{MergeSceneAssetService.ContentRoot}/{SceneId}";
            if (AssetDatabase.IsValidFolder(sceneFolder)
                && !EditorUtility.DisplayDialog("Generate Demo", "SC_RestaurantInterior already exists. Replace this demo content?", "Replace", "Cancel"))
            {
                return;
            }

            CreateRestaurantDemo(true);
        }

        public static MergeSceneConfig CreateRestaurantDemo(bool replaceExisting)
        {
            EditorStagePlaybackService.Stop();
            EditorPreviewService.ClearPreview();

            var sceneFolder = $"{MergeSceneAssetService.ContentRoot}/{SceneId}";
            if (replaceExisting && AssetDatabase.IsValidFolder(sceneFolder))
            {
                AssetDatabase.DeleteAsset(sceneFolder);
            }

            sceneFolder = MergeSceneAssetService.EnsureSceneFolders(SceneId);
            EnsureFolder(sceneFolder, "Scenes");
            EnsureFolder(sceneFolder, "Art");
            EnsureFolder(sceneFolder, "UI");

            var square = CreateSprite($"{sceneFolder}/Art/Square.png", new Color32(255, 255, 255, 255));
            var avatarFrame = CreateSprite($"{sceneFolder}/Art/DialogueAvatarFrame.png", new Color32(238, 184, 76, 255));
            var dialogueBackground = CreateSprite($"{sceneFolder}/Art/DialogueBubbleBackground.png", new Color32(24, 30, 38, 238));
            var sceneConfig = MergeSceneAssetService.CreateSceneConfig(SceneId, SceneName);
            var sceneConfigPath = AssetDatabase.GetAssetPath(sceneConfig);
            sceneConfig.Description = "Merge-2 food truck repair story demo: stove, sink, and signboard stages.";
            var dialogueItemPrefab = CreateDialogueUiPrefabs(sceneFolder, square, avatarFrame, dialogueBackground, sceneConfig);

            var stoveBefore = CreateRepairPrefab(sceneFolder, square, "PF_Stove_Before", "STOVE OLD", new Color32(96, 63, 45, 255), new Color32(170, 62, 50, 255), true);
            var stoveAfter = CreateRepairPrefab(sceneFolder, square, "PF_Stove_After", "STOVE NEW", new Color32(82, 118, 92, 255), new Color32(245, 189, 73, 255), false);
            var sinkBefore = CreateRepairPrefab(sceneFolder, square, "PF_Sink_Before", "SINK OLD", new Color32(45, 77, 92, 255), new Color32(62, 160, 190, 255), true);
            var sinkAfter = CreateRepairPrefab(sceneFolder, square, "PF_Sink_After", "SINK NEW", new Color32(64, 111, 128, 255), new Color32(167, 225, 236, 255), false);
            var signBefore = CreateRepairPrefab(sceneFolder, square, "PF_Signboard_Before", "SIGN OLD", new Color32(82, 69, 60, 255), new Color32(147, 97, 57, 255), true);
            var signAfter = CreateRepairPrefab(sceneFolder, square, "PF_Signboard_After", "SIGN NEW", new Color32(80, 70, 112, 255), new Color32(239, 114, 77, 255), false);

            var stage01 = MergeSceneAssetService.CreateStage(sceneConfig);
            ConfigureStage(stage01, "Stage_01_RepairStove", "Stage 01: Repair Stove", "The stove is broken. Guide the player through the first repair.", stoveBefore, stoveAfter, StageValidationState.Approved);
            AddDialogue(stage01.DialogueSequence,
                ("Amy", "Amy", "Oh no! This stove looks like it has been broken for a long time."),
                ("Tom", "Tom", "Do not worry. Once we repair the stove, the truck can cook again."),
                ("Amy", "Amy", "Great! After this, we can make more delicious food."));
            ApplyDialogueStyle(stage01.DialogueSequence, dialogueItemPrefab, avatarFrame, dialogueBackground);

            var stage02 = MergeSceneAssetService.CreateStage(sceneConfig);
            ConfigureStage(stage02, "Stage_02_RepairSink", "Stage 02: Repair Sink", "The sink is leaking. Switch between before and after states for review.", sinkBefore, sinkAfter, StageValidationState.Pending);
            AddDialogue(stage02.DialogueSequence,
                ("Amy", "Amy", "The sink is still dripping, and the floor is almost soaked."),
                ("Tom", "Tom", "I will replace the old pipe. Please get the cleaning tools ready."),
                ("Amy", "Amy", "The water flows smoothly now. The kitchen finally looks right."));
            ApplyDialogueStyle(stage02.DialogueSequence, dialogueItemPrefab, avatarFrame, dialogueBackground);

            var stage03 = MergeSceneAssetService.DuplicateStage(sceneConfig, stage01);
            ConfigureStage(stage03, "Stage_03_RepairSignboard", "Stage 03: Repair Signboard", "Created from a duplicated stage, then updated with new assets and dialogue.", signBefore, signAfter, StageValidationState.Locked);
            AddDialogue(stage03.DialogueSequence,
                ("Amy", "Amy", "Last comes the signboard. Without it, customers cannot find us."),
                ("Tom", "Tom", "I will reconnect the lights and paint it with a brighter color."),
                ("Amy", "Amy", "The sign is glowing. The food truck is officially open again."));
            ApplyDialogueStyle(stage03.DialogueSequence, dialogueItemPrefab, avatarFrame, dialogueBackground);

            CreateValidationImage(sceneConfig, stage01, new Color32(82, 118, 92, 255));
            CreateValidationImage(sceneConfig, stage02, new Color32(64, 111, 128, 255));
            CreateValidationImage(sceneConfig, stage03, new Color32(80, 70, 112, 255));

            EditorUtility.SetDirty(sceneConfig);
            AssetDatabase.SaveAssets();
            CreateDemoScene(sceneFolder, sceneConfig, stoveBefore, stoveAfter, sinkBefore, sinkAfter, signBefore, signAfter);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var reloadedSceneConfig = AssetDatabase.LoadAssetAtPath<MergeSceneConfig>(sceneConfigPath);
            Selection.activeObject = reloadedSceneConfig;
            return reloadedSceneConfig;
        }

        private static void ConfigureStage(
            MergeStageConfig stage,
            string stageId,
            string stageName,
            string description,
            GameObject beforePrefab,
            GameObject afterPrefab,
            StageValidationState state)
        {
            Undo.RecordObject(stage, "Configure Demo Stage");
            stage.StageId = stageId;
            stage.StageName = stageName;
            stage.StageDescription = description;
            stage.BeforePrefab = beforePrefab;
            stage.AfterPrefab = afterPrefab;
            stage.CanSkip = state != StageValidationState.Locked;
            stage.PlayDialogue = true;
            stage.PlayCameraTimeline = true;
            stage.DefaultPreviewState = StagePreviewState.Before;
            stage.ValidationState = state;
            stage.Locked = state == StageValidationState.Locked;
            stage.AcceptanceDescription = state switch
            {
                StageValidationState.Approved => "Demo acceptance: before/after states, Timeline, and dialogue are configured.",
                StageValidationState.Pending => "Demo acceptance: review sink animation and dialogue pacing.",
                StageValidationState.Locked => "Demo acceptance: locked after duplication to demonstrate edit protection.",
                _ => "Demo acceptance."
            };
            stage.LastModifiedBy = Environment.UserName;
            stage.LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            EditorUtility.SetDirty(stage);
        }

        private static void AddDialogue(DialogueSequenceConfig dialogue, params (string speakerId, string speakerName, string text)[] lines)
        {
            if (dialogue == null)
            {
                return;
            }

            Undo.RecordObject(dialogue, "Configure Demo Dialogue");
            dialogue.Lines.Clear();
            foreach (var line in lines)
            {
                dialogue.Lines.Add(new DialogueLine
                {
                    speakerId = line.speakerId,
                    speakerName = line.speakerName,
                    emotion = "Default",
                    text = line.text,
                    voiceKey = $"{dialogue.DialogueId}_{dialogue.Lines.Count + 1:00}",
                    typewriterSpeed = 28f,
                    autoWaitTime = 1.25f,
                    waitForClick = true
                });
            }

            EditorUtility.SetDirty(dialogue);
        }

        private static void ApplyDialogueStyle(DialogueSequenceConfig dialogue, GameObject itemPrefab, Sprite avatarFrame, Sprite dialogueBackground)
        {
            if (dialogue == null)
            {
                return;
            }

            Undo.RecordObject(dialogue, "Configure Demo Dialogue Style");
            foreach (var line in dialogue.Lines)
            {
                if (line == null)
                {
                    continue;
                }

                line.dialogueItemPrefab = itemPrefab;
                line.avatarFrame = avatarFrame;
                line.dialogueBackground = dialogueBackground;
                line.talkItemContentImage = null;
            }

            EditorUtility.SetDirty(dialogue);
        }

        private static GameObject CreateDialogueUiPrefabs(string sceneFolder, Sprite square, Sprite avatarFrame, Sprite dialogueBackground, MergeSceneConfig sceneConfig)
        {
            var sourcePanel = GameObject.Find("Canvas/StoryTalkPanel");
            var sourceItem = GameObject.Find("Canvas/StoryTalkPanel/StoryTalkSV/Viewport/Content/SelfTalk");
            GameObject panelPrefab;
            GameObject itemPrefab;

            if (sourcePanel != null && sourceItem != null)
            {
                panelPrefab = PrefabUtility.SaveAsPrefabAsset(sourcePanel, $"{sceneFolder}/UI/PF_{SceneId}_StoryTalkPanel.prefab");
                itemPrefab = PrefabUtility.SaveAsPrefabAsset(sourceItem, $"{sceneFolder}/UI/PF_{SceneId}_SelfTalkItem.prefab");
            }
            else
            {
                var created = CreateFallbackStoryTalkPanel(square);
                sourcePanel = created.panel;
                sourceItem = created.item;
                panelPrefab = PrefabUtility.SaveAsPrefabAsset(sourcePanel, $"{sceneFolder}/UI/PF_{SceneId}_StoryTalkPanel.prefab");
                itemPrefab = PrefabUtility.SaveAsPrefabAsset(sourceItem, $"{sceneFolder}/UI/PF_{SceneId}_SelfTalkItem.prefab");
                UnityEngine.Object.DestroyImmediate(sourcePanel);
            }

            sceneConfig.DialogueInterfacePrefab = panelPrefab;
            sceneConfig.DefaultDialogueItemPrefab = itemPrefab;
            sceneConfig.DefaultAvatarFrame = avatarFrame;
            sceneConfig.DefaultDialogueBackground = dialogueBackground;
            EditorUtility.SetDirty(sceneConfig);
            return itemPrefab;
        }

        private static (GameObject panel, GameObject item) CreateFallbackStoryTalkPanel(Sprite square)
        {
            var panel = new GameObject("StoryTalkPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(0f, 520f);
            panel.GetComponent<Image>().color = new Color32(0, 0, 0, 0);

            var scroll = new GameObject("StoryTalkSV", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(panel.transform, false);
            var scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scroll.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color32(0, 0, 0, 0);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 250f);
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.LowerCenter;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollComponent = scroll.GetComponent<ScrollRect>();
            scrollComponent.viewport = viewportRect;
            scrollComponent.content = contentRect;
            scrollComponent.horizontal = false;

            var item = CreateFallbackSelfTalkItem(content.transform, square);
            return (panel, item);
        }

        private static GameObject CreateFallbackSelfTalkItem(Transform parent, Sprite square)
        {
            var item = new GameObject("SelfTalk", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            item.transform.SetParent(parent, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(1046f, 250f);
            itemRect.pivot = new Vector2(0.5f, 0f);
            var itemLayout = item.GetComponent<HorizontalLayoutGroup>();
            itemLayout.padding = new RectOffset(23, 23, 25, 25);
            itemLayout.spacing = 11f;
            itemLayout.childAlignment = TextAnchor.LowerLeft;
            itemLayout.childControlWidth = false;
            itemLayout.childControlHeight = false;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = false;
            item.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var head = new GameObject("Head", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            head.transform.SetParent(item.transform, false);
            var headRect = head.GetComponent<RectTransform>();
            headRect.sizeDelta = new Vector2(200f, 200f);
            head.GetComponent<Image>().sprite = square;
            head.GetComponent<Image>().color = new Color32(238, 184, 76, 255);
            var headLayout = head.GetComponent<LayoutElement>();
            headLayout.preferredWidth = 200f;
            headLayout.preferredHeight = 200f;

            CreateLegacyText("Text", head.transform, "Name", 32, Color.black, new Vector2(200f, 52f), new Vector2(0f, -74f));

            var bubble = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            bubble.transform.SetParent(item.transform, false);
            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.sizeDelta = new Vector2(800f, 80f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);
            bubble.GetComponent<Image>().sprite = square;
            bubble.GetComponent<Image>().color = new Color32(24, 30, 38, 238);
            var bubbleLayout = bubble.GetComponent<VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(24, 24, 14, 14);
            bubbleLayout.childAlignment = TextAnchor.MiddleLeft;
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;
            bubble.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubble.GetComponent<LayoutElement>().preferredWidth = 800f;

            CreateLegacyText("Text", bubble.transform, "New Text", 34, Color.white, new Vector2(752f, 50f), Vector2.zero);
            return item;
        }

        private static Text CreateLegacyText(string name, Transform parent, string text, int fontSize, Color color, Vector2 size, Vector2 anchoredPosition)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Sprite CreateSprite(string path, Color32 color)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color32[32 * 32];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite loadedSprite)
                {
                    return loadedSprite;
                }
            }

            return null;
        }

        private static GameObject CreateRepairPrefab(string sceneFolder, Sprite square, string prefabName, string title, Color32 baseColor, Color32 accentColor, bool damaged)
        {
            var root = new GameObject(prefabName);
            AddBlock(root.transform, square, "Back Plate", baseColor, new Vector3(0f, 0f, 0f), new Vector3(2.2f, 1.35f, 1f));
            AddBlock(root.transform, square, "Accent", accentColor, new Vector3(0f, -0.08f, -0.01f), new Vector3(1.68f, 0.72f, 1f));
            AddBlock(root.transform, square, "Counter", new Color32(230, 221, 202, 255), new Vector3(0f, -0.78f, -0.02f), new Vector3(2.45f, 0.2f, 1f));

            if (damaged)
            {
                AddBlock(root.transform, square, "Crack A", new Color32(38, 33, 31, 255), new Vector3(-0.42f, 0.16f, -0.03f), new Vector3(0.07f, 0.58f, 1f), 18f);
                AddBlock(root.transform, square, "Crack B", new Color32(38, 33, 31, 255), new Vector3(0.5f, -0.08f, -0.03f), new Vector3(0.07f, 0.48f, 1f), -24f);
                AddBlock(root.transform, square, "Stain", new Color32(64, 45, 35, 210), new Vector3(0.65f, 0.27f, -0.04f), new Vector3(0.42f, 0.24f, 1f));
            }
            else
            {
                AddBlock(root.transform, square, "Sparkle A", new Color32(255, 255, 214, 255), new Vector3(-0.68f, 0.38f, -0.04f), new Vector3(0.1f, 0.4f, 1f), 45f);
                AddBlock(root.transform, square, "Sparkle B", new Color32(255, 255, 214, 255), new Vector3(0.76f, 0.3f, -0.04f), new Vector3(0.1f, 0.36f, 1f), -45f);
            }

            AddLabel(root.transform, title, new Vector3(0f, 0.92f, -0.05f));
            var prefabPath = $"{sceneFolder}/Prefabs/{prefabName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddBlock(Transform parent, Sprite sprite, string name, Color color, Vector3 localPosition, Vector3 localScale, float rotationZ = 0f)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
        }

        private static void AddLabel(Transform parent, string text, Vector3 localPosition)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.075f;
            mesh.fontSize = 32;
            mesh.color = Color.white;
        }

        private static void CreateValidationImage(MergeSceneConfig sceneConfig, MergeStageConfig stage, Color32 color)
        {
            var path = MergeSceneAssetService.GetValidationScreenshotPath(sceneConfig, stage);
            var texture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            var pixels = new Color32[320 * 180];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            stage.AcceptanceScreenshotPath = path;
            EditorUtility.SetDirty(stage);
        }

        private static void CreateDemoScene(
            string sceneFolder,
            MergeSceneConfig sceneConfig,
            GameObject stoveBefore,
            GameObject stoveAfter,
            GameObject sinkBefore,
            GameObject sinkAfter,
            GameObject signBefore,
            GameObject signAfter)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DemoRestaurantScene";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(36, 42, 48, 255);

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObject.AddComponent<Light>().type = LightType.Directional;

            var root = new GameObject("Restaurant Runtime Root");
            var stageAnchor = new GameObject("Stage Anchor");
            stageAnchor.transform.SetParent(root.transform, false);
            stageAnchor.transform.localPosition = Vector3.zero;

            var playerRoot = new GameObject("Merge Scene Player");
            var scenePlayer = playerRoot.AddComponent<MergeScenePlayer>();
            var stagePlayerObject = new GameObject("Stage Player");
            stagePlayerObject.transform.SetParent(playerRoot.transform, false);
            var stagePlayer = stagePlayerObject.AddComponent<MergeStagePlayer>();
            var director = stagePlayerObject.AddComponent<PlayableDirector>();
            var dialoguePlayer = stagePlayerObject.AddComponent<DialoguePlayer>();
            SetObjectField(scenePlayer, "sceneConfig", sceneConfig);
            SetObjectField(scenePlayer, "stagePlayer", stagePlayer);
            SetObjectField(stagePlayer, "playableDirector", director);
            SetObjectField(stagePlayer, "dialoguePlayer", dialoguePlayer);

            var scenePath = $"{sceneFolder}/Scenes/DemoRestaurantScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void SetObjectField(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
