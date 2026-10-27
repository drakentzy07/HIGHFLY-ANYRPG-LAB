// HIGHFLY A4 — Compact mobile HUD and touch onboarding on proven AnyRPG Features Demo.
// Does not edit AnyRPG engine. A gameplay scene must opt into this component.
// Real skills and RPG windows use native AnyRPG APIs (not fake events).
using System;
using System.Collections.Generic;
using AnyRPG;
using HIGHFLY.Mobile;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace HIGHFLY.Mobile
{
    [DisallowMultipleComponent]
    public sealed class HighflyAnyRpgMobileHud : MonoBehaviour
    {
        [Header("Native slots: configure in AnyRPG's action bars")]
        [SerializeField, Min(0)] private int attackSlotIndex = 0;
        [SerializeField, Min(0)] private int firstSkillSlotIndex = 1;
        [SerializeField] private bool hideDesktopActionBars = true;
        [SerializeField] private bool onlyWhileCharacterActive = true;

        private static HighflyAnyRpgMobileHud instance;
        private SystemGameManager gameManager;
        private Canvas canvas;
        private GameObject gameplayLayer, menuLayer, backLayer, tutorialLayer;
        private bool tutorialDismissed;
        private bool nativeModalVisible;
        private static Sprite circleSprite, roundedSprite;
        private const string TutorialSeenKey = "HIGHFLY_A4_MOBILE_GUIDE_SEEN";
        // Combat skills are discovered from learned AnyRPG abilities, never from
        // arbitrary utility/actionbar positions (e.g. summons or portals).
        private AbilityProperties[] combatAbilities = new AbilityProperties[4];
        private TextMeshProUGUI[] skillTexts = new TextMeshProUGUI[4];
        private Image[] skillIcons = new Image[4];
        private Image[] skillCooldowns = new Image[4];
        private Button[] skillButtons = new Button[4];
        private RectTransform[] skillTransforms = new RectTransform[4];
        private RectTransform jumpTransform, dashTransform, interactTransform, attackTransform, moveTransform;
        private GameObject customizationPanel;
        private TextMeshProUGUI[] assignmentLabels = new TextMeshProUGUI[4];
        private string[] equippedNames = new string[4];
        private const string LayoutKey = "HIGHFLY_A6_HUD_LAYOUT";
        private const string ScaleKey = "HIGHFLY_A6_HUD_SCALE";
        private const string OpacityKey = "HIGHFLY_A6_HUD_OPACITY";
        private const string SkillKey = "HIGHFLY_A6_EQUIPPED_";
        private int layoutPreset = 1;
        private float uiScale = 1f;
        private float uiOpacity = 1f;
        private GameObject aimCancelButton;
        private TextMeshProUGUI aimInstruction;
        private int aimingSlot = -1;
        private bool groundAimActive;
        private bool directionalAimActive;
        private RectTransform directionPreview;
        private Image directionPreviewImage;
        private bool aimWasCancelled;
        private Vector2 aimTouchStart;
        private TextMeshProUGUI attackText;
        private TextMeshProUGUI menuFeedbackText;
        private Image attackIcon;
        private Button attackButton;
        private Button dashButton;
        private TextMeshProUGUI dashLabel;
        private CloseableWindow openedNativeWindow;
        private float nextRefresh;
        private bool activeLastFrame;
        private readonly Dictionary<CanvasGroup, State> hiddenNative = new Dictionary<CanvasGroup, State>();
        private readonly List<CanvasGroup> addedNative = new List<CanvasGroup>();

        private struct State
        {
            public float alpha;
            public bool blocksRaycasts;
            public bool interactable;
        }

        private static Color C(byte r, byte g, byte b, byte a = 255)
            => new Color32(r, g, b, a);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            tutorialDismissed = PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
            layoutPreset = PlayerPrefs.GetInt(LayoutKey, 1);
            uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(ScaleKey, 1f), .85f, 1.35f);
            uiOpacity = Mathf.Clamp(PlayerPrefs.GetFloat(OpacityKey, 1f), .45f, 1f);
            for (int i = 0; i < 4; i++)
                equippedNames[i] = PlayerPrefs.GetString(SkillKey + i, "");
            Build();
            tutorialLayer.SetActive(false);
            gameplayLayer.SetActive(false); // do not synthesize devices in login/character creator
            canvas.enabled = false;
            HighflyTouchInputState.Enabled = false;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.12f;

            if (gameManager == null)
                gameManager = FindFirstObjectByType<SystemGameManager>(FindObjectsInactive.Include);

            bool ready = gameManager != null && gameManager.UIManager != null
                && gameManager.UIManager.ActionBarManager != null
                && (!onlyWhileCharacterActive || (gameManager.PlayerManagerClient != null
                    && gameManager.PlayerManagerClient.UnitController != null));

            if (canvas.enabled != ready) canvas.enabled = ready;
            if (!ready)
            {
                HighflyTouchInputState.Enabled = false;
                HighflyTouchInputState.Reset();
                if (activeLastFrame) RestoreNative();
                gameplayLayer.SetActive(false);
                menuLayer.SetActive(false);
                backLayer.SetActive(false);
                openedNativeWindow = null;
                tutorialLayer.SetActive(false);
                nativeModalVisible = false;
                activeLastFrame = false;
                return;
            }

            if (!activeLastFrame)
            {
                activeLastFrame = true;
                if (!tutorialDismissed) tutorialLayer.SetActive(true);
            }

            SuppressDesktopHelp();

            // FURY 360: AnyRPG's free-rotate mode uses camera direction for
            // 360-degree movement, unlike its old character-relative strafe mode.
            var playerController = gameManager.PlayerManagerClient != null
                ? gameManager.PlayerManagerClient.PlayerController : null;
            if (playerController != null) playerController.strafeModeActive = false;

            if (openedNativeWindow != null && !openedNativeWindow.IsOpen)
                openedNativeWindow = null;

            nativeModalVisible = gameManager.WindowManager != null
                && gameManager.WindowManager.CurrentWindow != null;
            bool canPlay = !nativeModalVisible && openedNativeWindow == null
                && !menuLayer.activeSelf && !tutorialLayer.activeSelf;
            if (gameplayLayer.activeSelf != canPlay)
                gameplayLayer.SetActive(canPlay);
            HighflyTouchInputState.Enabled = canPlay;
            if (!canPlay)
            {
                HighflyTouchInputState.Reset();
                if (groundAimActive || directionalAimActive) CancelSkillAim();
            }
            backLayer.SetActive(openedNativeWindow != null && openedNativeWindow.IsOpen);
            if (hideDesktopActionBars) HideNative();
            RefreshSlots();
            if (dashLabel != null)
            {
                float remaining = HighflyTouchInputState.DashRemaining;
                dashLabel.text = remaining > 0.05f ? remaining.ToString("0.0") : "DASH";
                dashButton.interactable = remaining <= 0f;
            }
        }

        // These are the keyboard/desktop help overlays visible in the mobile video.
        // Close instead of destroying to preserve upstream AnyRPG prefabs.
        private void SuppressDesktopHelp()
        {
            if (gameManager == null || gameManager.UIManager == null) return;
            var ui = gameManager.UIManager;
            if (ui.keyboardHintWindow != null && ui.keyboardHintWindow.IsOpen)
                ui.keyboardHintWindow.CloseWindow();
            if (ui.helpMenuWindow != null && ui.helpMenuWindow.IsOpen)
                ui.helpMenuWindow.CloseWindow();
        }

        public void DismissMobileTutorial()
        {
            tutorialDismissed = true;
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
            tutorialLayer.SetActive(false);
        }

        public void ReopenMobileTutorial()
        {
            if (nativeModalVisible || openedNativeWindow != null) return;
            menuLayer.SetActive(false);
            tutorialLayer.SetActive(true);
        }

        // Use the exact active actionbar slots, so icons, equipment assignment,
        // learned abilities and cooldowns remain owned by AnyRPG.
        private List<ActionButton> Slots()
        {
            if (gameManager == null || gameManager.UIManager == null
                || gameManager.UIManager.ActionBarManager == null) return null;
            return gameManager.UIManager.ActionBarManager.GetCurrentActionButtons();
        }

        private static ActionButton At(List<ActionButton> slots, int index)
        {
            return slots != null && index >= 0 && index < slots.Count
                ? slots[index] : null;
        }


        private static bool LooksLikeCombat(AbilityProperties ability, UnitController unit)
        {
            if (ability == null || ability.IsAutoAttack ||
                ability is GatherAbilityProperties || ability is CraftAbilityProperties)
                return false;
            string name = (ability.DisplayName ?? "").ToLowerInvariant();
            // Keep generic traversal, portals and summons out of the 4 combat slots.
            if (name.Contains("portal") || name.Contains("teleport") ||
                name.Contains("summon") ||
                name.Contains("gate") || name.Contains("mount") ||
                name.Contains("recall") || name.Contains("hearth"))
                return false;
            var target = ability.GetTargetOptions(unit);
            if (target != null && target.CanCastOnEnemy) return true;

            // Some genuine offensive spells are self-centered AoE / projectiles.
            // Check native AnyRPG effect types, not whether the slot is occupied.
            // This preserves combat skills without ever equipping fishing/crafting.
            var effects = ability.GetCastEndEffects(unit);
            if (effects != null)
                foreach (var effect in effects)
                    if (effect is AttackEffectProperties ||
                        effect is ProjectileEffectProperties ||
                        (effect is AOEEffectProperties && target != null &&
                         target.RequiresGroundTarget))
                        return true;
            return false;
        }

        private void RefreshSlots()
        {
            var unit = gameManager.PlayerManagerClient != null
                ? gameManager.PlayerManagerClient.UnitController : null;
            if (unit == null || unit.CharacterAbilityManager == null) return;
            var manager = unit.CharacterAbilityManager;
            var candidates = new List<AbilityProperties>();

            // Prefer original player-equipped action bar, not random alphabetic skills.
            var slots = Slots();
            if (slots != null)
                foreach (var slot in slots)
                {
                    var ability = slot != null ? slot.Useable as AbilityProperties : null;
                    if (LooksLikeCombat(ability, unit) && !candidates.Contains(ability))
                        candidates.Add(ability);
                }
            foreach (var ability in manager.AbilityList.Values)
                if (LooksLikeCombat(ability, unit) && !candidates.Contains(ability))
                    candidates.Add(ability);

            // Explicitly equipped abilities take precedence across reloads;
            // never backfill with gathering, crafting or travel skills.
            var assigned = new AbilityProperties[4];
            var used = new HashSet<AbilityProperties>();
            for (int i = 0; i < assigned.Length; i++)
                if (!string.IsNullOrEmpty(equippedNames[i]))
                    foreach (var candidate in candidates)
                        if (candidate.ResourceName == equippedNames[i] && !used.Contains(candidate))
                        {
                            assigned[i] = candidate;
                            used.Add(candidate);
                            break;
                        }
            int nextCandidate = 0;
            for (int i = 0; i < 4; i++)
            {
                if (assigned[i] != null) continue;
                while (nextCandidate < candidates.Count && used.Contains(candidates[nextCandidate]))
                    nextCandidate++;
                if (nextCandidate >= candidates.Count) break;
                assigned[i] = candidates[nextCandidate++];
                used.Add(assigned[i]);
            }
            for (int index = 0; index < 4; index++)
            {
                var ability = assigned[index];
                combatAbilities[index] = ability;
                skillButtons[index].interactable = ability != null;
                skillIcons[index].enabled = ability != null && ability.Icon != null;
                if (skillIcons[index].enabled) skillIcons[index].sprite = ability.Icon;
                skillTexts[index].text = ability != null ? "S" + (index + 1) : "+";
                float cooldown = 0f;
                if (ability != null && manager.AbilityCoolDownDictionary.TryGetValue(
                    ability.DisplayName, out var cd) && cd.InitialCoolDown > .01f)
                    cooldown = cd.RemainingCoolDown / cd.InitialCoolDown;
                skillCooldowns[index].fillAmount = Mathf.Clamp01(cooldown);
            }

            // Stock demo characters may load with the unarmed attack unset.
            // Use the AnyRPG unit profile default or a learned auto-attack.
            if (manager.AutoAttackAbility == null)
                manager.LearnDefaultAutoAttackAbility();
            if (manager.AutoAttackAbility == null)
                foreach (var learned in manager.AbilityList.Values)
                    if (learned != null && learned.IsAutoAttack)
                    {
                        manager.SetAutoAttackAbility(learned);
                        break;
                    }
            // The stock Mecanim player has "Unarmed" but no default ability.
            // Reuse the original Features Demo Attack resource in that case.
            // Nothing is unlocked or written into the player's saved ability list.
            if (manager.AutoAttackAbility == null && gameManager.SystemDataFactory != null)
            {
                var attackResource = gameManager.SystemDataFactory.GetResource<Ability>("Attack");
                if (attackResource != null && attackResource.AbilityProperties != null)
                    manager.SetAutoAttackAbility(attackResource.AbilityProperties);
            }
            var basic = manager.AutoAttackAbility;
            attackButton.interactable = true; // always allow feedback on press
            attackIcon.enabled = basic != null && basic.Icon != null;
            if (attackIcon.enabled) attackIcon.sprite = basic.Icon;
            attackText.text = basic != null ? "ATK" : "ATK?";
        }

        private static bool IsTravelOrSummon(AbilityProperties ability)
        {
            string name = (ability.DisplayName ?? "").ToLowerInvariant();
            return name.Contains("portal") || name.Contains("teleport") ||
                name.Contains("summon") || name.Contains("dragon") ||
                name.Contains("gate") || name.Contains("mount") ||
                name.Contains("recall") || name.Contains("hearth");
        }


        // Touch skill handling (left joystick and right-hand pointer remain independent).
        // Only ground-target skills use AnyRPG's original circular projector.
        public void BeginSkillTouch(int index, Vector2 screenPosition)
        {
            aimWasCancelled = false;
            if (!GameplayAvailable() || index < 0 || index >= combatAbilities.Length)
                return;
            var ability = combatAbilities[index];
            var unit = gameManager.PlayerManagerClient.UnitController;
            if (ability == null || unit == null) return;
            var target = ability.GetTargetOptions(unit);
            if (target == null) return;
            if (groundAimActive || directionalAimActive) CancelSkillAim();

            if (!target.RequiresGroundTarget)
            {
                directionalAimActive = true;
                aimingSlot = index;
                aimTouchStart = screenPosition;
                if (aimCancelButton != null) aimCancelButton.SetActive(true);
                if (aimInstruction != null)
                {
                    aimInstruction.text = "ARRASTRÁ PARA DIRIGIR  ·  SOLTÁ PARA LANZAR";
                    aimInstruction.gameObject.SetActive(true);
                }
                if (directionPreview != null) directionPreview.gameObject.SetActive(true);
                return;
            }
            var manager = unit.CharacterAbilityManager;
            manager.BeginAbility(ability, true);
            if (!manager.WaitingForTarget()) return;

            groundAimActive = true;
            aimingSlot = index;
            aimTouchStart = screenPosition;
            HighflyTouchAimState.Active = true;
            if (aimCancelButton != null) aimCancelButton.SetActive(true);
            if (aimInstruction != null) aimInstruction.gameObject.SetActive(true);
            if (gameManager.CastTargettingManager != null)
                gameManager.CastTargettingManager.EnableProjector(ability);
            DragSkillTouch(index, screenPosition);
        }

        public void DragSkillTouch(int index, Vector2 screenPosition)
        {
            if (directionalAimActive && aimingSlot == index)
            {
                Vector2 delta = screenPosition - aimTouchStart;
                var rect = aimCancelButton != null
                    ? aimCancelButton.transform as RectTransform : null;
                bool cancel = rect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect,screenPosition,null);
                if (aimInstruction != null)
                    aimInstruction.text = cancel ? "SOLTÁ PARA CANCELAR" :
                        "APUNTADO DIRECCIONAL  ·  SOLTÁ PARA LANZAR";
                if (directionPreview != null)
                {
                    Vector2 origin;
                    var root = gameplayLayer.transform as RectTransform;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        root,aimTouchStart,null,out origin))
                    {
                        directionPreview.anchoredPosition = origin;
                        directionPreview.sizeDelta = new Vector2(
                            Mathf.Clamp(delta.magnitude/Mathf.Max(.1f,canvas.scaleFactor),
                                8f,310f),14f);
                        directionPreview.localRotation =
                            Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                        if (directionPreviewImage != null)
                            directionPreviewImage.color = cancel ? C(245,85,109,205) :
                                C(64,214,255,215);
                    }
                }
                return;
            }
            if (!groundAimActive || aimingSlot != index) return;
            if (aimCancelButton != null)
            {
                var rect = aimCancelButton.transform as RectTransform;
                bool hoveringCancel = rect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect,
                        screenPosition, null);
                if (aimInstruction != null)
                    aimInstruction.text = hoveringCancel
                        ? "SOLTÁ PARA CANCELAR"
                        : "APUNTÁ CON EL DEDO  ·  SOLTÁ PARA LANZAR";
            }

            // Start at camera center, then steer from the original touch location:
            // otherwise a skill icon at the lower right would aim at the sky/feet.
            HighflyTouchAimState.ScreenPosition =
                new Vector2(Screen.width * .50f, Screen.height * .54f) +
                (screenPosition - aimTouchStart) * 1.40f;
            var cast = gameManager != null ? gameManager.CastTargettingManager : null;
            if (cast != null && cast.CastTargetController != null)
                cast.CastTargetController.Follow();
        }

        public void EndSkillTouch(int index, Vector2 screenPosition)
        {
            if (aimWasCancelled)
            {
                aimWasCancelled = false;
                return;
            }
            if (directionalAimActive && aimingSlot == index)
            {
                Vector2 delta = screenPosition - aimTouchStart;
                var rect = aimCancelButton != null
                    ? aimCancelButton.transform as RectTransform : null;
                bool cancel = rect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect,screenPosition,null);
                directionalAimActive = false;
                aimingSlot = -1;
                if (directionPreview != null) directionPreview.gameObject.SetActive(false);
                if (aimCancelButton != null) aimCancelButton.SetActive(false);
                if (aimInstruction != null) aimInstruction.gameObject.SetActive(false);
                if (cancel) return;
                if (delta.sqrMagnitude > 28f*28f)
                    AimAtWorldDirection(delta);
                PressSkill(index);
                return;
            }
            if (!groundAimActive || aimingSlot != index)
            {
                PressSkill(index);
                return;
            }

            if (aimCancelButton != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    aimCancelButton.transform as RectTransform, screenPosition, null))
            {
                CancelSkillAim();
                return;
            }
            DragSkillTouch(index, screenPosition);
            var unit = gameManager.PlayerManagerClient.UnitController;
            var cast = gameManager.CastTargettingManager;
            var camera = gameManager.CameraManager != null
                ? gameManager.CameraManager.ActiveMainCamera : null;
            if (cast == null || camera == null || unit == null)
            {
                CancelSkillAim();
                return;
            }
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(HighflyTouchAimState.ScreenPosition);
            if (!Physics.Raycast(ray, out hit, 100f,
                gameManager.SystemConfigurationManager.DefaultGroundMask) ||
                Vector3.Distance(unit.transform.position, hit.point) > 39f)
            {
                if (aimInstruction != null) aimInstruction.text = "FUERA DE ALCANCE";
                CancelSkillAim();
                return;
            }

            // Uses the engine's real ground-target cast and effect context.
            groundAimActive = false;
            aimingSlot = -1;
            HighflyTouchAimState.Active = false;
            if (aimCancelButton != null) aimCancelButton.SetActive(false);
            if (aimInstruction != null) aimInstruction.gameObject.SetActive(false);
            unit.CharacterAbilityManager.SetGroundTargetClient(hit.point);
        }

        public void CancelSkillAim()
        {
            if (!groundAimActive && !directionalAimActive) return;
            bool wasGround = groundAimActive;
            directionalAimActive = false;
            groundAimActive = false;
            aimWasCancelled = true;
            aimingSlot = -1;
            HighflyTouchAimState.Active = false;
            if (aimCancelButton != null) aimCancelButton.SetActive(false);
            if (aimInstruction != null) aimInstruction.gameObject.SetActive(false);
            if (directionPreview != null) directionPreview.gameObject.SetActive(false);
            if (wasGround && gameManager != null && gameManager.PlayerManagerClient != null &&
                gameManager.PlayerManagerClient.UnitController != null)
                gameManager.PlayerManagerClient.UnitController.CharacterAbilityManager
                    .DeactivateTargetingMode();
        }

        // Align direction with the actual camera and native enemy target.
        // Does not change hitboxes or invent a cone wider than the underlying spell.
        private void AimAtWorldDirection(Vector2 drag)
        {
            var unit = gameManager.PlayerManagerClient.UnitController;
            var cam = gameManager.CameraManager != null
                ? gameManager.CameraManager.ActiveMainCamera : null;
            if (unit == null || cam == null) return;
            Vector3 forward=cam.transform.forward;forward.y=0f;
            Vector3 right=cam.transform.right;right.y=0f;
            if (forward.sqrMagnitude<.0001f) forward=unit.transform.forward;
            forward.Normalize(); right.Normalize();
            Vector3 heading=(right*drag.x+forward*drag.y).normalized;
            if (heading.sqrMagnitude<.001f) return;
            unit.UnitMotor.FaceDirection(heading);

            UnitController closest=null;
            float bestScore=float.MaxValue;
            foreach(var candidate in FindObjectsByType<UnitController>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == unit ||
                    candidate.CharacterStats == null || !candidate.CharacterStats.IsAlive ||
                    candidate.BaseCharacter == null ||
                    Faction.RelationWith(candidate,unit.BaseCharacter.Faction) > -1)
                    continue;
                var to=candidate.transform.position-unit.transform.position;
                to.y=0;
                float distance=to.magnitude;
                if (distance > 35f || distance < .05f) continue;
                float alignment=Vector3.Dot(heading,to/distance);
                if (alignment < .80f) continue;
                float score=distance + (1f-alignment)*18f;
                if (score < bestScore){bestScore=score;closest=candidate;}
            }
            if(closest != null) unit.SetTarget(closest);
        }

        public void PressSkill(int index)
        {
            if (!GameplayAvailable() || index < 0 || index >= 4) return;
            var ability = combatAbilities[index];
            if (ability == null) return;
            var unit = gameManager.PlayerManagerClient.UnitController;
            unit.CharacterAbilityManager.BeginAbility(ability, true);
        }

        public void CycleEquippedSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 3 || gameManager == null ||
                gameManager.PlayerManagerClient == null) return;
            var unit = gameManager.PlayerManagerClient.UnitController;
            if (unit == null || unit.CharacterAbilityManager == null) return;
            var list = new List<AbilityProperties>();
            foreach (var skill in unit.CharacterAbilityManager.AbilityList.Values)
                if (LooksLikeCombat(skill,unit)) list.Add(skill);
            list.Sort((a,b) => string.Compare(a.DisplayName,b.DisplayName,StringComparison.Ordinal));
            if (list.Count == 0) return;
            int currentIndex = list.FindIndex(a => a.ResourceName == equippedNames[slotIndex]);
            for (int offset = 1; offset <= list.Count; offset++)
            {
                var next = list[(currentIndex+offset+list.Count)%list.Count];
                bool alreadyUsed = false;
                for (int i = 0; i < 4; i++)
                    if (i != slotIndex && equippedNames[i] == next.ResourceName)
                        alreadyUsed = true;
                if (alreadyUsed) continue;
                equippedNames[slotIndex] = next.ResourceName;
                PlayerPrefs.SetString(SkillKey + slotIndex,next.ResourceName);
                PlayerPrefs.Save();
                if (assignmentLabels[slotIndex] != null)
                    assignmentLabels[slotIndex].text = "S"+(slotIndex+1)+" · "+next.DisplayName;
                RefreshSlots();
                return;
            }
        }

        public void PressAttack()
        {
            if (!GameplayAvailable()) return;
            var unit = gameManager.PlayerManagerClient.UnitController;
            var manager = unit.CharacterAbilityManager;
            if (manager.AutoAttackAbility == null)
                manager.LearnDefaultAutoAttackAbility();
            if (manager.AutoAttackAbility == null && gameManager.SystemDataFactory != null)
            {
                var attackResource = gameManager.SystemDataFactory.GetResource<Ability>("Attack");
                if (attackResource != null)
                    manager.SetAutoAttackAbility(attackResource.AbilityProperties);
            }
            if (manager.AutoAttackAbility == null)
            {
                Debug.LogWarning("[HIGHFLY FURY] No se encontró el recurso nativo de ataque básico.");
                return;
            }
            UnitController enemy = unit.Target as UnitController;
            if (enemy == null || enemy.CharacterStats == null || !enemy.CharacterStats.IsAlive ||
                Faction.RelationWith(enemy, unit.BaseCharacter.Faction) > -1)
            {
                enemy = null;
                float bestSq = 12f * 12f;
                foreach (var candidate in FindObjectsByType<UnitController>(FindObjectsSortMode.None))
                {
                    if (candidate == null || candidate == unit ||
                        candidate.CharacterStats == null || !candidate.CharacterStats.IsAlive ||
                        candidate.BaseCharacter == null ||
                        Faction.RelationWith(candidate, unit.BaseCharacter.Faction) > -1)
                        continue;
                    Vector3 delta = candidate.transform.position - unit.transform.position;
                    float sq = delta.sqrMagnitude;
                    if (sq > bestSq || Vector3.Dot(unit.transform.forward, delta.normalized) < -.25f)
                        continue;
                    enemy = candidate;
                    bestSq = sq;
                }
            }
            if (enemy != null)
            {
                unit.SetTarget(enemy);
                unit.CharacterCombat.Attack(enemy, true);
            }
            else
            {
                // A basic swing may still animate without a target depending on
                // the unit's own attack clips. Never damage friendly NPCs.
                manager.AttemptAutoAttack(true);
                Debug.Log("[HIGHFLY FURY] Sin objetivo hostil próximo.");
            }
        }

        private bool GameplayAvailable()
        {
            return gameManager != null && gameManager.PlayerManagerClient != null
                && gameManager.PlayerManagerClient.UnitController != null
                && openedNativeWindow == null && !menuLayer.activeSelf
                && !tutorialLayer.activeSelf && !nativeModalVisible;
        }

        public void PressInteract()
        {
            if (!GameplayAvailable()) return;
            var controller = gameManager.PlayerManagerClient.PlayerController;
            if (controller == null) return;
            if (controller.Interactables != null && controller.Interactables.Count > 0)
            {
                controller.RightMouseInteraction(controller.Interactables[
                    controller.Interactables.Count - 1]);
            }
            else
            {
                var target = gameManager.PlayerManagerClient.UnitController.Target;
                if (target != null) controller.RightMouseInteraction(target);
            }
        }

        public void ToggleMenu()
        {
            if (openedNativeWindow != null)
            {
                CloseNativeWindow();
                return;
            }
            bool show = !menuLayer.activeSelf;
            menuLayer.SetActive(show);
            gameplayLayer.SetActive(!show && !nativeModalVisible && !tutorialLayer.activeSelf);
        }

        public void OpenNative(string which)
        {
            if (gameManager == null || gameManager.UIManager == null) return;
            UIManager ui = gameManager.UIManager;
            CloseableWindow target = null;
            switch (which)
            {
                case "inventory": target = ui.inventoryWindow; break;
                case "character": target = ui.characterPanelWindow; break;
                case "skills": target = ui.abilityBookWindow; break;
                case "quests": target = ui.questLogWindow; break;
                case "craft": target = ui.craftingWindow; break;
                case "map": target = ui.mainMapWindow; break;
                case "settings": target = ui.settingsMenuWindow; break;
                case "achievements": target = ui.achievementListWindow; break;
                case "mainmenu": target = ui.inGameMainMenuWindow; break;
            }
            if (target == null || target.CloseableWindowContents == null)
            {
                string warning = "Este módulo no está configurado en Features Demo: " + which;
                Debug.LogWarning("[HIGHFLY A5] " + warning);
                if (menuFeedbackText != null) menuFeedbackText.text = warning;
                return;
            }
            if (menuFeedbackText != null) menuFeedbackText.text = "";
            menuLayer.SetActive(false);
            gameplayLayer.SetActive(false);
            openedNativeWindow = target;
            backLayer.SetActive(true);
            target.OpenWindow();
        }

        public void CloseNativeWindow()
        {
            if (openedNativeWindow != null && openedNativeWindow.IsOpen)
                openedNativeWindow.CloseWindow();
            openedNativeWindow = null;
            backLayer.SetActive(false);
            menuLayer.SetActive(false);
            gameplayLayer.SetActive(!nativeModalVisible && !tutorialLayer.activeSelf);
        }

        private void HideNative()
        {
            var ui = gameManager.UIManager;
            var bars = ui.ActionBarManager;
            if (bars == null) return;

            if (bars.SystemBarController != null)
                Hide(bars.SystemBarController.gameObject);

            if (bars.ActionBarControllers != null)
                foreach (var bar in bars.ActionBarControllers)
                    if (bar != null) Hide(bar.gameObject);

            // The native gamepad panel still owns the cooldown/icon state.
            if (ui.GamepadWindow != null) Hide(ui.GamepadWindow.gameObject);
        }

        private void Hide(GameObject target)
        {
            if (target == null) return;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
                addedNative.Add(group);
            }
            if (!hiddenNative.ContainsKey(group))
                hiddenNative[group] = new State {
                    alpha = group.alpha,
                    blocksRaycasts = group.blocksRaycasts,
                    interactable = group.interactable
                };
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void RestoreNative()
        {
            foreach (var entry in hiddenNative)
            {
                if (entry.Key == null) continue;
                entry.Key.alpha = entry.Value.alpha;
                entry.Key.blocksRaycasts = entry.Value.blocksRaycasts;
                entry.Key.interactable = entry.Value.interactable;
            }
            hiddenNative.Clear();
            foreach (var group in addedNative)
                if (group != null) Destroy(group);
            addedNative.Clear();
        }

        private void OnDestroy()
        {
            RestoreNative();
            HighflyTouchInputState.Enabled = false;
            HighflyTouchInputState.Reset();
            if (instance == this) instance = null;
        }

        // Runtime UI creation keeps all of our HUD separate from upstream
        // AnyRPG prefabs and remains simple to remove/rebuild.
        private void Build()
        {
            GameObject canvasGo = new GameObject("HIGHFLY FURY Premium Android Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            canvasGo.AddComponent<HighflySpanishMobileUI>();
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2316, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            // AnyRPG multiplies camera right-stick input by this preference.
            // An unset value is zero: previously no swipe could rotate the camera.
            // Upgrade old slow A5 default but respect deliberate user settings.
            if (PlayerPrefs.GetFloat("JoystickLookSpeed", 0f) <= 1.61f)
                PlayerPrefs.SetFloat("JoystickLookSpeed", 2.85f);

            gameplayLayer = Layer("Gameplay", canvasGo.transform);
            // Create camera surface FIRST: action buttons added later win hit testing.
            var look = Panel("Camera swipe", gameplayLayer.transform, C(0,0,0,1));
            Stretch(look, new Vector2(.50f,0), Vector2.one);
            look.gameObject.AddComponent<HighflyTouchLookArea>();

            var move = Panel("FURY 360 joystick", gameplayLayer.transform, C(8,25,49,154));
            Place(move, new Vector2(0,0), new Vector2(216,198), new Vector2(342,342));
            moveTransform = move;
            move.GetComponent<Image>().sprite = SoftDisc();
            var outerRing = move.gameObject.AddComponent<Outline>();
            outerRing.effectColor = C(59,205,255,115);
            outerRing.effectDistance = new Vector2(3,-3);
            var knob = Panel("FURY direction control", move, C(38,164,226,218));
            Place(knob, new Vector2(.5f,.5f), Vector2.zero, new Vector2(112,112));
            knob.GetComponent<Image>().sprite = SoftDisc();
            var knobShadow = knob.gameObject.AddComponent<Shadow>();
            knobShadow.effectColor = C(0,0,0,210);
            knobShadow.effectDistance = new Vector2(4,-6);
            knob.GetComponent<Image>().raycastTarget = false;
            var joystick = move.gameObject.AddComponent<HighflyTouchMoveArea>();
            joystick.SetKnob(knob);

            // Native AnyRPG actions, not speculative virtual-device buttons.
            // Right-hand mobility next to skills; two fingers can move and jump.
            var jump = UiButton("SALTO ×2", gameplayLayer.transform, Vector2.right,
                new Vector2(-765,119), new Vector2(120,120), C(27,82,129,240));
            jump.gameObject.AddComponent<HighflyImmediatePress>().IsJump = true;
            jumpTransform = jump.transform as RectTransform;
            dashButton = UiButton("DASH", gameplayLayer.transform, Vector2.right,
                new Vector2(-630,119), new Vector2(120,120), C(56,50,130,240));
            dashLabel = dashButton.GetComponentInChildren<TextMeshProUGUI>();
            dashTransform = dashButton.transform as RectTransform;
            dashButton.gameObject.AddComponent<HighflyImmediatePress>().IsJump = false;

            var interact = UiButton("USAR", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-382,83), new Vector2(108,85),
                C(18,73,102,233));
            interact.onClick.AddListener(PressInteract);
            interactTransform = interact.transform as RectTransform;

            // Arced four skills; center-right under the camera swipe surface.
            Vector2[] positions = {
                new Vector2(-531,227), new Vector2(-411,308),
                new Vector2(-283,336), new Vector2(-167,280)
            };
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                var button = UiButton("S"+(i+1), gameplayLayer.transform,
                    new Vector2(1,0), positions[i], new Vector2(112,112),
                    C(18,56,102,236));
                var pointer = button.gameObject.AddComponent<HighflySkillPointer>();
                pointer.Hud = this;
                pointer.Slot = slot;
                skillButtons[i] = button;
                skillTransforms[i] = button.transform as RectTransform;
                skillTexts[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                skillIcons[i] = InnerIcon(button.transform);
                skillCooldowns[i] = Cooldown(button.transform);
            }

            // Aim cancel is visible only while dragging a ground-target skill.
            var cancelAim = UiButton("✕", gameplayLayer.transform,
                Vector2.one, new Vector2(-64,-166),
                new Vector2(95,95), C(124,35,63,248));
            cancelAim.onClick.AddListener(CancelSkillAim);
            aimCancelButton = cancelAim.gameObject;
            aimCancelButton.SetActive(false);
            var beam = Panel("FURY mobile direction guide",gameplayLayer.transform,
                C(64,214,255,215));
            beam.anchorMin = beam.anchorMax = new Vector2(.5f,.5f);
            beam.pivot = new Vector2(0f,.5f);
            beam.sizeDelta=new Vector2(8,14);
            directionPreview=beam;
            directionPreviewImage=beam.GetComponent<Image>();
            directionPreviewImage.raycastTarget=false;
            directionPreview.gameObject.SetActive(false);
            aimInstruction = Label("APUNTÁ CON EL DEDO  ·  SOLTÁ PARA LANZAR",
                gameplayLayer.transform, new Vector2(.5f,1),
                new Vector2(0,-91), new Vector2(680,66), 26);
            aimInstruction.gameObject.SetActive(false);

            attackButton = UiButton("ATK", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-133,140),
                new Vector2(156,156), C(20,104,168,247));
            attackButton.onClick.AddListener(PressAttack);
            attackTransform = attackButton.transform as RectTransform;
            attackText = attackButton.GetComponentInChildren<TextMeshProUGUI>();
            attackIcon = InnerIcon(attackButton.transform);

            var menuBtn = UiButton("☰", gameplayLayer.transform,
                Vector2.one, new Vector2(-65,-62),
                new Vector2(94,69), C(12,40,73,242));
            menuBtn.onClick.AddListener(ToggleMenu);

            menuLayer = Layer("Menu (modal)", canvasGo.transform);
            var dim = Panel("Dim background", menuLayer.transform, C(4,12,28,200));
            Stretch(dim, Vector2.zero, Vector2.one);
            var frame = Panel("Centered menu", menuLayer.transform, C(7,23,48,252));
            var frameEdge = frame.gameObject.AddComponent<Outline>();
            frameEdge.effectColor = C(61,179,230,210);
            frameEdge.effectDistance = new Vector2(2,-2);
            Place(frame, new Vector2(.5f,.5f), Vector2.zero, new Vector2(935,790));
            Label("HIGHFLY  ·  MENÚ", frame, new Vector2(.5f,1),
                new Vector2(0,-58), new Vector2(800,75), 43);
            menuFeedbackText = Label("", frame, new Vector2(.5f,0),
                new Vector2(0,10), new Vector2(690,55), 18);
            string[] items = {"Personaje","Inventario","Habilidades","Misiones",
                "Fabricación","Mapa","Logros","Opciones"};
            string[] targets = {"character","inventory","skills","quests",
                "craft","map","achievements","settings"};
            for (int i = 0; i < items.Length; i++)
            {
                string id = targets[i];
                var button = UiButton(items[i], frame, new Vector2(.5f,.5f),
                    new Vector2(i%2==0 ? -216 : 216, 225 - (i/2)*122),
                    new Vector2(387,102), C(36,76,119,255));
                button.onClick.AddListener(() => OpenNative(id));
            }
            var closeMenu = UiButton("CERRAR", frame, new Vector2(.5f,0),
                new Vector2(0,62), new Vector2(255,78), C(88,54,102,255));
            closeMenu.onClick.AddListener(ToggleMenu);
            var editHud = UiButton("EDITAR HUD / SENSIBILIDAD", frame,
                new Vector2(.5f,0), new Vector2(0,177),
                new Vector2(425,74), C(12,100,147,255));
            editHud.onClick.AddListener(() => customizationPanel.SetActive(true));
            var reopenGuide = UiButton("GUÍA TÁCTIL", frame, new Vector2(.5f,0),
                new Vector2(0,264), new Vector2(228,64), C(29,97,133,230));
            reopenGuide.onClick.AddListener(ReopenMobileTutorial);
            BuildCustomization(menuLayer.transform);
            ApplyHudPreset();
            menuLayer.SetActive(false);

            backLayer = Layer("Back from native AnyRPG window", canvasGo.transform);
            var back = UiButton("◀ VOLVER", backLayer.transform, Vector2.one,
                new Vector2(-125,-75), new Vector2(220,96), C(48,94,145,255));
            back.onClick.AddListener(CloseNativeWindow);
            backLayer.SetActive(false);

            tutorialLayer = Layer("Guía táctil HIGHFLY", canvasGo.transform);
            var overlay = Panel("Fondo guía", tutorialLayer.transform, C(4,10,24,200));
            Stretch(overlay, Vector2.zero, Vector2.one);
            var card = Panel("Guía móvil", tutorialLayer.transform, C(17,37,66,250));
            Place(card, new Vector2(.5f,.5f), Vector2.zero, new Vector2(730,525));
            Label("HIGHFLY · GUÍA TÁCTIL", card, new Vector2(.5f,1),
                new Vector2(0,-70), new Vector2(680,65), 34);
            Label("JOYSTICK IZQUIERDO · Caminar\n" +
                  "LADO DERECHO · Deslizá para mover la cámara\n" +
                  "SALTO / DASH · Movimiento\n" +
                  "ATK / S1–S4 · Combate y habilidades\n" +
                  "••• · Inventario, personaje y opciones",
                  card, new Vector2(.5f,.5f), new Vector2(0,4),
                  new Vector2(678,275), 26);
            var start = UiButton("¡ENTRAR AL MUNDO!", card,
                new Vector2(.5f,0), new Vector2(0,70),
                new Vector2(390,83), C(26,121,147,255));
            start.onClick.AddListener(DismissMobileTutorial);
            tutorialLayer.SetActive(false);
        }


        private void ApplyHudPreset()
        {
            // Base layout preserves the full S23 landscape controls. MOBA
            // gathers main actions around the right thumb without overlap.
            Vector2[] offsets = layoutPreset == 1
                ? new [] {
                    new Vector2(-560,217), new Vector2(-475,352),
                    new Vector2(-337,409), new Vector2(-213,342)
                  }
                : new [] {
                    new Vector2(-531,227), new Vector2(-411,308),
                    new Vector2(-283,336), new Vector2(-167,280)
                  };
            for (int i=0; i<4; i++)
                if (skillTransforms[i] != null) skillTransforms[i].anchoredPosition=offsets[i];
            if (jumpTransform != null)
                jumpTransform.anchoredPosition = layoutPreset == 1
                    ? new Vector2(-756,104) : new Vector2(-765,119);
            if (dashTransform != null)
                dashTransform.anchoredPosition = layoutPreset == 1
                    ? new Vector2(-629,104) : new Vector2(-630,119);
            if (interactTransform != null)
                interactTransform.anchoredPosition = layoutPreset == 1
                    ? new Vector2(-418,100) : new Vector2(-382,83);
            if (attackTransform != null)
                attackTransform.anchoredPosition = layoutPreset == 1
                    ? new Vector2(-144,126) : new Vector2(-133,140);
            if (moveTransform != null)
                moveTransform.localScale = Vector3.one * uiScale;
            foreach (var t in skillTransforms)
                if (t != null) t.localScale=Vector3.one*uiScale;
            foreach (var t in new [] {jumpTransform,dashTransform,interactTransform,attackTransform})
                if (t != null) t.localScale=Vector3.one*uiScale;
            foreach (var t in new [] {moveTransform,jumpTransform,dashTransform,interactTransform,attackTransform})
                SetOpacity(t);
            foreach (var t in skillTransforms) SetOpacity(t);
        }

        private void SetOpacity(RectTransform rect)
        {
            if (rect == null) return;
            var image = rect.GetComponent<Image>();
            if (image == null) return;
            var color=image.color;
            color.a=Mathf.Min(1f,uiOpacity);
            image.color=color;
        }

        private void BuildCustomization(Transform parent)
        {
            var panel = Panel("A6 HUD customizer", parent, C(5,18,40,253));
            customizationPanel=panel.gameObject;
            Stretch(panel,new Vector2(0,0),Vector2.one);
            Label("HIGHFLY · CONTROLES MÓVILES",panel,
                new Vector2(.5f,1f),new Vector2(0,-95),new Vector2(1250,70),40);
            Label("CÁMARA HORIZONTAL",panel,new Vector2(.5f,.5f),
                new Vector2(-370,245),new Vector2(460,50),27);
            CreateSettingSlider(panel,new Vector2(220,245),
                PlayerPrefs.GetFloat("HIGHFLY_A6_CAMERA_HORIZONTAL",1.25f),
                .45f,2.7f,v=>PlayerPrefs.SetFloat("HIGHFLY_A6_CAMERA_HORIZONTAL",v));
            Label("CÁMARA VERTICAL",panel,new Vector2(.5f,.5f),
                new Vector2(-370,155),new Vector2(460,50),27);
            CreateSettingSlider(panel,new Vector2(220,155),
                PlayerPrefs.GetFloat("HIGHFLY_A6_CAMERA_VERTICAL",1.10f),
                .45f,2.7f,v=>PlayerPrefs.SetFloat("HIGHFLY_A6_CAMERA_VERTICAL",v));
            Label("TAMAÑO DE HUD",panel,new Vector2(.5f,.5f),
                new Vector2(-370,65),new Vector2(460,50),27);
            CreateSettingSlider(panel,new Vector2(220,65),uiScale,.85f,1.35f,
                v=>{uiScale=v;PlayerPrefs.SetFloat(ScaleKey,v);ApplyHudPreset();});
            Label("TRANSPARENCIA",panel,new Vector2(.5f,.5f),
                new Vector2(-370,-25),new Vector2(460,50),27);
            CreateSettingSlider(panel,new Vector2(220,-25),uiOpacity,.45f,1f,
                v=>{uiOpacity=v;PlayerPrefs.SetFloat(OpacityKey,v);ApplyHudPreset();});
            var moba=UiButton("DISTRIBUCIÓN MOBA",panel,
                new Vector2(.5f,.5f),new Vector2(-240,-136),
                new Vector2(380,72),C(16,104,157,255));
            moba.onClick.AddListener(()=>{layoutPreset=1;PlayerPrefs.SetInt(LayoutKey,1);ApplyHudPreset();});
            var classic=UiButton("DISTRIBUCIÓN CLÁSICA",panel,
                new Vector2(.5f,.5f),new Vector2(240,-136),
                new Vector2(380,72),C(52,70,125,255));
            classic.onClick.AddListener(()=>{layoutPreset=0;PlayerPrefs.SetInt(LayoutKey,0);ApplyHudPreset();});
            for(int i=0;i<4;i++)
            {
                int index=i;
                var button=UiButton("S"+(i+1)+" · CAMBIAR SKILL",panel,
                    new Vector2(.5f,.5f),new Vector2(-630+i*418,-264),
                    new Vector2(380,75),C(24,77,111,255));
                assignmentLabels[i]=button.GetComponentInChildren<TextMeshProUGUI>();
                button.onClick.AddListener(()=>CycleEquippedSkill(index));
            }
            var done=UiButton("GUARDAR Y VOLVER",panel,new Vector2(.5f,.5f),
                new Vector2(0,-382),new Vector2(450,76),C(13,120,145,255));
            done.onClick.AddListener(()=>{PlayerPrefs.Save();customizationPanel.SetActive(false);});
            customizationPanel.SetActive(false);
        }

        private static void CreateSettingSlider(Transform parent, Vector2 pos,
            float value,float min,float max, UnityEngine.Events.UnityAction<float> onChange)
        {
            var track=Panel("A6 slider",parent,C(31,63,92,255));
            Place(track,new Vector2(.5f,.5f),pos,new Vector2(690,25));
            var fill=Panel("Fill",track,C(41,193,225,255));
            Stretch(fill,Vector2.zero,Vector2.one);
            var handle=Panel("Touch handle",track,C(207,244,255,255));
            Place(handle,new Vector2(.5f,.5f),Vector2.zero,new Vector2(62,66));
            var slider=track.gameObject.AddComponent<Slider>();
            slider.targetGraphic=handle.GetComponent<Image>();
            slider.fillRect=fill;
            slider.handleRect=handle;
            slider.minValue=min;slider.maxValue=max;
            slider.value=value;
            slider.onValueChanged.AddListener(onChange);
        }

        private static Sprite SoftDisc()
        {
            if (circleSprite == null) circleSprite = CreateHudShape(false);
            return circleSprite;
        }

        private static Sprite SoftRounded()
        {
            if (roundedSprite == null) roundedSprite = CreateHudShape(true);
            return roundedSprite;
        }

        // Reusable lightweight alpha mask: circular action buttons and rounded menus.
        // Native Unity UI sprites, no external assets or license obligations.
        private static Sprite CreateHudShape(bool rounded)
        {
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) * (2f / n) - 1f;
                float v = (y + .5f) * (2f / n) - 1f;
                float signedDistance;
                if (rounded)
                {
                    float dx = Mathf.Max(Mathf.Abs(u) - .80f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(v) - .80f, 0f);
                    signedDistance = Mathf.Sqrt(dx * dx + dy * dy) - .17f;
                }
                else
                {
                    signedDistance = Mathf.Sqrt(u*u + v*v) - .95f;
                }
                float alpha = Mathf.Clamp01(.04f - signedDistance * 32f);
                texture.SetPixel(x, y, new Color(1,1,1,alpha));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0,0,n,n), new Vector2(.5f,.5f));
        }

        private static GameObject Layer(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, Vector2.zero, Vector2.one);
            return go;
        }

        private static RectTransform Panel(string name, Transform parent, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = new Vector2(.5f,.5f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = pos; rect.sizeDelta = size;
        }

        private static TextMeshProUGUI Label(string text, Transform parent,
            Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, anchor, pos, size);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(13, fontSize * .65f);
            tmp.fontSizeMax = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button UiButton(string text, Transform parent,
            Vector2 anchor, Vector2 pos, Vector2 size, Color bg)
        {
            var image = Panel(text+" Button", parent, bg);
            Place(image, anchor, pos, size);
            image.GetComponent<Image>().sprite = size.x <= size.y * 1.65f ? SoftDisc() : SoftRounded();
            var edge = image.gameObject.AddComponent<Outline>();
            edge.effectColor = C(56,188,235,192);
            edge.effectDistance = new Vector2(2,-2);
            var shade = image.gameObject.AddComponent<Shadow>();
            shade.effectColor = C(0,0,0,195);
            shade.effectDistance = new Vector2(4,-6);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = C(68,180,221,255);
            colors.pressedColor = C(20,124,171,255);
            colors.disabledColor = C(35,48,66,133);
            button.colors = colors;
            Label(text, image, new Vector2(.5f,.5f),
                Vector2.zero, size - new Vector2(8,8), Mathf.Clamp(size.y * .23f, 17f, 27f));
            return button;
        }

        private static void OnScreen(string title, Transform parent, Vector2 anchor,
            Vector2 pos, Vector2 size, Color bg, string inputControl)
        {
            var rect = Panel(title+" OnScreenInput", parent, bg);
            Place(rect, anchor, pos, size);
            rect.GetComponent<Image>().sprite = SoftDisc();
            var control = rect.gameObject.AddComponent<OnScreenButton>();
            control.controlPath = inputControl;
            Label(title, rect, new Vector2(.5f,.5f),
                Vector2.zero, size, 20);
        }

        private static Image InnerIcon(Transform parent)
        {
            var rect = Panel("Native skill icon", parent, Color.white);
            Stretch(rect, new Vector2(.10f,.10f), new Vector2(.90f,.90f));
            var image = rect.GetComponent<Image>();
            image.enabled = false;
            image.raycastTarget = false;
            rect.SetAsFirstSibling(); // behind slot label, behind cooldown
            return image;
        }

        private static Image Cooldown(Transform parent)
        {
            var rect = Panel("Native cooldown", parent, C(5,12,22,160));
            Stretch(rect, Vector2.zero, Vector2.one);
            var image = rect.GetComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillAmount = 0;
            image.raycastTarget = false;
            // text remains visible above cooldown graphic.
            rect.SetSiblingIndex(Math.Max(0,rect.GetSiblingIndex()-1));
            return image;
        }
    }

    // The standard Button.onClick is fired on pointer release: a mobile
    // movement action must begin on touch-down, including while walking.
    public sealed class HighflyImmediatePress : MonoBehaviour, IPointerDownHandler
    {
        public bool IsJump;
        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsJump) HighflyTouchInputState.TapJump();
            else HighflyTouchInputState.TapDash();
        }
    }
}
