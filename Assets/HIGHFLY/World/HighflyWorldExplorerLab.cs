using UnityEngine;
using UnityEngine.SceneManagement;
using AnyRPG;

namespace Highfly.World
{
    // A7.1 temporary test-only utility. Uses AnyRPG's OWN player transition API,
    // never SceneManager.LoadScene directly, so player/save/network state is retained.
    // This is not a replacement for fixing individual world portals.
    public sealed class HighflyWorldExplorerLab : MonoBehaviour
    {
        private struct Destination
        {
            public string Label;
            public string Scene;
            public Destination(string label, string scene) { Label = label; Scene = scene; }
        }

        // Explicit allowlist: ONLY scenes supplied by the official A Lost Soul add-on.
        private static readonly Destination[] Areas = {
            new Destination("Soul Forge", "ExampleSoulForge"),
            new Destination("Birch Grove", "ExampleBirchGrove"),
            new Destination("Crystal Mine", "ExampleCrystalMine"),
            new Destination("Lightfury Outpost", "ExampleLightfuryOutpost"),
            new Destination("Mana Seal", "ExampleManaSeal"),
            new Destination("Sun Temple", "ExampleSunTemple"),
            new Destination("Ancient Energy Temple", "ExampleTempleOfAncientEnergy"),
            new Destination("Arena of Trials", "ExampleArenaOfTrials")
        };

        private SystemGameManager manager;
        private float checkAfter;
        private bool expanded;
        private float smoothedDelta = 1f / 30f;
        private string feedback = "";
        private float feedbackUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<HighflyWorldExplorerLab>() != null) return;
            var go = new GameObject("HIGHFLY A7.1 World Explorer (temporary test UI)");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyWorldExplorerLab>();
        }

        private void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Mathf.Clamp(Time.unscaledDeltaTime, .001f, .3f), .08f);
            if (Time.unscaledTime < checkAfter) return;
            checkAfter = Time.unscaledTime + .75f;
            if (manager == null)
                manager = FindFirstObjectByType<SystemGameManager>(FindObjectsInactive.Include);
        }

        private bool CanTravel()
        {
            return manager != null &&
                manager.GameMode == GameMode.Local &&
                manager.PlayerManagerServer != null &&
                manager.PlayerManagerClient != null &&
                manager.PlayerManagerClient.UnitController != null &&
                manager.LevelManagerClient != null &&
                !manager.LevelManagerClient.LoadingLevel;
        }

        private void TravelTo(Destination destination)
        {
            if (!CanTravel()) { Note("Personaje no listo"); return; }
            if (!Application.CanStreamedLevelBeLoaded(destination.Scene))
            {
                Note("NO incluido: " + destination.Label);
                Debug.LogError("[HIGHFLY A7.1] Destination scene not in WebGL build: " + destination.Scene);
                return;
            }
            if (SceneManager.GetActiveScene().name == destination.Scene)
            {
                Note("Ya estas en " + destination.Label);
                return;
            }
            // Same code path as the original LoadSceneComponent portal.
            var unit = manager.PlayerManagerClient.UnitController;
            Debug.Log("[HIGHFLY A7.1] Testing AnyRPG scene transition: " +
                      SceneManager.GetActiveScene().name + " -> " + destination.Scene);
            expanded = false;
            manager.PlayerManagerServer.LoadScene(destination.Scene, unit);
        }

        private void Note(string value)
        {
            feedback = value;
            feedbackUntil = Time.unscaledTime + 5f;
        }

        private void OnGUI()
        {
            if (!CanTravel()) { expanded = false; return; }
            // Diagnostic button stays compact; existing FURY HUD stays untouched.
            float scale = Mathf.Clamp(Screen.height / 1080f, .48f, 1.4f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            const float buttonWidth = 235f;
            float x = w - buttonWidth - 18f;
            float y = 85f;

            if (GUI.Button(new Rect(x, y, buttonWidth, 52f),
                "ZONAS · " + Mathf.RoundToInt(1f / smoothedDelta) + " FPS"))
                expanded = !expanded;
            if (expanded)
            {
                float h = Areas.Length * 51f + 90f;
                GUI.Box(new Rect(x - 17, y + 60, buttonWidth + 17, h), "A7.1 · EXPLORACION");
                var playerController = manager.PlayerManagerClient.PlayerController;
                int nearby = playerController != null && playerController.Interactables != null
                    ? playerController.Interactables.Count : 0;
                GUI.Label(new Rect(x - 5, y + 90, buttonWidth, 38),
                    "USAR detecta: " + nearby + " objetos cercanos");
                for (int i = 0; i < Areas.Length; i++)
                {
                    Destination area = Areas[i];
                    string label = (Application.CanStreamedLevelBeLoaded(area.Scene) ? "" : "[FALTA] ") + area.Label;
                    if (GUI.Button(new Rect(x, y + 131 + i * 51f, buttonWidth - 8, 47f), label))
                        TravelTo(area);
                }
            }
            if (Time.unscaledTime < feedbackUntil)
                GUI.Box(new Rect(x - 180, y + 10, 170, 62), feedback);
            GUI.matrix = old;
        }
    }
}
