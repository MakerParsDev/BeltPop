# BeltPop: Sort & Build

**BeltPop** is an Android-first Unity 6 puzzle/builder project. The current playable vertical slice is an offline conveyor-routing puzzle, not a commercial release.

## Current playable slice

- **12 authored 7×7 layouts**, using four route families: straight, red detour, blue detour and combined detours. Every map has a fixed, reproducible package manifest and one color-routing switch.
- Route red (R) packages **up** and blue (B) packages **right** through the purple junction; the downstream belts lead to matching exits.
- **RUN / PAUSE / RESUME / RETRY / NEXT LEVEL** controls, package counters, level results and three-star perfect completion. A failure does not consume lives or require advertising.
- Deterministic pure C# simulation isolated from the Unity presentation: stable row-major positions, explicit input/output ports, start-of-tick occupancy, simultaneous move commits and queued player switches.
- The Unity scene is **Assets/Scenes/BeltPopPrototype.unity**, and it is the **first enabled build scene** in `ProjectSettings/EditorBuildSettings.asset`.
- No server, advertisements, purchases, account creation, tracking SDK or paid external service is used by the prototype.

These are **design-validation layouts**, not final hand-reviewed commercial content. The builder metagame, audio/art polish, progress persistence, daily challenges, monetization and Android production release remain future work.

## Open and play

1. Open the Unity project in the Editor version declared by `ProjectSettings/ProjectVersion.txt`. The source was tested with `6000.6.5f1` and Universal 2D / URP.
2. Open `Assets/Scenes/BeltPopPrototype.unity` and press **Play**.
3. Click **RUN** and rotate the purple junction before each parcel exits. Pause or retry freely. Completing a level with all correct deliveries unlocks the next of the twelve authored layouts.
4. If the scene is not present in an earlier checkout, Unity's **BeltPop > Create Playable Prototype Scene** menu creates it without overwriting an existing scene.

The prototype is portrait-first. The Game View may show unused side margins in a landscape desktop window; Android is the planned delivery platform.

## Tests and reproduction

Use **Window > General > Test Runner > EditMode** and run assembly `BeltPop.EditModeTests`. The tests assert:

- All twelve manifests, unique board geometries and correct source/sink topology.
- A deterministic **100%-correct solution trace for each of the twelve levels**.
- Repeatable score/tick counts, failure paths, input switching, pause, restart and invalid level boundaries.

**Current evidence:** 9/9 EditMode tests passed in Unity on 2026-10-10. Real-player usability review and physical Android device coverage are **not yet verified**.

Unity batch mode testing is also possible with `-runTests -testPlatform EditMode -testResults <path> -quit` when the project is not locked by another Editor instance.

## Build requirements

Android Build Support, Android SDK/NDK and OpenJDK must be installed through Unity Hub to produce an Android APK/AAB. The development machine used for the first slice did **not** have the Android build module installed; no Android binary, Play Console listing, revenue or retention measurements are claimed.

The optional **CoplayDev MCP for Unity** editor package is referenced in `Packages/manifest.json`. Optional Roslyn tooling downloaded by that package is not part of the game runtime and should not be confused with game source.

## Code layout

| Folder | Responsibility |
| --- | --- |
| `Assets/BeltPop/Core/` | Deterministic, Unity-independent game state and twelve-level catalog |
| `Assets/BeltPop/Presentation/` | Portrait-first UI, tile visualization and touch input |
| `Assets/BeltPop/Editor/` | Non-overwriting playable scene initializer |
| `Assets/BeltPop/Tests/Editor/` | Unity NUnit EditMode regression coverage |
| `Assets/Scenes/` | Unity scene assets |

The Git repository is the implementation source of truth. Keep generated `Library/`, `Temp/`, build outputs, signing keys, credentials and local editor cache out of version control.
