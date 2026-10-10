# BeltPop — repository-native agent instructions

This public Unity repository is the implementation source of truth. Inspect live Git status and Unity project files before changes; preserve unrelated in-progress edits.

- Unity Editor: pinned in `ProjectSettings/ProjectVersion.txt`; check `Packages/manifest.json` and `Packages/packages-lock.json` before package work.
- Run the prototype from `Assets/Scenes/BeltPopPrototype.unity`. Use **BeltPop > Create Playable Prototype Scene** once if it is absent; the creator refuses to overwrite an existing prototype and refuses to replace a dirty active scene.
- Domain rules: `Assets/BeltPop/Core/BeltPopSimulation.cs` contains deterministic pure C# without Unity physics/frame-time authority; view in `Assets/BeltPop/Presentation/BeltPopGame.cs` only displays state and gathers taps.
- Automated regression: Unity Test Runner EditMode, assembly `BeltPop.EditModeTests` at `Assets/BeltPop/Tests/Editor`. Add a regression test when changing movement order, ports, scoring or loss/win rules.
- Never delete or revert other agents' existing local edits; no blind editor-scene overwrite. No generated `Library/`, `Temp/`, credentials, signing keys or developer account details in Git.
- Do not add ads, IAP, user analytics SDK, paid services, server/auth or production store actions before gameplay validation, specific permissions and compliance review.
- Check Console errors, EditMode test results and Android build evidence at the exact current working tree before saying a phase is complete.
- Project is Android-first / portrait, offline-first; no real-time multiplayer or pay-to-win. Avoid copying proprietary artwork or creating misleading store assets.
- For working instructions see `README.md` and the actual checked-in code, not private external session records.
