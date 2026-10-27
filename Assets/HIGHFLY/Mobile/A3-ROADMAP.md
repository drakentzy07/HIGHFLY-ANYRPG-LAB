# HIGHFLY × AnyRPG — A3 Native Mobile HUD (2026-10-27)

**Status: SOURCE INTEGRATED — NOT VERIFIED IN UNITY OR ON GALAXY S23 ULTRA.**
A1 and A2 remain separately available. A3 branches off A2.

## Immediate purpose

Replace the PC action-bar presentation with a real touch input layer that
invokes the **existing AnyRPG action bar and game windows**. Preserve the
underlying inventory, quest, crafting, progression, items and ability logic.

## Files introduced / changed in A3

- \`HighflyAnyRpgMobileHud.cs\` — automatically generated overlay:
  * Left analog joystick; right half touch-and-drag camera.
  * Jump via native AnyRPG \`JUMP\` mapping to XInput button West.
  * ROLL as prototype via R keyboard mapping. This is **not** FURY DASH.
  * ATK + S1..S4 call **actual** \`ActionButton.OnClick(true)\` through
    \`UIManager.ActionBarManager.GetCurrentActionButtons()\`.
  * Equipment icons and native cooldown overlay copied from existing
    \`ActionButton\` presentation only; damage, cost and cooldown remain owned
    by AnyRPG.
  * USAR calls native PlayerController interaction with nearby/selected target.
  * Square centered menu opens real native AnyRPG windows: character, bag,
    abilities, quests, crafting, map, achievements, settings.
  * A back button closes a window that this new menu opened.
  * Temporarily hides only original action bars, preserving their data and
    restoring their CanvasGroup state on HUD destruction.
- \`HighflyMobileAutoBootstrap.cs\` — auto adds HUD on Android and handheld
  WebGL. Leaves desktop default UI alone.
- \`HighflyTouchMoveArea.cs\`, \`HighflyTouchLookArea.cs\` — moved the A2
  controllers to properly named Unity scripts, and target the same virtual
  XInputController device used in the original AnyRPG input action map.
- \`Editor/HighflyA3Installer.cs\` — opt-in scene installer plus diagnostics.

## A3 slot policy — important

The first native AnyRPG actionbar slot is used for ATK; the next four for S1
through S4. All five are **existing AnyRPG slots**, not invented skills.

In the inspector adjust \`attackSlotIndex\` and \`firstSkillSlotIndex\`
if the desired equipment assignment is different.

If a native slot is empty, the corresponding touch button is disabled.
To use ATK, equip an appropriate attack usable into the chosen source slot.
Future evolution: drive basic attack directly from native auto-attack system,
rather than consuming an action slot.

## What this does NOT claim

- No Unity compile has been executed here.
- The input layout, camera vertical direction, mobile focus, C# compile,
  multitouch fidelity and fullscreen-safe-area behavior remain unverified.
- Gamepad bindings are Unity XInput-compatible and must be tested on mobile
  WebGL: unusual virtual-device pairing may require an input action adapter.
- ROLL currently maps to the default R key, not a custom Fury 360 dash.
- Skills are regular tap-to-cast. MOBA drag aim, cancel button, air casts,
  four-hit combo and true free aim are subsequent milestones.
- Native inventory/menu content is currently opened unchanged and needs
  mobile re-layout (large tap targets, responsive fonts, pagination). The
  *entry* is now mobile-friendly, not the complete native window content.
- HP/MP/Energy remains visible through native player unit UI until a custom
  status-bar bridge is wired.
- This HUD has runtime UI creation with a debug art style. It is not the final
  polished visual skin.

## Critical validation checklist

1. Compile A3 on Unity 6000.3.10f1 with Input System 1.18.0.
2. Build A3 WebGL without changing A1; verify artifact / test URL.
3. Test Samsung S23 Ultra horizontal. Check menu/creator has no touch HUD.
4. Enter gameplay. Verify left joystick, diagonals, camera horizontal AND
   vertical, simultaneous two-finger movement + camera.
5. Equip actionbar slot 0 with attack and slots 1..4 with usable skills.
   Confirm cooldowns, cost, hit registration and native ability timing.
6. Jump and roll while walking; avoid claiming Fury dash is integrated.
7. Open Bag, Skills, Quests, Character, Craft, Map, Options; confirm native
   windows are reachable and return button works.
8. Profile framerate, touch areas, top-right menu cutouts, screen orientation.
9. Audit original native window sizes and migrate each to responsive UI.
10. Only after these tests: prepare clean Pages release then APK ARM64.

## Native AnyRPG integration verified from public source

- \`Assets/AnyRPG/Core/System/Scripts/UI/Buttons/ActionButton.cs\`
  — \`OnClick(true)\`, \`Useable\`, \`Icon\`, \`CoolDownIcon\`.
- \`Assets/AnyRPG/Core/System/Scripts/GameManager/UIManagers/ActionBarManager.cs\`
  — \`GetCurrentActionButtons()\`.
- \`Assets/AnyRPG/Core/System/Scripts/GameManager/UIManagers/UIManager.cs\`
  — action bar and popup references.
- \`Assets/AnyRPG/Core/System/Scripts/Characters/Player/PlayerController.cs\`
  — \`RightMouseInteraction()\` and \`Interactables\`.
- \`Assets/AnyRPG/Core/System/Input/AnyRPGInputActions.inputactions\`
  — jump XInput button West, roll keyboard R, analog via Gamepad.current.
- Unity OnScreenControl docs:
  https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/OnScreen.html

All new A3 code is project-authored; no external art assets added.
