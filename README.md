# Merge-2 Scene Editor

Editor and runtime preview framework for Merge-2 repair story scenes.

Open the editor from:

`Tools/Merge-2/Merge Scene Editor`

## Installation

Install as a Unity Package Manager Git dependency:

```json
{
  "dependencies": {
    "com.merge2.scene-editor": "https://github.com/<owner>/<repo>.git"
  }
}
```

Or embed it under a Unity project's `Packages/com.merge2.scene-editor` folder.

## Features

- ScriptableObject scene, stage, repair target, and dialogue data models.
- Merge scene editor window for stage ordering, validation, timeline tools, and dialogue editing.
- Repair Timeline generation with animation preview clips.
- Configurable dialogue UI prefab, dialogue item prefab, avatar frame, and dialogue background.
- Editor playback preview: Timeline first, then dialogue.
- Dialogue preview appends each spoken line into a scrollable list and closes the panel when the stage dialogue ends.
- Demo generator: `Tools/Merge-2/Create Restaurant Demo`.

## Package Layout

- `Runtime/`: runtime data models and playback components.
- `Editor/`: editor window, demo generation, validation, preview, and timeline tooling.
- `Documentation~/`: package usage notes.

## Requirements

- Unity 6000.0 or newer.
- Timeline, UGUI, and Input System packages.
- Dialogue item prefabs may use UGUI `Text` or TextMeshPro text components. TextMeshPro is detected without a hard assembly dependency.
