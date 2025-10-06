# Basic Scratch-Style Scene

This sample scene spawns the FUnity workspace at runtime so you can experiment with block-driven logic immediately.

## Usage
1. Import the sample through the Unity Package Manager.
2. Open `BasicScratchScene.unity`.
3. Enter play mode. The scene bootstrapper will create a workspace GameObject, a UI Toolkit document, and populate it with motion and looks blocks.

The layout automatically falls back to a code-generated template if the optional `WorkspaceLayout.uxml` or `WorkspaceStyles.uss` references are not provided.
