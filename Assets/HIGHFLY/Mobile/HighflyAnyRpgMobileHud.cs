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
        private TextMeshProUGUI attackText;
        private Image attackIcon;
        private Button attackButton;
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

            if (openedNativeWindow != null && !openedNativeWindow.IsOpen)
                openedNativeWindow = null;

            nativeModalVisible = gameManager.WindowManager != null
                && gameManager.WindowManager.CurrentWindow != null;
            bool canPlay = !nativeModalVisible && openedNativeWindow == null
                && !menuLayer.activeSelf && !tutorialLayer.activeSelf;
            if (gameplayLayer.activeSelf != canPlay)
                gameplayLayer.SetActive(canPlay);
            HighflyTouchInputState.Enabled = canPlay;
            if (!canPlay) HighflyTouchInputState.Reset();
            backLayer.SetActive(openedNativeWindow != null && openedNativeWindow.IsOpen);
            if (hideDesktopActionBars) HideNative();
            RefreshSlots();
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


        private void RefreshSlots()
        {
            var unit = gameManager.PlayerManagerClient != null
                ? gameManager.PlayerManagerClient.UnitController : null;
            if (unit == null || unit.CharacterAbilityManager == null) return;
            var manager = unit.CharacterAbilityManager;

            // Enumerate learned native combat abilities. A summon, portal, or
            // item in the desktop actionbar must never masquerade as S1/S2.
            var combat = new List<AbilityProperties>();
            foreach (var ability in manager.AbilityList.Values)
            {
                if (ability == null || ability.IsAutoAttack) continue;
                var target = ability.GetTargetOptions(unit);
                if (target == null || !target.CanCastOnEnemy ||
                    target.RequiresGroundTarget) continue;
                combat.Add(ability);
            }
            combat.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName,
                StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < 4; i++)
            {
                AbilityProperties ability = i < combat.Count ? combat[i] : null;
                combatAbilities[i] = ability;
                skillButtons[i].interactable = ability != null;
                skillIcons[i].enabled = ability != null && ability.Icon != null;
                if (skillIcons[i].enabled) skillIcons[i].sprite = ability.Icon;
                skillTexts[i].text = ability != null ? "S" + (i + 1) : "+";

                float cooldown = 0f;
                if (ability != null &&
                    manager.AbilityCoolDownDictionary.TryGetValue(ability.DisplayName,
                        out var cd) && cd.InitialCoolDown > .01f)
                    cooldown = cd.RemainingCoolDown / cd.InitialCoolDown;
                skillCooldowns[i].fillAmount = Mathf.Clamp01(cooldown);
            }

            var basic = manager.AutoAttackAbility;
            attackButton.interactable = basic != null;
            attackIcon.enabled = basic != null && basic.Icon != null;
            if (attackIcon.enabled) attackIcon.sprite = basic.Icon;
            attackText.text = "ATK";
        }

        public void PressSkill(int index)
        {
            if (!GameplayAvailable() || index < 0 || index >= 4) return;
            AbilityProperties ability = combatAbilities[index];
            if (ability == null) return;
            var unit = gameManager.PlayerManagerClient.UnitController;
            unit.CharacterAbilityManager.BeginAbility(ability, true);
        }

        public void PressAttack()
        {
            if (!GameplayAvailable()) return;
            var unit = gameManager.PlayerManagerClient.UnitController;
            if (unit.CharacterAbilityManager.AutoAttackAbility == null)
            {
                Debug.LogWarning("[HIGHFLY A5] ATK unavailable: this character has no native basic attack equipped.");
                return;
            }
            unit.CharacterAbilityManager.AttemptAutoAttack(true);
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
            if (target == null)
            {
                Debug.LogWarning("HIGHFLY A3: AnyRPG window unavailable: " + which);
                return;
            }
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
            GameObject canvasGo = new GameObject("HIGHFLY A4 Mobile Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2316, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            // AnyRPG multiplies camera right-stick input by this preference.
            // An unset value is zero: previously no swipe could rotate the camera.
            if (PlayerPrefs.GetFloat("JoystickLookSpeed", 0f) <= 0.01f)
                PlayerPrefs.SetFloat("JoystickLookSpeed", 1.6f);

            gameplayLayer = Layer("Gameplay", canvasGo.transform);
            // Create camera surface FIRST: action buttons added later win hit testing.
            var look = Panel("Camera swipe", gameplayLayer.transform, C(0,0,0,1));
            Stretch(look, new Vector2(.50f,0), Vector2.one);
            look.gameObject.AddComponent<HighflyTouchLookArea>();

            var move = Panel("Joystick movement", gameplayLayer.transform, C(22,70,105,95));
            Place(move, new Vector2(0,0), new Vector2(183,173), new Vector2(240,240));
            move.GetComponent<Image>().sprite = SoftDisc();
            var knob = Panel("Joystick thumb", move, C(82,170,230,184));
            Place(knob, new Vector2(.5f,.5f), Vector2.zero, new Vector2(85,85));
            knob.GetComponent<Image>().sprite = SoftDisc();
            knob.GetComponent<Image>().raycastTarget = false;
            var joystick = move.gameObject.AddComponent<HighflyTouchMoveArea>();
            joystick.SetKnob(knob);

            // Native AnyRPG actions, not speculative virtual-device buttons.
            var jump = UiButton("SALTO", gameplayLayer.transform, new Vector2(.5f,0),
                new Vector2(-201,146), new Vector2(111,111), C(45,93,140,174));
            jump.onClick.AddListener(HighflyTouchInputState.TapJump);
            var roll = UiButton("RODAR", gameplayLayer.transform, new Vector2(.5f,0),
                new Vector2(-68,146), new Vector2(111,111), C(45,93,140,174));
            roll.onClick.AddListener(HighflyTouchInputState.TapRoll);

            var interact = UiButton("USAR", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-365,90), new Vector2(108,85),
                C(40,80,125,188));
            interact.onClick.AddListener(PressInteract);

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
                    C(35,87,154,190));
                button.onClick.AddListener(() => PressSkill(slot));
                skillButtons[i] = button;
                skillTexts[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                skillIcons[i] = InnerIcon(button.transform);
                skillCooldowns[i] = Cooldown(button.transform);
            }

            attackButton = UiButton("ATK", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-133,140),
                new Vector2(149,149), C(40,103,180,218));
            attackButton.onClick.AddListener(PressAttack);
            attackText = attackButton.GetComponentInChildren<TextMeshProUGUI>();
            attackIcon = InnerIcon(attackButton.transform);

            var menuBtn = UiButton("•••", gameplayLayer.transform,
                Vector2.one, new Vector2(-65,-62),
                new Vector2(94,69), C(40,75,111,195));
            menuBtn.onClick.AddListener(ToggleMenu);

            menuLayer = Layer("Menu (modal)", canvasGo.transform);
            var dim = Panel("Dim background", menuLayer.transform, C(4,12,28,200));
            Stretch(dim, Vector2.zero, Vector2.one);
            var frame = Panel("Centered menu", menuLayer.transform, C(17,31,59,250));
            Place(frame, new Vector2(.5f,.5f), Vector2.zero, new Vector2(760,690));
            Label("HIGHFLY  ·  MENÚ", frame, new Vector2(.5f,1),
                new Vector2(0,-52), new Vector2(650,75), 40);
            string[] items = {"Personaje","Inventario","Habilidades","Misiones",
                "Fabricación","Mapa","Logros","Opciones"};
            string[] targets = {"character","inventory","skills","quests",
                "craft","map","achievements","settings"};
            for (int i = 0; i < items.Length; i++)
            {
                string id = targets[i];
                var button = UiButton(items[i], frame, new Vector2(.5f,.5f),
                    new Vector2(i%2==0 ? -185 : 185, 195 - (i/2)*120),
                    new Vector2(320,96), C(36,76,119,255));
                button.onClick.AddListener(() => OpenNative(id));
            }
            var closeMenu = UiButton("CERRAR", frame, new Vector2(.5f,0),
                new Vector2(0,62), new Vector2(255,78), C(88,54,102,255));
            closeMenu.onClick.AddListener(ToggleMenu);
            var reopenGuide = UiButton("GUÍA TÁCTIL", frame, new Vector2(.5f,0),
                new Vector2(0,153), new Vector2(228,64), C(29,97,133,230));
            reopenGuide.onClick.AddListener(ReopenMobileTutorial);
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
                  "SALTO / RODAR · Movimiento\n" +
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
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = C(100,176,244,255);
            colors.pressedColor = C(29,119,175,255);
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
}
