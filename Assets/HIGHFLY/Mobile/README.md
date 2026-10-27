# HIGHFLY x AnyRPG — A2 Android Touch Foundation

Status: **SOURCE ONLY — NO UNITY BUILD OR DEVICE VALIDATION YET**

## Why this exists

Our A1 Features Demo WebGL build works, but it still depends on desktop input.
The AnyRPG Core 1.1 engine can use Unity's Input System. This A2 slice adds
a deliberately isolated, opt-in touch adapter without editing AnyRPG's code.

Starting point:
\`highfly/a1-original-demo-webgl-20261026\`.

## Files

- \`HighflyTouchAreas.cs\`: left fixed movement area (virtual \`<Gamepad>/leftStick\`),
  right drag-anywhere camera area (virtual \`<Gamepad>/rightStick\`). Each tracks
  its own pointer ID so two fingers can be used concurrently.
- \`Editor/HighflyMobileTouchHarness.cs\`: Unity Editor menu to add a temporary
  two-zone test Canvas to the currently open gameplay scene. It is NOT the
  final premium HUD and does NOT add gameplay/skills/buttons.
  
## Current dependency check (repository)

- Unity Editor: \`6000.3.10f1\`
- \`com.unity.inputsystem\`: \`1.18.0\`
- \`com.unity.ugui\`: \`2.0.0\`

## First integration test in Unity

1. Open the desired A1 gameplay scene (NOT the character creator/menu scene).
2. In Unity select \`HIGHFLY > Mobile > Create A2 Touch Test Harness\`.
3. Confirm the scene has exactly one active \`EventSystem\`, using
   \`InputSystemUIInputModule\` for touch UI. The builder warns when a different
   existing module is found; inspect rather than automatically replacing it.
4. Inspect the actual \`AnyRPGInputActions\` asset and AnyRPG controller mode.
   Configure \`GamePad\` control mode in the game and verify the actions read
   \`<Gamepad>/leftStick\` and \`<Gamepad>/rightStick\`.
   **Do not assume** the camera uses \`rightStick\`; confirm before judging.
5. Enter Play Mode. On Android/WebGL Android test left thumb movement, right
   swipe camera, then both gestures simultaneously.
6. Release each finger; ensure that movement and rotation stop immediately.
   Open UI panels and check that controls do not intercept clicks; hide or
   disable the test Canvas when an AnyRPG modal window is open.
7. Check up/down, left/right, diagonals, sensitivity, dead zone, latency,
   rotations while moving, loss of focus, and loading a new scene.
8. Review mobile FPS, aspect ratio, safe area and touch accuracy on the
   Galaxy S23 Ultra in landscape.

No A2 code is injected at runtime unless this harness is explicitly added
to a scene. Existing A1 behavior should be unchanged on its original branch.

## Important limitations / next steps

- OnScreenControl synthesizes a virtual Gamepad. If AnyRPG uses PlayerInput
  dynamic device switching, touch and the virtual Gamepad can compete, causing
  input-scheme jitter. Verify the active scheme and adjust pairing/switching
  according to Unity Input System documentation. Do not hack the AnyRPG player
  controller without tracing its current action map first.
- Touch drag -> virtual rightStick is a PROTOTYPE bridge. Validate camera
  rotation and tune sensitivity in device tests. If AnyRPG doesn't consume
  rightStick, route drag deltas through an adapter to its actual camera input.
- The generator intentionally uses plain, temporary debug graphics, not a
  premium UI. Replace with fullscreen gameplay HUD and safe-area-aware layout.
- Buttons for ATK, jump, dash, use, 4 skills, cancel and menu are **not wired**.
  First enumerate real AnyRPG actions/events and build reliable bindings.
- Keep A1 intact; do not merge A2 before Unity compile + WebGL Android touch test.
- Android APK needs Unity Android Build Support (SDK, NDK, OpenJDK), ARM64,
  landscape/fullscreen settings and actual device testing.

## References

- AnyRPG new Input System / replaceable AnyRPGInputActions:
  https://discussions.unity.com/t/released-any-mmo-rpg-open-source-rpg-mmorpg-engine/761611?page=7
- Unity On-Screen Controls:
  https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/OnScreen.html
- Unity guide to custom OnScreenControl:
  https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/create-custom-on-screen-control.md
- Unity Android Input:
  https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/developing/input

Licence: new HIGHFLY touch adapter code may be reused under the root MIT license.
This adds no external models, textures, fonts or paid assets.
