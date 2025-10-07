# FUnity

FUnity is a Scratch-inspired learning toolkit distributed through the Unity Package Manager (UPM). The repository is organised as a Unity package at the repository root with a dedicated development project stored under `FUnityProject/` for local testing and iteration.

## Getting Started
1. Open your Unity project.
2. Add the following entry to your `Packages/manifest.json` dependencies section:
   ```json
   "com.papacoder.funity": "https://github.com/oco777/FUnity.git"
   ```
3. After Unity finishes resolving packages, explore the **Samples** window to import the `Basic Scene` sample.

### Local Development Project

To experiment with the package in isolation, open the project located in `FUnityProject/` with Unity 2021.3 or newer. The project depends on the local package via a relative path (`"file:../"`), so changes made to the package are immediately reflected inside the project.

## Package Layout
- **Runtime/** – Core runtime scripts, UI helpers, block definitions, and resource assets.
- **Editor/** – Editor tooling to streamline FUnity specific authoring flows.
- **UXML/** and **USS/** – UI Toolkit layouts and styles.
- **Art/** – Package art assets such as logos or promotional imagery.
- **Samples~/** – Importable samples demonstrating package usage.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request describing your proposed changes.

## License
FUnity is released under the MIT License. See [LICENSE.md](LICENSE.md) for more information.
