# Repository Guidelines

## Project Structure

This repository is a Unity 6.3 project (`6000.3.28f1`) using URP. `Assets/Scenes` contains `SampleScene.unity`; `Assets/Settings` contains render pipeline and volume assets; `Assets/TutorialInfo/Scripts` contains template readme scripts. Put new gameplay code and assets under descriptive folders in `Assets`. `Packages/manifest.json` and `Packages/packages-lock.json` define dependencies; `ProjectSettings` stores project-wide settings. Keep Unity-generated `.meta` files beside their assets and preserve their GUIDs.

## Build and Development

- The Unity CLI is available in PowerShell as `unity`. In Codex shell sessions, use the Windows App Execution Alias at `"$env:LOCALAPPDATA\Microsoft\WindowsApps\unity.exe"` with elevated execution access if the sandbox cannot launch `unity` by name. Do not install a duplicate CLI or change `PATH` for this.
- `unity open .` opens this project in its configured Editor.
- `unity status --project-path .` checks the Editor and Pipeline connection.
- When more than one Editor may be open, include `--project-path .` to target this checkout.
- `unity test . --mode EditMode` runs EditMode tests; use `PlayMode` for runtime tests.
- `unity build . --target StandaloneWindows64` builds a Windows player. Change the target as needed; Unity must have that platform module installed.

The Unity Test Framework package is installed, but no project-specific tests currently exist. Add tests under `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`, with assembly definitions where appropriate. Run relevant tests after adding or changing testable behavior.

## C# Style and Assets

Use four spaces for indentation. Name types and methods in `PascalCase`, locals and parameters in `camelCase`, and private fields in `camelCase`; expose Inspector fields with `[SerializeField] private`. Keep one primary MonoBehaviour per script and name the file after its class. Prefer small, focused components. Edit scenes and assets in Unity when possible; do not hand-edit generated project files. Do not commit local caches or build output such as `Library`, `Temp`, `Logs`, or `obj`.

## Commits and Pull Requests

This checkout has no Git metadata, so existing commit conventions cannot be verified. Use concise Conventional Commit prefixes such as `feat:`, `fix:`, or `docs:`. PRs should explain the change and how it was checked, link related work when available, and include screenshots for visible scene or UI changes. Note any Unity version or package changes.
