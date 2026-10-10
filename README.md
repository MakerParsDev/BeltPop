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
- Repeatable score/tick counts, failure paths, input switching, pause, restart, invalid level boundaries, blocked routes and mismatched input ports.

**Current evidence:** 11/11 EditMode tests passed in Unity 6000.6.5f1 on 2026-10-11. All twelve levels also passed a scripted end-to-end test on a physical Android 9 ARMv7 device. Human-player usability, accessibility and cross-device performance remain **unverified**.

Unity batch mode testing is also possible with `-batchmode -nographics -projectPath "<project>" -runTests -testPlatform EditMode -testResults "<results.xml>" -logFile "<test.log>"` when no other Editor instance has the project open. Do not append `-quit` to this test command: the Test Runner exits the Editor after writing the XML result.

## Build requirements

Install **Android Build Support, Android SDK, Android NDK and OpenJDK** for the Unity Editor version using Unity Hub. **A local Android debug APK was successfully built** with Unity 6000.6.5f1 on 2026-10-11: IL2CPP ARM64 compilation and Gradle packaging completed, and archive integrity plus the Android v2 debug signature were verified. The prototype debug package is `com.makerparsdev.beltpop.debug` (minimum Android API 26, target API 36, ARM64).

**The ARM64 APK remains available for 64-bit Android devices.** A separate ARMv7 debug APK was built, installed and launched on a physical Android 9 device. RUN/PAUSE/RESUME/RETRY input, failure counters and app background/foreground were smoke-tested.

On 2026-10-11, timed Android Monkey touch sequences completed all twelve authored puzzles on the physical device: 114/114 correct parcel deliveries, zero wrong deliveries, three stars per level (36/36 total), working NEXT LEVEL transitions, and the final "All 12 puzzles completed!" screen. This validates a real on-device scripted UI/gameplay path, **not** human-player usability, accessibility, sustained performance or a multi-device compatibility matrix. No Play Console listing, revenue or retention measurement is claimed.

The local debug build entry point is `BeltPop.Editor.BeltPopAndroidBuild.BuildDebug`, available through Unity batch mode (when no other Editor has the project open):

~~~powershell
& "<Unity Editor>/Editor/Unity.exe" -batchmode -quit -projectPath "<project>" -buildTarget Android -executeMethod BeltPop.Editor.BeltPopAndroidBuild.BuildDebug -logFile "<build.log>"
~~~

The regular build method targets only `Assets/Scenes/BeltPopPrototype.unity`, writes the ignored `Builds/Android/BeltPop-debug.apk`, and uses a debug-only application identifier. Both the original identifier and target CPU architecture are restored on normal exit.

For 32-bit ARMv7 test phones, use the separate debug build entry point:

~~~powershell
& "<Unity Editor>/Editor/Unity.exe" -batchmode -quit -projectPath "<project>" -buildTarget Android -executeMethod BeltPop.Editor.BeltPopAndroidBuild.BuildArmv7Debug -logFile "<build-armv7.log>"
~~~

This writes `Builds/Android/BeltPop-debug-armv7.apk` (native `armeabi-v7a`) without replacing the ARM64 APK. Confirm the connected device ABIs with `adb shell getprop ro.product.cpu.abilist` before installation. This ARMv7-only artifact is for local testing, not Google Play publication; production releases must also support ARM64. A forced Editor termination may bypass settings restoration, so verify Android Player Settings after an interrupted build.

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
