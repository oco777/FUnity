# FUnity

FUnity is a Scratch-inspired visual programming toolkit for Unity projects. This package ships in Unity Package Manager (UPM) format so that educators and creators can drop the workspace into any project that targets Unity 6000.0.58f2.

## Features
- Node-based workspace tailored for children
- UI Toolkit driven block palette and stage preview
- Extensible block definitions and execution runtime
- Sample scene demonstrating a minimal setup

## Requirements
- Unity 6000.0.58f2 or newer
- UI Toolkit package (`com.unity.ui`)

## Installation
1. Open your Unity project.
2. Open **Window > Package Manager**.
3. Click the `+` button and choose **Add package from disk...**
4. Select the `package.json` inside `Packages/com.papacoder.funity`.

## Getting Started
1. Import the sample from **Package Manager > FUnity > Samples > Basic Scratch-Style Workspace**.
2. Open the `BasicScratchScene` sample scene.
3. Press play to see the workspace surface with starter blocks. Explore the scripts in `Runtime/` to build custom block behaviours.

## Folder Overview
- `Runtime/Core`: Core runtime data models and services that drive the visual programming engine.
- `Runtime/UI`: UI Toolkit components for rendering the workspace, palette, and stage.
- `Runtime/Blocks`: Reusable block definitions and behaviours.
- `Runtime/Resources`: Shared ScriptableObjects and default assets loaded at runtime.
- `UXML` & `USS`: UI Toolkit templates and styling.
- `Art`: Artwork and icons for the interface.
- `Samples~`: Importable examples that showcase practical usage.

## License
Distributed under the MIT License. See `LICENSE.md` for details.
