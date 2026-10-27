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
        private TMP_Text bannerTitle;

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
            rect.sizeDelta = new Vector2(780,75);
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
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
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
            questBanner = banner;
            questBanner.SetActive(false);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .45f;
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
                questBanner.SetActive(false);
                return;
            }

            // Translate fixed UI labels only. Proper names and dynamic quest
            // narrative are deliberately untouched to avoid fake translations.
            var labels = current.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                string key = label.text.Trim();
                if (Words.TryGetValue(key, out var translated) &&
                    !string.Equals(key, translated, StringComparison.Ordinal))
                    label.text = translated;
            }
        }

        private void StyleWindow(CloseableWindowContents window)
        {
            if (window == null) return;
            string kind = (window.GetType().Name + " " + window.gameObject.name).ToLowerInvariant();
            bool quest = kind.Contains("quest") || kind.Contains("dialog") ||
                kind.Contains("interact") || kind.Contains("conversation");
            if (questBanner != null)
            {
                bannerTitle.text = quest ? "SISTEMA  ·  MISIÓN / DIÁLOGO" : "HIGHFLY  ·  SISTEMA";
                questBanner.SetActive(quest);
            }

            enlarged = window.RectTransform;
            if (enlarged != null)
            {
                originalScale = enlarged.localScale;
                // Larger, readable landscape window; avoid modifying anchors,
                // button positions, and upstream quest scripts.
                enlarged.localScale = originalScale * (quest ? 1.20f : 1.10f);
            }
            var background = window.BackGroundImage;
            if (background != null && background.GetComponent<Outline>() == null)
            {
                var border = background.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(.12f,.70f,.94f,.88f);
                border.effectDistance = new Vector2(3,-3);
                var shadow = background.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0,0,0,.72f);
                shadow.effectDistance = new Vector2(7,-9);
            }
        }

        private void RestorePrevious()
        {
            if (enlarged != null) enlarged.localScale = originalScale;
            enlarged = null;
            questBanner?.SetActive(false);
        }

        private void OnDisable() { RestorePrevious(); }
        private void OnDestroy() { RestorePrevious(); }
    }
}
