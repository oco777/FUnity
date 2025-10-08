# FUnity - Visual Programming Environment

FUnity is a Scratch-inspired learning toolkit that brings block-based programming concepts to Unity's UI Toolkit. The package
provides runtime code, UI definitions, artwork, and samples that can be installed through the Unity Package Manager (UPM).

## Requirements
- Unity 6.0 (6000.0) or newer
- UI Toolkit package (automatically resolved as `com.unity.ui`)

## Installing from GitHub (UPM)
To install FUnity from GitHub, add the repository URL to your Unity project's `Packages/manifest.json` dependencies:

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git"
```

Unity will download the package and expose the runtime code, UI Toolkit layouts, art assets, and documentation under
**Packages/com.papacoder.funity**. Import the bundled sample from the **Package Manager** window to explore a working scene.

## Local Development
If you want to make changes to the package locally:

1. Clone this repository next to your Unity project directory.
2. In your Unity project's `Packages/manifest.json`, reference the package using a local path, for example:

   ```json
   "com.papacoder.funity": "file:../FUnity"
   ```

3. Open the Unity project. Changes you make inside the cloned repository's folders are immediately reflected.

## Samples
The package ships with a **Basic Scene** sample. Import it through the Unity Package Manager to see a minimal setup. The sample
includes a scene, panel settings, and a `SampleController` script demonstrating basic usage.

## Documentation
Character backstories and other supporting documentation live in the `Docs/` folder. Additional guides will be added here as the
project evolves.

## Repository Layout
- `Art/` – Logos and artwork used by the package.
- `Docs/` – Project documentation (e.g., character descriptions).
- `Runtime/` – Runtime scripts grouped into `Core/`, `UI/`, `Blocks/`, and `Resources/`.
- `UXML/` – UI Toolkit layout definitions.
- `USS/` – UI Toolkit style sheets.
- `Samples~/BasicScene/` – Importable sample scene, controller script, and documentation for the sample.
- `CHANGELOG.md` – Package changelog following Unity package conventions.
- `package.json` – Unity package manifest describing metadata and dependencies.

## License
FUnity is released under the MIT License. See [LICENSE.md](LICENSE.md) for full details.
