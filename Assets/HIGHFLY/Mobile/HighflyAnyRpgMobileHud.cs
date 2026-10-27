// HIGHFLY A3 — Mobile HUD adapter for AnyRPG Core 1.1.
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
        private GameObject gameplayLayer, menuLayer, backLayer;
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
            Build();
            gameplayLayer.SetActive(false); // do not synthesize devices in login/character creator
            canvas.enabled = false;
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
                if (activeLastFrame) RestoreNative();
                gameplayLayer.SetActive(false);
                menuLayer.SetActive(false);
                backLayer.SetActive(false);
                openedNativeWindow = null;
                activeLastFrame = false;
                return;
            }

            if (!activeLastFrame)
            {
                activeLastFrame = true;
                gameplayLayer.SetActive(openedNativeWindow == null && !menuLayer.activeSelf);
                if (hideDesktopActionBars) HideNative();
            }

            if (hideDesktopActionBars) HideNative(); // newly opened gamepad window

            if (openedNativeWindow != null && !openedNativeWindow.IsOpen)
            {
                openedNativeWindow = null;
                backLayer.SetActive(false);
                gameplayLayer.SetActive(true);
            }

            RefreshSlots();
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
            var slots = Slots();
            for (int i = 0; i < 4; i++)
            {
                ActionButton original = At(slots, firstSkillSlotIndex + i);
                bool equipped = original != null && original.Useable != null;
                skillButtons[i].interactable = equipped;
                skillIcons[i].enabled = equipped && original.Icon != null
                    && original.Icon.sprite != null;
                if (skillIcons[i].enabled) skillIcons[i].sprite = original.Icon.sprite;

                // Native cooldown UI already reflects actual ability cooldown.
                float remaining = original != null && original.CoolDownIcon != null
                    ? original.CoolDownIcon.fillAmount : 0f;
                skillCooldowns[i].fillAmount = equipped ? Mathf.Clamp01(remaining) : 0f;
                skillTexts[i].text = equipped ? "S" + (i + 1) : "+";
            }

            var attack = At(slots, attackSlotIndex);
            bool attackReady = attack != null && attack.Useable != null;
            attackButton.interactable = attackReady;
            attackIcon.enabled = attackReady && attack.Icon != null && attack.Icon.sprite != null;
            if (attackIcon.enabled) attackIcon.sprite = attack.Icon.sprite;
            attackText.text = "ATK";
        }

        public void PressSkill(int index)
        {
            if (!GameplayAvailable() || index < 0 || index >= 4) return;
            PressSlot(firstSkillSlotIndex + index);
        }

        public void PressAttack()
        {
            if (!GameplayAvailable()) return;
            PressSlot(attackSlotIndex);
        }

        private bool GameplayAvailable()
        {
            return gameManager != null && gameManager.PlayerManagerClient != null
                && gameManager.PlayerManagerClient.UnitController != null
                && openedNativeWindow == null && !menuLayer.activeSelf;
        }

        private void PressSlot(int slot)
        {
            ActionButton action = At(Slots(), slot);
            if (action == null || action.Useable == null) return;
            action.OnClick(true);
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
            gameplayLayer.SetActive(!show);
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
            gameplayLayer.SetActive(true);
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
            if (instance == this) instance = null;
        }

        // Runtime UI creation keeps all of our HUD separate from upstream
        // AnyRPG prefabs and remains simple to remove/rebuild.
        private void Build()
        {
            GameObject canvasGo = new GameObject("HIGHFLY A3 Mobile Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2340, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            gameplayLayer = Layer("Gameplay", canvasGo.transform);
            // Create camera surface FIRST: action buttons added later win hit testing.
            var look = Panel("Camera swipe", gameplayLayer.transform, C(0,0,0,1));
            Stretch(look, new Vector2(.50f,0), Vector2.one);
            look.gameObject.AddComponent<HighflyTouchLookArea>();

            var move = Panel("Joystick movement", gameplayLayer.transform, C(22,70,105,125));
            Place(move, new Vector2(0,0), new Vector2(225,215), new Vector2(300,300));
            var knob = Panel("Joystick thumb", move, C(65,165,226,210));
            Place(knob, new Vector2(.5f,.5f), Vector2.zero, new Vector2(98,98));
            knob.GetComponent<Image>().raycastTarget = false;
            var joystick = move.gameObject.AddComponent<HighflyTouchMoveArea>();
            joystick.SetKnob(knob);

            OnScreen("SALTO", gameplayLayer.transform, new Vector2(.5f,0),
                new Vector2(-265,160), new Vector2(142,142),
                C(45,83,117,220), "<XInputController>/buttonWest");

            // AnyRPG ROLL defaults to R on keyboard. Dash/FURY is a separate upgrade.
            OnScreen("RODAR", gameplayLayer.transform, new Vector2(.5f,0),
                new Vector2(-100,160), new Vector2(142,142),
                C(45,83,117,220), "<Keyboard>/r");

            var interact = UiButton("USAR", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-460,90), new Vector2(135,100),
                C(53,89,115,238));
            interact.onClick.AddListener(PressInteract);

            // Arced four skills; center-right under the camera swipe surface.
            Vector2[] positions = {
                new Vector2(-675,260), new Vector2(-540,365),
                new Vector2(-385,415), new Vector2(-230,365)
            };
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                var button = UiButton("S"+(i+1), gameplayLayer.transform,
                    new Vector2(1,0), positions[i], new Vector2(138,138),
                    C(46,83,151,235));
                button.onClick.AddListener(() => PressSkill(slot));
                skillButtons[i] = button;
                skillTexts[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                skillIcons[i] = InnerIcon(button.transform);
                skillCooldowns[i] = Cooldown(button.transform);
            }

            attackButton = UiButton("ATK", gameplayLayer.transform,
                new Vector2(1,0), new Vector2(-180,165),
                new Vector2(185,185), C(49,97,173,245));
            attackButton.onClick.AddListener(PressAttack);
            attackText = attackButton.GetComponentInChildren<TextMeshProUGUI>();
            attackIcon = InnerIcon(attackButton.transform);

            var menuBtn = UiButton("•••", gameplayLayer.transform,
                Vector2.one, new Vector2(-84,-77),
                new Vector2(120,88), C(40,75,111,235));
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
            menuLayer.SetActive(false);

            backLayer = Layer("Back from native AnyRPG window", canvasGo.transform);
            var back = UiButton("◀ VOLVER", backLayer.transform, Vector2.one,
                new Vector2(-125,-75), new Vector2(220,96), C(48,94,145,255));
            back.onClick.AddListener(CloseNativeWindow);
            backLayer.SetActive(false);
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
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = C(100,176,244,255);
            colors.pressedColor = C(29,119,175,255);
            button.colors = colors;
            Label(text, image, new Vector2(.5f,.5f),
                Vector2.zero, size - new Vector2(8,8), 29);
            return button;
        }

        private static void OnScreen(string title, Transform parent, Vector2 anchor,
            Vector2 pos, Vector2 size, Color bg, string inputControl)
        {
            var rect = Panel(title+" OnScreenInput", parent, bg);
            Place(rect, anchor, pos, size);
            var control = rect.gameObject.AddComponent<OnScreenButton>();
            control.controlPath = inputControl;
            Label(title, rect, new Vector2(.5f,.5f),
                Vector2.zero, size, 27);
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
