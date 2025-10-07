# FUnity

FUnity is a Scratch-inspired learning toolkit distributed through the Unity Package Manager (UPM). This repository contains the canonical package layout ready to be imported into any Unity 2021.3 or newer project.

## Getting Started
1. Open your Unity project.
2. Add the following entry to your `Packages/manifest.json` dependencies section:
   ```json
   "com.funity.core": "https://github.com/FUnity/FUnity.git"
   ```
3. After Unity finishes resolving packages, explore the **Samples** window to import the `Basic Scene` sample.

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
