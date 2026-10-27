// HIGHFLY mobile-only Spanish labels and premium native RPG modal frame.
// Preserves AnyRPG quest logic / buttons / rewards. Translation of custom
// quest dialogue text still requires translated data, not guesswork.
using System;
using System.Collections.Generic;
using AnyRPG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HIGHFLY.Mobile
{
    public sealed class HighflySpanishMobileUI : MonoBehaviour
    {
        private SystemGameManager manager;
        private CloseableWindowContents current;
        private RectTransform enlarged;
        private Vector3 originalScale;
        private float nextRefresh;
        private GameObject questBanner;
        private const string PremiumSurfaceName = "HIGHFLY A6 PREMIUM NATIVE FRAME";
        private TMP_Text bannerTitle;
        private TMP_Text bannerSubtitle;
        private bool premiumModal;
        private CloseableWindow nativeWindow;

        private static readonly Dictionary<string,string> Words =
            new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Play", "Jugar"}, {"Play Offline", "Jugar sin conexión"},
            {"Continue", "Continuar"}, {"New Game", "Nueva partida"},
            {"New Character", "Nuevo personaje"}, {"Start Game", "Entrar al mundo"},
            {"Character", "Personaje"}, {"Characters", "Personajes"},
            {"Inventory", "Inventario"}, {"Items", "Objetos"},
            {"Skills", "Habilidades"}, {"Abilities", "Habilidades"},
            {"Ability", "Habilidad"}, {"Quest", "Misión"},
            {"Quests", "Misiones"}, {"Quest Log", "Diario de misiones"},
            {"Quest Details", "Detalles de misión"}, {"Available Quests", "Misiones disponibles"},
            {"Accept", "Aceptar"}, {"Accept Quest", "Aceptar misión"},
            {"Complete", "Completar"}, {"Complete Quest", "Completar misión"},
            {"Turn In", "Entregar"}, {"Decline", "Rechazar"},
            {"Cancel", "Cancelar"}, {"Close", "Cerrar"},
            {"Back", "Volver"}, {"Return", "Volver"},
            {"Next", "Siguiente"}, {"Previous", "Anterior"},
            {"OK", "Aceptar"}, {"Yes", "Sí"}, {"No", "No"},
            {"Settings", "Opciones"}, {"Options", "Opciones"},
            {"Language", "Idioma"}, {"Controls", "Controles"},
            {"Video", "Video"}, {"Sound", "Sonido"},
            {"Key Bindings", "Asignación de teclas"},
            {"Map", "Mapa"}, {"World Map", "Mapamundi"},
            {"Crafting", "Fabricación"}, {"Craft", "Fabricar"},
            {"Achievements", "Logros"}, {"Achievements Book", "Logros"},
            {"Equipment", "Equipamiento"}, {"Equip", "Equipar"},
            {"Unequip", "Desequipar"}, {"Weapon", "Arma"},
            {"Armor", "Armadura"}, {"Sell", "Vender"},
            {"Buy", "Comprar"}, {"Shop", "Tienda"},
            {"Vendor", "Comerciante"}, {"Trade", "Intercambiar"},
            {"Interaction", "Interacción"}, {"Interact", "Interactuar"},
            {"Talk", "Hablar"}, {"Talk To", "Hablar con"},
            {"Rewards", "Recompensas"}, {"Reward", "Recompensa"},
            {"Objectives", "Objetivos"}, {"Objective", "Objetivo"},
            {"Description", "Descripción"}, {"Progress", "Progreso"},
            {"Required Level", "Nivel requerido"}, {"Level", "Nivel"},
            {"Experience", "Experiencia"}, {"Gold", "Oro"},
            {"Health", "Salud"}, {"Mana", "Maná"}, {"Energy", "Energía"},
            {"Damage", "Daño"}, {"Attack", "Ataque"},
            {"Defense", "Defensa"}, {"Strength", "Fuerza"},
            {"Agility", "Agilidad"}, {"Intelligence", "Inteligencia"},
            {"Range", "Alcance"}, {"Cooldown", "Recarga"},
            {"Cast", "Lanzar"}, {"Apply", "Aplicar"},
            {"Help", "Ayuda"}, {"Main Menu", "Menú principal"},
            {"Exit Game", "Salir del juego"}, {"Save", "Guardar"},
            {"Load", "Cargar"}, {"Loading", "Cargando"},
            {"Ready", "Listo"}, {"On", "Activado"},
            {"Off", "Desactivado"}, {"Full Screen", "Pantalla completa"},
            {"Resolution", "Resolución"}, {"Graphics Quality", "Calidad gráfica"},
            {"Master Volume", "Volumen general"}, {"Music Volume", "Volumen de música"},
            {"Music", "Música"}, {"Effects Volume", "Volumen de efectos"},
            {"Male", "Masculino"}, {"Female", "Femenino"},
            {"Name", "Nombre"}, {"Faction", "Facción"},
            {"Class", "Clase"}, {"Race", "Raza"},
            {"Location", "Ubicación"}, {"Difficulty", "Dificultad"},
            {"Active", "Activo"}, {"Completed", "Completado"},
            {"Unavailable", "No disponible"}, {"Learn", "Aprender"},
            {"Requirements", "Requisitos"}, {"Not Enough Mana", "Maná insuficiente"},
            {"No Target", "Sin objetivo"}, {"Interact With", "Interactuar con"},
            {"Quest Completed", "Misión completada"},
            {"New Quest", "Nueva misión"}, {"Quest Accepted", "Misión aceptada"},
            {"Quest Updated", "Misión actualizada"}, {"Quest Failed", "Misión fallida"},
            {"Continue Game", "Continuar partida"}, {"Create", "Crear"}
        };

        private void Awake()
        {
            var banner = new GameObject("HIGHFLY System Quest Banner",
                typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(transform, false);
            var rect = banner.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,1f);
            rect.pivot = new Vector2(.5f,1f);
            rect.anchoredPosition = new Vector2(0,-20);
            rect.sizeDelta = new Vector2(980,112);
            var bg = banner.GetComponent<Image>();
            bg.color = new Color(.027f,.09f,.16f,.93f);
            bg.raycastTarget = false;
            var edge = banner.AddComponent<Outline>();
            edge.effectColor = new Color(.20f,.80f,1f,.85f);
            edge.effectDistance = new Vector2(2,-2);

            var title = new GameObject("System Mission Title",
                typeof(RectTransform),typeof(TextMeshProUGUI));
            title.transform.SetParent(banner.transform,false);
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(18,39); tr.offsetMax = new Vector2(-18,-6);
            var label = title.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.73f,.94f,1f,1f);
            label.fontSize = 31;
            label.enableAutoSizing = true;
            label.fontSizeMin = 21;
            label.fontSizeMax = 31;
            label.raycastTarget = false;
            label.text = "HIGHFLY  ·  SISTEMA";
            bannerTitle = label;

            var note = new GameObject("Native mission status and navigation",
                typeof(RectTransform),typeof(TextMeshProUGUI));
            note.transform.SetParent(banner.transform,false);
            var noteRect = note.GetComponent<RectTransform>();
            noteRect.anchorMin = Vector2.zero; noteRect.anchorMax = Vector2.one;
            noteRect.offsetMin = new Vector2(18,8); noteRect.offsetMax = new Vector2(-18,-68);
            var small = note.GetComponent<TextMeshProUGUI>();
            small.fontSize = 22;
            small.enableAutoSizing = true;
            small.fontSizeMin = 17;
            small.fontSizeMax = 22;
            small.alignment = TextAlignmentOptions.Center;
            small.color = new Color(.73f,.82f,.94f,1f);
            small.raycastTarget = false;
            small.text = "Tu historia · Misiones · Recompensas";
            bannerSubtitle = small;
            questBanner = banner;
            questBanner.SetActive(false);
        }


        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .33f;
            if (manager == null)
                manager = FindFirstObjectByType<SystemGameManager>(FindObjectsInactive.Include);
            if (manager == null || manager.WindowManager == null ||
                manager.PlayerManagerClient == null ||
                manager.PlayerManagerClient.UnitController == null)
                return;

            var active = manager.WindowManager.CurrentWindow;
            if (active != current)
            {
                RestorePrevious();
                current = active;
                if (current != null) StyleWindow(current);
            }

            if (current == null)
            {
                if (questBanner != null) questBanner.SetActive(false);
                return;
            }

            TranslateVisibleLabels();
            if (premiumModal) RefreshQuestHeader();
            // Native interaction choices are pooled. Re-style newly spawned
            // buttons without altering their original listeners.
            if (current is InteractionPanel)
                StyleInteractionChoices(current);
        }

        private static Color Tone(byte r, byte g, byte b, byte a = 255)
            => new Color32(r,g,b,a);

        private static void OutlineOnce(GameObject go, Color color, int size)
        {
            if (go == null) return;
            var line = go.GetComponent<Outline>();
            if (line == null) line = go.AddComponent<Outline>();
            line.effectColor = color;
            line.effectDistance = new Vector2(size,-size);
        }

        private static void ShadowOnce(GameObject go)
        {
            if (go == null || go.GetComponent<Shadow>() != null) return;
            var shade = go.AddComponent<Shadow>();
            shade.effectColor = Tone(0,0,0,190);
            shade.effectDistance = new Vector2(7,-9);
        }

        // The actual AnyRPG buttons retain their existing OnClick handlers,
        // quest prerequisites, selection state and reward validation.
        private static void StyleButton(HighlightButton action, bool primary)
        {
            if (action == null || action.Button == null) return;
            var button = action.Button;
            var element = button.GetComponent<LayoutElement>();
            if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = primary ? 73f : 62f;
            element.preferredHeight = primary ? 82f : 68f;

            var graphic = button.targetGraphic as Image;
            if (graphic != null)
            {
                graphic.color = primary ? Tone(13,112,165,248) : Tone(17,59,101,239);
                OutlineOnce(graphic.gameObject,
                    primary ? Tone(82,220,255,235) : Tone(52,138,205,195),2);
                ShadowOnce(graphic.gameObject);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Tone(115,206,252);
            colors.pressedColor = Tone(44,161,218);
            colors.selectedColor = Tone(101,200,253);
            colors.disabledColor = Tone(105,105,125,100);
            button.colors = colors;

            var label = action.Text != null
                ? action.Text : button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Tone(239,248,255);
                label.enableAutoSizing = true;
                label.fontSize = Mathf.Max(26, label.fontSize);
                label.fontSizeMin = 18;
                label.fontSizeMax = 35;
            }
        }

        private static void StyleInteractionChoices(CloseableWindowContents window)
        {
            var buttons = window.GetComponentsInChildren<HighlightButton>(true);
            foreach (var button in buttons)
                StyleButton(button, false);
        }

        private void StyleWindow(CloseableWindowContents window)
        {
            if (window == null) return;
            premiumModal = window is DialogPanel ||
                window is QuestGiverPanel || window is InteractionPanel;
            nativeWindow = window.GetComponentInParent<CloseableWindow>();
            // A6.2 integrates the title within the native premium frame.
            // No floating old banner on top of the world or outside the panel.
            if (questBanner != null) questBanner.SetActive(false);

            // Scale the REAL native window, not its children (which caused
            // layout mismatches in earlier Android experiments).
            enlarged = nativeWindow != null && nativeWindow.RectTransform != null
                ? nativeWindow.RectTransform : window.RectTransform;
            if (enlarged != null)
            {
                originalScale = enlarged.localScale;
                // More of the landscape surface for desktop-authored AnyRPG UI.
                bool largeRpg = window is InventoryPanel || window is CharacterPanel ||
                    window is QuestLogPanel || window is CraftingPanel;
                float factor = premiumModal ? 1.55f : largeRpg ? 1.78f : 1.48f;
                float width = Mathf.Abs(enlarged.rect.width * originalScale.x);
                float height = Mathf.Abs(enlarged.rect.height * originalScale.y);
                if (width > 1f)
                    factor = Mathf.Min(factor, 2160f / width);
                if (height > 1f)
                    factor = Mathf.Min(factor, 930f / height);
                factor = Mathf.Clamp(factor, .80f, 1.9f);
                enlarged.localScale = originalScale * factor;
            }

            ApplyPremiumWindowFrame(window);
            var background = nativeWindow != null &&
                nativeWindow.MobileWindowBackground != null
                ? nativeWindow.MobileWindowBackground : window.BackGroundImage;
            if (background != null)
            {
                if (premiumModal) background.color = Tone(7,23,46,249);
                OutlineOnce(background.gameObject,
                    premiumModal ? Tone(69,196,245,230) : Tone(38,108,175,190),
                    premiumModal ? 3 : 2);
                ShadowOnce(background.gameObject);
            }
            if (window is InventoryPanel)
            {
                // Large touch-friendly slots, native bag drag/drop retained.
                foreach (var grid in window.GetComponentsInChildren<GridLayoutGroup>(true))
                {
                    if (grid.cellSize.x < 72f || grid.cellSize.y < 72f)
                        grid.cellSize = new Vector2(Mathf.Max(72f,grid.cellSize.x),
                            Mathf.Max(72f,grid.cellSize.y));
                    grid.spacing = new Vector2(Mathf.Max(6f,grid.spacing.x),
                        Mathf.Max(6f,grid.spacing.y));
                }
            }
            if (!premiumModal)
            {
                // Full UI typography for smartphone landscape: scale the native
                // RPG text, but never modify gameplay content or quest identifiers.
                foreach (var txt in window.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (txt == null) continue;
                    txt.enableAutoSizing = true;
                    txt.fontSize = Mathf.Max(txt.fontSize, 23f);
                    txt.fontSizeMin = Mathf.Max(txt.fontSizeMin, 16f);
                    txt.fontSizeMax = Mathf.Max(txt.fontSizeMax, 27f);
                }
                foreach (var action in window.GetComponentsInChildren<HighlightButton>(true))
                    StyleButton(action,false);
                return;
            }

            if (window is DialogPanel dialog)
            {
                StyleButton(dialog.MobileContinue, true);
                StyleButton(dialog.MobileViewQuest, false);
                StyleButton(dialog.MobileAcceptQuest, true);
                if (dialog.MobileNpcPortrait != null)
                    OutlineOnce(dialog.MobileNpcPortrait.gameObject, Tone(80,204,251,230), 3);
                if (dialog.MobileNpcSnapshot != null)
                    OutlineOnce(dialog.MobileNpcSnapshot.gameObject, Tone(80,204,251,230), 3);
                if (dialog.MobileNpcName != null)
                {
                    dialog.MobileNpcName.color = Tone(96,218,255);
                    dialog.MobileNpcName.fontSize = Mathf.Max(29, dialog.MobileNpcName.fontSize);
                }
                if (dialog.MobileDialogBody != null)
                {
                    dialog.MobileDialogBody.color = Tone(237,246,255);
                    dialog.MobileDialogBody.enableAutoSizing = true;
                    dialog.MobileDialogBody.fontSize = 32;
                    dialog.MobileDialogBody.fontSizeMin = 22;
                    dialog.MobileDialogBody.fontSizeMax = 34;
                }
            }
            else if (window is QuestGiverPanel quest)
            {
                StyleButton(quest.MobileAcceptQuest,true);
                StyleButton(quest.MobileCompleteQuest,true);
                if (quest.MobileQuestDetails != null)
                {
                    var detailTexts = quest.MobileQuestDetails.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var value in detailTexts)
                        if (value != null)
                        {
                            value.color = Tone(229,240,250);
                            value.enableAutoSizing = true;
                            value.fontSizeMax = Mathf.Max(value.fontSize,28);
                            value.fontSizeMin = Mathf.Min(value.fontSizeMax,19);
                        }
                }
            }
            else if (window is InteractionPanel)
                StyleInteractionChoices(window);
            RefreshQuestHeader();
            PositionBannerAboveActiveWindow();
        }

        // A5's banner floated at the top of the display, disconnected from the
        // NPC window. Position it directly above that native panel in landscape.
        private void PositionBannerAboveActiveWindow()
        {
            if (questBanner == null || !premiumModal || enlarged == null) return;
            var banner = questBanner.transform as RectTransform;
            var hudRoot = banner != null ? banner.parent as RectTransform : null;
            if (hudRoot == null) return;
            var corners = new Vector3[4];
            enlarged.GetWorldCorners(corners);
            Vector3 above = (corners[1] + corners[2]) * .5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, above);
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hudRoot, screenPoint, null, out localPoint)) return;
            banner.anchorMin = banner.anchorMax = new Vector2(.5f,.5f);
            banner.anchoredPosition = new Vector2(
                Mathf.Clamp(localPoint.x, -hudRoot.rect.width*.5f+510f,
                    hudRoot.rect.width*.5f-510f),
                Mathf.Clamp(localPoint.y + 57f,
                    -hudRoot.rect.height*.5f+80f, hudRoot.rect.height*.5f-65f));
        }


        // Non-blocking native skin: original AnyRPG quest buttons, inventory
        // slots, rewards, focus, close handling and saves stay in their own UI.
        // The large shell is drawn behind them; old desktop window frames
        // are not rendered as a second overlay.
        private void ApplyPremiumWindowFrame(CloseableWindowContents window)
        {
            if (window == null || enlarged == null) return;
            RectTransform holder = enlarged;
            var found = holder.Find(PremiumSurfaceName);
            if (found != null) return;
            var shell = new GameObject(PremiumSurfaceName,
                typeof(RectTransform),typeof(Image),typeof(Outline));
            shell.transform.SetParent(holder,false);
            shell.transform.SetAsFirstSibling();
            var rect = shell.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-8,-8);
            rect.offsetMax = new Vector2(8,8);
            var graphic = shell.GetComponent<Image>();
            graphic.color = Tone(8,20,42,244);
            graphic.raycastTarget = false;
            var line = shell.GetComponent<Outline>();
            line.effectColor = Tone(53,191,240,229);
            line.effectDistance = new Vector2(3,-3);
            var bar = new GameObject("HIGHFLY platinum header",
                typeof(RectTransform),typeof(Image));
            bar.transform.SetParent(shell.transform,false);
            var br = bar.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0,1);br.anchorMax = new Vector2(1,1);
            br.pivot = new Vector2(.5f,1);
            br.anchoredPosition = new Vector2(0,0);
            br.sizeDelta = new Vector2(-3,58);
            var headerBg = bar.GetComponent<Image>();
            headerBg.color = Tone(13,70,115,230);
            headerBg.raycastTarget = false;
            var label = new GameObject("HIGHFLY header copy",
                typeof(RectTransform),typeof(TextMeshProUGUI));
            label.transform.SetParent(bar.transform,false);
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;
            lr.offsetMin=new Vector2(22,4);lr.offsetMax=new Vector2(-10,-4);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.fontSize=28;
            tmp.enableAutoSizing=true;
            tmp.fontSizeMin=18;
            tmp.fontSizeMax=30;
            tmp.alignment=TextAlignmentOptions.MidlineLeft;
            tmp.color=Tone(214,244,255);
            tmp.raycastTarget=false;
            tmp.text = window is QuestGiverPanel ? "HIGHFLY · MISIONES Y RECOMPENSAS" :
                window is DialogPanel ? "HIGHFLY · DIÁLOGO" :
                window is InteractionPanel ? "HIGHFLY · INTERACCIÓN" :
                window is InventoryPanel ? "HIGHFLY · INVENTARIO" :
                window is CharacterPanel ? "HIGHFLY · PERSONAJE" :
                window is QuestLogPanel ? "HIGHFLY · DIARIO DE MISIONES" :
                window is CraftingPanel ? "HIGHFLY · FABRICACIÓN" :
                "HIGHFLY · SISTEMA";
        }

        private void RefreshQuestHeader()
        {
            if (bannerTitle == null || current == null) return;
            string title = "HIGHFLY  ·  SISTEMA";
            string subtitle = "NPC · Conversaciones, misiones y recompensas";
            if (current is DialogPanel dialog)
            {
                string name = dialog.MobileNpcName != null
                    ? dialog.MobileNpcName.text : "";
                if (!string.IsNullOrWhiteSpace(name))
                    title = "NPC  ·  " + name;
                subtitle = "Continuar diálogo · Ver misión · Aceptar misión";
            }
            else if (current is QuestGiverPanel quest)
            {
                title = "SISTEMA  ·  MISIÓN";
                var chosen = quest.MobileCurrentQuest;
                if (chosen != null)
                {
                    title = "MISIÓN  ·  " + chosen.DisplayName;
                    string status = chosen.GetStatus(manager.PlayerManagerClient.UnitController);
                    if (status == "complete")
                        subtitle = "Objetivo completado · Reclamá tu recompensa";
                    else if (status == "inprogress")
                        subtitle = "Misión en curso · Revisá los objetivos";
                    else
                        subtitle = "Revisá los requisitos y aceptá la misión";
                }
                else
                    subtitle = "Elegí un encargo para ver objetivos y recompensas";
            }
            else if (current is InteractionPanel panel)
            {
                var actor = panel.MobileInteractable;
                if (actor != null) title = "NPC  ·  " + actor.DisplayName;
                subtitle = "Elegí con quién hablar, qué misión aceptar o qué usar";
            }
            bannerTitle.text = title;
            if (bannerSubtitle != null) bannerSubtitle.text = subtitle;
        }

        private void TranslateVisibleLabels()
        {
            var labels = current.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                string key = label.text.Trim();
                if (Words.TryGetValue(key, out var translated) &&
                    !string.Equals(key, translated, StringComparison.Ordinal))
                    label.text = translated;
                else if (key.EndsWith(" (Complete)",StringComparison.Ordinal))
                    label.text = key.Substring(0,key.Length-11) + " (Completada)";
            }
        }

        private void RestorePrevious()
        {
            if (enlarged != null) enlarged.localScale = originalScale;
            enlarged = null;
            nativeWindow = null;
            premiumModal = false;
            if (questBanner != null) questBanner.SetActive(false);
        }

        private void OnDisable() { RestorePrevious(); }
        private void OnDestroy() { RestorePrevious(); }
    }
}
