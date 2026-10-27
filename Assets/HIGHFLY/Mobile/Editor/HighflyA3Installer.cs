#if UNITY_EDITOR
using HIGHFLY.Mobile;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace HIGHFLY.Mobile.Editor
{
    public static class HighflyA3Installer
    {
        [MenuItem("HIGHFLY/Mobile/Install A3 Native HUD in Open Scene")]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<HighflyAnyRpgMobileHud>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("HIGHFLY A3: a mobile HUD is already installed in this scene.");
                return;
            }

            var go = new GameObject("HIGHFLY A3 Native HUD");
            Undo.RegisterCreatedObjectUndo(go, "Install HIGHFLY Mobile HUD");
            Undo.AddComponent<HighflyAnyRpgMobileHud>(go);

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var goEvent = new GameObject("HIGHFLY Touch EventSystem");
                Undo.RegisterCreatedObjectUndo(goEvent, "Install HIGHFLY Touch EventSystem");
                Undo.AddComponent<EventSystem>(goEvent);
                Undo.AddComponent<InputSystemUIInputModule>(goEvent);
            }
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning("HIGHFLY A3: Existing EventSystem is not using InputSystemUIInputModule. " +
                    "Multi-touch HUD requires this module; switch the module in the inspector " +
                    "and disable the incompatible StandaloneInputModule.");
            }

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("HIGHFLY A3 HUD installed in the open scene. " +
                "Input actions, actionbar assignments and device compilation still require testing. " +
                "A1 and A2 branches remain untouched.");
        }

        [MenuItem("HIGHFLY/Mobile/Verify A3 scene setup")]
        private static void Verify()
        {
            var h = Object.FindFirstObjectByType<HighflyAnyRpgMobileHud>(FindObjectsInactive.Include);
            var e = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            Debug.Log($"HIGHFLY A3 scene audit: HUD={(h != null ? "yes" : "NO")}; " +
                $"EventSystem={(e != null ? "yes" : "NO")}; " +
                $"InputSystemUIInputModule={(e != null && e.GetComponent<InputSystemUIInputModule>() != null ? "yes" : "NO")}");
        }
    }
}
#endif
