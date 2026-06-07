using System.Collections.Generic;
using Merge2.SceneEditor.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Merge2.SceneEditor.Editor
{
    public sealed class MergeSceneEditorWindow : EditorWindow
    {
        private readonly List<ValidationResult> validationResults = new();
        private MergeSceneConfig selectedScene;
        private MergeStageConfig selectedStage;
        private ScrollView sceneList;
        private ScrollView stageList;
        private VisualElement inspectorPanel;
        private VisualElement validationPanel;
        private ObjectField sceneObjectField;

        [MenuItem("Tools/Merge-2/Merge Scene Editor")]
        public static void Open()
        {
            var window = GetWindow<MergeSceneEditorWindow>();
            window.titleContent = new GUIContent("Merge Scene Editor");
            window.minSize = new Vector2(980, 560);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("merge-scene-editor-root");

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.merge2.scene-editor/Editor/USS/MergeSceneEditor.uss");
            if (stylesheet != null)
            {
                rootVisualElement.styleSheets.Add(stylesheet);
            }

            BuildToolbar();
            BuildMainLayout();
            RefreshSceneList();
            RefreshStageList();
            RefreshInspector();
            RefreshValidationPanel();
        }

        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new Button(CreateNewScene) { text = "New Scene" });
            toolbar.Add(new Button(CreateStage) { text = "New Stage" });
            toolbar.Add(new Button(DuplicateSelectedStage) { text = "Duplicate Stage" });
            toolbar.Add(new Button(GenerateDemo) { text = "Generate Demo" });
            toolbar.Add(new Button(DeleteSelectedStage) { text = "Delete Stage" });
            toolbar.Add(new Button(MoveStageUp) { text = "Move Up" });
            toolbar.Add(new Button(MoveStageDown) { text = "Move Down" });
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new Button(RunValidation) { text = "Validate" });
            toolbar.Add(new Button(() => EditorStagePlaybackService.PlayStage(selectedScene, selectedStage)) { text = "Play Stage" });
            toolbar.Add(new Button(EditorStagePlaybackService.Complete) { text = "Complete Stage" });
            toolbar.Add(new Button(EditorStagePlaybackService.Stop) { text = "Stop" });
            toolbar.Add(new Button(() => EditorPreviewService.PreviewStage(selectedStage, StagePreviewState.Before)) { text = "Preview Before" });
            toolbar.Add(new Button(() => EditorPreviewService.PreviewStage(selectedStage, StagePreviewState.After)) { text = "Preview After" });
            toolbar.Add(new Button(ClearAllPreviewObjects) { text = "Clear Preview" });
            toolbar.Add(new Button(SaveAssets) { text = "Save" });

            rootVisualElement.Add(toolbar);
        }

        private void BuildMainLayout()
        {
            var main = new VisualElement();
            main.AddToClassList("main-layout");
            rootVisualElement.Add(main);

            var left = new VisualElement();
            left.AddToClassList("left-panel");
            main.Add(left);

            var center = new VisualElement();
            center.AddToClassList("center-panel");
            main.Add(center);

            var right = new VisualElement();
            right.AddToClassList("right-panel");
            main.Add(right);

            sceneObjectField = new ObjectField("Scene Config")
            {
                objectType = typeof(MergeSceneConfig),
                allowSceneObjects = false
            };
            sceneObjectField.RegisterValueChangedCallback(evt =>
            {
                selectedScene = evt.newValue as MergeSceneConfig;
                selectedStage = null;
                RefreshStageList();
                RefreshInspector();
            });
            left.Add(sceneObjectField);

            left.Add(CreateTitle("Scenes"));
            sceneList = new ScrollView();
            sceneList.style.flexGrow = 1;
            left.Add(sceneList);

            center.Add(CreateTitle("Stages"));
            stageList = new ScrollView();
            stageList.style.flexGrow = 1;
            center.Add(stageList);

            center.Add(CreateTitle("Validation"));
            validationPanel = new VisualElement();
            center.Add(validationPanel);

            right.Add(CreateTitle("Inspector"));
            inspectorPanel = new ScrollView();
            inspectorPanel.style.flexGrow = 1;
            right.Add(inspectorPanel);
        }

        private static Label CreateTitle(string text)
        {
            var label = new Label(text);
            label.AddToClassList("panel-title");
            return label;
        }

        private void RefreshSceneList()
        {
            sceneList?.Clear();
            var guids = AssetDatabase.FindAssets("t:MergeSceneConfig");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sceneConfig = AssetDatabase.LoadAssetAtPath<MergeSceneConfig>(path);
                if (sceneConfig == null)
                {
                    continue;
                }

                var button = new Button(() =>
                {
                    selectedScene = sceneConfig;
                    selectedStage = null;
                    sceneObjectField.SetValueWithoutNotify(selectedScene);
                    RefreshSceneList();
                    RefreshStageList();
                    RefreshInspector();
                })
                {
                    text = string.IsNullOrWhiteSpace(sceneConfig.SceneName) ? sceneConfig.name : sceneConfig.SceneName
                };

                button.AddToClassList("list-button");
                if (sceneConfig == selectedScene)
                {
                    button.AddToClassList("selected-list-button");
                }

                sceneList.Add(button);
            }
        }

        private void RefreshStageList()
        {
            stageList?.Clear();

            if (selectedScene == null)
            {
                stageList?.Add(new Label("Select or create a scene config."));
                return;
            }

            for (var i = 0; i < selectedScene.Stages.Count; i++)
            {
                var stage = selectedScene.Stages[i];
                var index = i;
                var stageLabel = stage == null ? $"Missing Stage {i + 1}" : $"{i + 1:00}. {stage.StageName}";
                var button = new Button(() =>
                {
                    selectedStage = selectedScene.Stages[index];
                    RefreshStageList();
                    RefreshInspector();
                })
                {
                    text = stageLabel
                };

                button.AddToClassList("list-button");
                if (stage == selectedStage)
                {
                    button.AddToClassList("selected-list-button");
                }

                stageList.Add(button);
            }
        }

        private void RefreshInspector()
        {
            inspectorPanel?.Clear();

            if (selectedStage != null)
            {
                if (selectedStage.Locked)
                {
                    inspectorPanel.Add(new HelpBox("This stage is locked. Mark it Pending before editing key settings.", HelpBoxMessageType.Info));
                }

                var stageInspector = new InspectorElement(new SerializedObject(selectedStage));
                stageInspector.SetEnabled(!selectedStage.Locked);
                inspectorPanel.Add(stageInspector);
                AddTimelineTools(inspectorPanel, selectedStage);
                AddAcceptanceTools(inspectorPanel, selectedStage);
                AddDialogueEditor(inspectorPanel, selectedStage);
                return;
            }

            if (selectedScene != null)
            {
                inspectorPanel.Add(new InspectorElement(new SerializedObject(selectedScene)));
                return;
            }

            inspectorPanel?.Add(new Label("No config selected."));
        }

        private void AddDialogueEditor(VisualElement parent, MergeStageConfig stage)
        {
            AddDialogueUiSettings(parent);
            parent.Add(CreateTitle("Dialogue"));

            if (stage.DialogueSequence == null)
            {
                parent.Add(new HelpBox("This stage has no DialogueSequenceConfig assigned.", HelpBoxMessageType.Warning));
                var createButton = new Button(() =>
                {
                    CreateDialogueSequence(stage);
                    RefreshInspector();
                })
                {
                    text = "Create Dialogue Config"
                };
                createButton.SetEnabled(!stage.Locked);
                parent.Add(createButton);
                return;
            }

            var dialogue = stage.DialogueSequence;
            var dialogueRoot = new VisualElement();
            dialogueRoot.AddToClassList("dialogue-editor");
            dialogueRoot.SetEnabled(!stage.Locked);

            var dialogueId = new TextField("Dialogue ID") { value = dialogue.DialogueId };
            dialogueId.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(dialogue, "Modify Dialogue ID");
                dialogue.DialogueId = evt.newValue;
                EditorUtility.SetDirty(dialogue);
            });
            dialogueRoot.Add(dialogueId);

            var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            header.Add(new Button(() => AddDialogueLine(dialogue)) { text = "Add Line" });
            header.Add(new Button(() => EditorStagePlaybackService.PreviewDialogueList(selectedScene, stage)) { text = "Preview Dialogue UI" });
            header.Add(new Button(() =>
            {
                Selection.activeObject = dialogue;
                EditorGUIUtility.PingObject(dialogue);
            })
            {
                text = "Ping Dialogue Asset"
            });
            dialogueRoot.Add(header);

            if (dialogue.Lines.Count == 0)
            {
                dialogueRoot.Add(new HelpBox("No dialogue lines yet. Click Add Line to start.", HelpBoxMessageType.Info));
            }

            for (var i = 0; i < dialogue.Lines.Count; i++)
            {
                AddDialogueLineEditor(dialogueRoot, dialogue, i);
            }

            parent.Add(dialogueRoot);
        }

        private void AddDialogueLineEditor(VisualElement parent, DialogueSequenceConfig dialogue, int index)
        {
            var line = dialogue.Lines[index];
            if (line == null)
            {
                line = new DialogueLine();
                dialogue.Lines[index] = line;
            }

            var lineRoot = new VisualElement();
            lineRoot.AddToClassList("dialogue-line");

            var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var title = new Label($"{index + 1:00}");
            title.AddToClassList("dialogue-line-index");
            titleRow.Add(title);
            titleRow.Add(new Button(() => MoveDialogueLine(dialogue, index, -1)) { text = "Up" });
            titleRow.Add(new Button(() => MoveDialogueLine(dialogue, index, 1)) { text = "Down" });
            titleRow.Add(new Button(() => DuplicateDialogueLine(dialogue, index)) { text = "Copy" });
            titleRow.Add(new Button(() => RemoveDialogueLine(dialogue, index)) { text = "Delete" });
            lineRoot.Add(titleRow);

            var speakerId = new TextField { value = line.speakerId };
            speakerId.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.speakerId = evt.newValue));
            AddDialogueField(lineRoot, "Speaker ID", speakerId);

            var speakerName = new TextField { value = line.speakerName };
            speakerName.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.speakerName = evt.newValue));
            AddDialogueField(lineRoot, "Speaker", speakerName);

            var emotion = new TextField { value = line.emotion };
            emotion.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.emotion = evt.newValue));
            AddDialogueField(lineRoot, "Emotion", emotion);

            var portrait = new ObjectField
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = line.portrait
            };
            portrait.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.portrait = evt.newValue as Sprite));
            AddDialogueField(lineRoot, "Portrait", portrait);

            var itemPrefab = new ObjectField
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = line.dialogueItemPrefab
            };
            itemPrefab.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.dialogueItemPrefab = evt.newValue as GameObject));
            AddDialogueField(lineRoot, "Item Prefab", itemPrefab);

            var avatarFrame = new ObjectField
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = line.avatarFrame
            };
            avatarFrame.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.avatarFrame = evt.newValue as Sprite));
            AddDialogueField(lineRoot, "Avatar Frame", avatarFrame);

            var dialogueBackground = new ObjectField
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = line.dialogueBackground
            };
            dialogueBackground.tooltip = "Sets this line's TalkItem/Content background. Empty uses the scene default background.";
            dialogueBackground.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target =>
            {
                target.dialogueBackground = evt.newValue as Sprite;
                target.talkItemContentImage = null;
            }));
            AddDialogueField(lineRoot, "Bubble BG", dialogueBackground);

            var text = new TextField { value = line.text, multiline = true };
            text.AddToClassList("dialogue-text-field");
            text.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.text = evt.newValue));
            AddDialogueField(lineRoot, "Line Text", text);

            var voiceKey = new TextField { value = line.voiceKey };
            voiceKey.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.voiceKey = evt.newValue));
            AddDialogueField(lineRoot, "Voice Key", voiceKey);

            var typewriterSpeed = new FloatField { value = line.typewriterSpeed };
            typewriterSpeed.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.typewriterSpeed = Mathf.Max(0f, evt.newValue)));
            AddDialogueField(lineRoot, "Type Speed", typewriterSpeed);

            var autoWait = new FloatField { value = line.autoWaitTime };
            autoWait.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.autoWaitTime = Mathf.Max(0f, evt.newValue)));
            AddDialogueField(lineRoot, "Auto Wait", autoWait);

            var waitForClick = new Toggle { value = line.waitForClick };
            waitForClick.RegisterValueChangedCallback(evt => ModifyDialogueLine(dialogue, line, target => target.waitForClick = evt.newValue));
            AddDialogueField(lineRoot, "Wait Click", waitForClick);

            parent.Add(lineRoot);
        }

        private static void AddDialogueField(VisualElement parent, string labelText, VisualElement field)
        {
            var row = new VisualElement();
            row.AddToClassList("dialogue-field-row");

            var label = new Label(labelText);
            label.AddToClassList("dialogue-field-label");

            field.AddToClassList("dialogue-field-input");

            row.Add(label);
            row.Add(field);
            parent.Add(row);
        }

        private void AddDialogueUiSettings(VisualElement parent)
        {
            parent.Add(CreateTitle("Dialogue UI"));

            if (selectedScene == null)
            {
                parent.Add(new HelpBox("Select a scene config before editing dialogue UI settings.", HelpBoxMessageType.Info));
                return;
            }

            var root = new VisualElement();
            root.AddToClassList("dialogue-editor");

            var interfacePrefab = new ObjectField("UI Prefab")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = selectedScene.DialogueInterfacePrefab
            };
            interfacePrefab.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(selectedScene, "Modify Dialogue Interface Prefab");
                selectedScene.DialogueInterfacePrefab = evt.newValue as GameObject;
                EditorUtility.SetDirty(selectedScene);
            });
            root.Add(interfacePrefab);

            var itemPrefab = new ObjectField("Default Item")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = selectedScene.DefaultDialogueItemPrefab
            };
            itemPrefab.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(selectedScene, "Modify Default Dialogue Item Prefab");
                selectedScene.DefaultDialogueItemPrefab = evt.newValue as GameObject;
                EditorUtility.SetDirty(selectedScene);
            });
            root.Add(itemPrefab);

            var styleRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var defaultAvatarFrame = new ObjectField("Default Avatar Frame")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = selectedScene.DefaultAvatarFrame
            };
            defaultAvatarFrame.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(selectedScene, "Modify Default Avatar Frame");
                selectedScene.DefaultAvatarFrame = evt.newValue as Sprite;
                EditorUtility.SetDirty(selectedScene);
            });
            var defaultDialogueBackground = new ObjectField("Default Bubble BG")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = selectedScene.DefaultDialogueBackground
            };
            defaultDialogueBackground.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(selectedScene, "Modify Default Dialogue Background");
                selectedScene.DefaultDialogueBackground = evt.newValue as Sprite;
                EditorUtility.SetDirty(selectedScene);
            });
            styleRow.Add(defaultAvatarFrame);
            styleRow.Add(defaultDialogueBackground);
            root.Add(styleRow);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttonRow.Add(new Button(CreateDialoguePrefabsFromCurrentScene) { text = "Create From StoryTalkPanel" });
            buttonRow.Add(new Button(() => EditorStagePlaybackService.PreviewDialogueList(selectedScene, selectedStage)) { text = "Preview Dialogue List" });
            root.Add(buttonRow);

            parent.Add(root);
        }

        private void AddTimelineTools(VisualElement parent, MergeStageConfig stage)
        {
            parent.Add(CreateTitle("Timeline"));

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Button(() =>
            {
                TimelineAssetService.EnsureTimelines(selectedScene, stage);
                RefreshInspector();
            })
            {
                text = "Ensure Timelines"
            });
            row.Add(new Button(() => TimelineAssetService.OpenTimeline(stage, false)) { text = "Open Repair Timeline" });
            row.Add(new Button(() => TimelineAssetService.OpenTimeline(stage, true)) { text = "Open Camera Timeline" });
            parent.Add(row);

            var rebuildRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            rebuildRow.Add(new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Rebuild Repair Timeline", "Creates a new repair Timeline and replaces the current reference. Existing assets are not deleted.", "Rebuild", "Cancel"))
                {
                    TimelineAssetService.RecreateRepairTimeline(selectedScene, stage);
                    RefreshInspector();
                }
            })
            {
                text = "Rebuild Repair Timeline"
            });
            rebuildRow.Add(new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Rebuild Camera Timeline", "Creates a new camera Timeline and replaces the current reference. Existing assets are not deleted.", "Rebuild", "Cancel"))
                {
                    TimelineAssetService.RecreateCameraTimeline(selectedScene, stage);
                    RefreshInspector();
                }
            })
            {
                text = "Rebuild Camera Timeline"
            });
            parent.Add(rebuildRow);
        }

        private void AddAcceptanceTools(VisualElement parent, MergeStageConfig stage)
        {
            parent.Add(CreateTitle("Acceptance"));

            var stateRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            stateRow.Add(new Button(() => SetValidationState(StageValidationState.Pending)) { text = "Pending" });
            stateRow.Add(new Button(() => SetValidationState(StageValidationState.Approved)) { text = "Approved" });
            stateRow.Add(new Button(() => SetValidationState(StageValidationState.Locked)) { text = "Locked" });
            parent.Add(stateRow);

            var screenshotRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            screenshotRow.Add(new Button(CaptureAcceptanceScreenshot) { text = "Capture Screenshot" });
            screenshotRow.Add(new Button(OpenAcceptanceScreenshot) { text = "Open Screenshot" });
            parent.Add(screenshotRow);

            if (!string.IsNullOrWhiteSpace(stage.AcceptanceScreenshotPath))
            {
                parent.Add(new Label(stage.AcceptanceScreenshotPath));
            }
        }

        private void RefreshValidationPanel()
        {
            validationPanel?.Clear();

            if (validationResults.Count == 0)
            {
                validationPanel?.Add(new Label("Validation has not run yet."));
                return;
            }

            foreach (var result in validationResults)
            {
                var label = new Label($"[{result.Severity}] {result.Message}");
                switch (result.Severity)
                {
                    case ValidationSeverity.Error:
                        label.AddToClassList("validation-error");
                        break;
                    case ValidationSeverity.Warning:
                        label.AddToClassList("validation-warning");
                        break;
                    default:
                        label.AddToClassList("validation-pass");
                        break;
                }

                validationPanel.Add(label);
            }
        }

        private void CreateNewScene()
        {
            var scene = MergeSceneAssetService.CreateSceneConfig("SC_NewScene", "New Scene");
            selectedScene = scene;
            selectedStage = null;
            sceneObjectField.SetValueWithoutNotify(selectedScene);
            RefreshSceneList();
            RefreshStageList();
            RefreshInspector();
        }

        private void CreateStage()
        {
            if (selectedScene == null)
            {
                CreateNewScene();
            }

            selectedStage = MergeSceneAssetService.CreateStage(selectedScene);
            RefreshStageList();
            RefreshInspector();
        }

        private void DeleteSelectedStage()
        {
            if (selectedScene == null || selectedStage == null)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Stage", $"Delete {selectedStage.StageName}? The config asset will remain in the project.", "Delete", "Cancel"))
            {
                return;
            }

            Undo.RecordObject(selectedScene, "Remove Merge Stage");
            selectedScene.Stages.Remove(selectedStage);
            EditorUtility.SetDirty(selectedScene);
            selectedStage = null;
            RefreshStageList();
            RefreshInspector();
        }

        private void DuplicateSelectedStage()
        {
            if (selectedScene == null || selectedStage == null)
            {
                return;
            }

            selectedStage = MergeSceneAssetService.DuplicateStage(selectedScene, selectedStage);
            RefreshStageList();
            RefreshInspector();
        }

        private void GenerateDemo()
        {
            var scene = DemoContentService.CreateRestaurantDemo(true);
            selectedScene = scene;
            selectedStage = selectedScene.Stages.Count > 0 ? selectedScene.Stages[0] : null;
            sceneObjectField.SetValueWithoutNotify(selectedScene);
            RefreshSceneList();
            RefreshStageList();
            RefreshInspector();
            RunValidation();
        }

        private void MoveStageUp()
        {
            MoveSelectedStage(-1);
        }

        private void MoveStageDown()
        {
            MoveSelectedStage(1);
        }

        private void MoveSelectedStage(int direction)
        {
            if (selectedScene == null || selectedStage == null)
            {
                return;
            }

            var index = selectedScene.Stages.IndexOf(selectedStage);
            var targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= selectedScene.Stages.Count)
            {
                return;
            }

            Undo.RecordObject(selectedScene, "Reorder Merge Stage");
            selectedScene.Stages[index] = selectedScene.Stages[targetIndex];
            selectedScene.Stages[targetIndex] = selectedStage;
            EditorUtility.SetDirty(selectedScene);
            RefreshStageList();
        }

        private void RunValidation()
        {
            validationResults.Clear();
            validationResults.AddRange(ValidationService.Validate(selectedScene));
            RefreshValidationPanel();
        }

        private void CreateDialogueSequence(MergeStageConfig stage)
        {
            if (stage == null || stage.Locked)
            {
                return;
            }

            Undo.RecordObject(stage, "Create Dialogue Sequence");
            var sceneId = selectedScene != null ? MergeSceneAssetService.SanitizeIdentifier(selectedScene.SceneId, "SC_NewScene") : "SC_NewScene";
            MergeSceneAssetService.EnsureSceneFolders(sceneId);
            var dialogue = CreateInstance<DialogueSequenceConfig>();
            dialogue.DialogueId = $"DLG_{sceneId}_{stage.StageId}";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{MergeSceneAssetService.ContentRoot}/{sceneId}/Dialogues/{dialogue.DialogueId}.asset");
            AssetDatabase.CreateAsset(dialogue, path);
            stage.DialogueSequence = dialogue;
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
        }

        private void AddDialogueLine(DialogueSequenceConfig dialogue)
        {
            Undo.RecordObject(dialogue, "Add Dialogue Line");
            dialogue.Lines.Add(new DialogueLine
            {
                speakerName = "Character",
                text = "New dialogue line"
            });
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();
            RefreshInspector();
        }

        private void DuplicateDialogueLine(DialogueSequenceConfig dialogue, int index)
        {
            if (dialogue == null || index < 0 || index >= dialogue.Lines.Count)
            {
                return;
            }

            var source = dialogue.Lines[index];
            Undo.RecordObject(dialogue, "Duplicate Dialogue Line");
            dialogue.Lines.Insert(index + 1, new DialogueLine
            {
                speakerId = source.speakerId,
                speakerName = source.speakerName,
                portrait = source.portrait,
                dialogueItemPrefab = source.dialogueItemPrefab,
                avatarFrame = source.avatarFrame,
                dialogueBackground = source.dialogueBackground,
                talkItemContentImage = source.talkItemContentImage,
                emotion = source.emotion,
                text = source.text,
                voiceKey = source.voiceKey,
                typewriterSpeed = source.typewriterSpeed,
                autoWaitTime = source.autoWaitTime,
                waitForClick = source.waitForClick
            });
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();
            RefreshInspector();
        }

        private void RemoveDialogueLine(DialogueSequenceConfig dialogue, int index)
        {
            if (dialogue == null || index < 0 || index >= dialogue.Lines.Count)
            {
                return;
            }

            Undo.RecordObject(dialogue, "Remove Dialogue Line");
            dialogue.Lines.RemoveAt(index);
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();
            RefreshInspector();
        }

        private void MoveDialogueLine(DialogueSequenceConfig dialogue, int index, int direction)
        {
            if (dialogue == null)
            {
                return;
            }

            var targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || index >= dialogue.Lines.Count || targetIndex >= dialogue.Lines.Count)
            {
                return;
            }

            Undo.RecordObject(dialogue, "Move Dialogue Line");
            var line = dialogue.Lines[index];
            dialogue.Lines[index] = dialogue.Lines[targetIndex];
            dialogue.Lines[targetIndex] = line;
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();
            RefreshInspector();
        }

        private static void ModifyDialogueLine(DialogueSequenceConfig dialogue, DialogueLine line, System.Action<DialogueLine> change)
        {
            if (dialogue == null || line == null || change == null)
            {
                return;
            }

            Undo.RecordObject(dialogue, "Modify Dialogue Line");
            change(line);
            EditorUtility.SetDirty(dialogue);
        }

        private void CreateDialoguePrefabsFromCurrentScene()
        {
            if (selectedScene == null)
            {
                return;
            }

            var panel = GameObject.Find("Canvas/StoryTalkPanel");
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Dialogue UI Not Found", "The current scene does not contain Canvas/StoryTalkPanel.", "OK");
                return;
            }

            var selfTalk = GameObject.Find("Canvas/StoryTalkPanel/StoryTalkSV/Viewport/Content/SelfTalk");
            if (selfTalk == null)
            {
                EditorUtility.DisplayDialog("Dialogue Item Not Found", "The current scene does not contain Canvas/StoryTalkPanel/StoryTalkSV/Viewport/Content/SelfTalk.", "OK");
                return;
            }

            var sceneId = MergeSceneAssetService.SanitizeIdentifier(selectedScene.SceneId, "SC_NewScene");
            var sceneFolder = MergeSceneAssetService.EnsureSceneFolders(sceneId);
            EnsureFolder(sceneFolder, "UI");

            var panelPath = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/UI/PF_{sceneId}_StoryTalkPanel.prefab");
            var itemPath = AssetDatabase.GenerateUniqueAssetPath($"{sceneFolder}/UI/PF_{sceneId}_SelfTalkItem.prefab");

            var panelPrefab = PrefabUtility.SaveAsPrefabAsset(panel, panelPath);
            var itemPrefab = PrefabUtility.SaveAsPrefabAsset(selfTalk, itemPath);

            Undo.RecordObject(selectedScene, "Create Dialogue UI Prefabs");
            selectedScene.DialogueInterfacePrefab = panelPrefab;
            selectedScene.DefaultDialogueItemPrefab = itemPrefab;
            EditorUtility.SetDirty(selectedScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshInspector();
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            var path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private void SetValidationState(StageValidationState state)
        {
            AcceptanceService.SetValidationState(selectedStage, state);
            RefreshStageList();
            RefreshInspector();
        }

        private void CaptureAcceptanceScreenshot()
        {
            if (selectedScene == null || selectedStage == null)
            {
                return;
            }

            if (AcceptanceService.CaptureSceneViewScreenshot(selectedScene, selectedStage, out var path))
            {
                EditorUtility.DisplayDialog("Acceptance Screenshot", $"Saved: {path}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Screenshot Failed", "No Scene View camera is available. Open the Scene view first.", "OK");
            }

            RefreshInspector();
        }

        private void OpenAcceptanceScreenshot()
        {
            if (selectedStage == null || string.IsNullOrWhiteSpace(selectedStage.AcceptanceScreenshotPath))
            {
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(selectedStage.AcceptanceScreenshotPath);
            if (texture != null)
            {
                Selection.activeObject = texture;
                AssetDatabase.OpenAsset(texture);
            }
        }

        private static void SaveAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ClearAllPreviewObjects()
        {
            EditorStagePlaybackService.Stop();
            EditorPreviewService.ClearPreview();
        }
    }
}
