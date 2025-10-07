# FUnity - Visual Programming Environment

FUnity is a Scratch-inspired learning toolkit built on top of Unity's UI Toolkit. This repository is structured so it can be
installed directly through the Unity Package Manager (UPM) while also shipping with a dedicated development project for local
iteration and play testing.

## Package Installation (UPM)
To install FUnity from GitHub, add the repository URL to your Unity project's `Packages/manifest.json` dependencies:

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git"
```

Unity will download the package, making the runtime code, UI Toolkit layouts, and art assets available under
**Packages/com.papacoder.funity**. Import the bundled sample from the **Package Manager** window to explore a working scene.

## Local Development Project
The repository includes `FUnityProject/`, a Unity project configured to reference the package via a relative path. Open the
project in Unity Hub (Unity 6 or newer) to validate changes you make inside the package folders. The `Packages/manifest.json`
entry uses `"file:../"`, so editing files in the package root immediately reflects inside the development project.

## Repository Layout
- `Runtime/` – Runtime scripts grouped into `Core/`, `UI/`, `Blocks/`, and `Resources/`.
- `UXML/` – UI Toolkit layout definitions used by the package.
- `USS/` – UI Toolkit style sheets.
- `Art/` – Package artwork such as logos or screenshots.
- `Samples~/BasicScene/` – Importable sample scene, controller script, and documentation.
- `FUnityProject/` – Unity project for development and testing (contains `Assets/`, `Packages/`, `ProjectSettings/`, and `UserSettings/`).

## License
FUnity is released under the MIT License. See [LICENSE.md](LICENSE.md) for full details.
