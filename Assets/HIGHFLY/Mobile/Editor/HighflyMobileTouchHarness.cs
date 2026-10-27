// HIGHFLY A2 - opt-in editor-only touch harness.
// Creates a non-destructive test Canvas. Run from an open gameplay scene.
// Do not use its plain test graphics as the final HIGHFLY premium HUD.
#if UNITY_EDITOR
using HIGHFLY.Mobile;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HIGHFLY.Mobile.Editor
{
    public static class HighflyMobileTouchHarness
    {
        private const string RootName = "HIGHFLY Mobile Touch Test (A2)";

        [MenuItem("HIGHFLY/Mobile/Create A2 Touch Test Harness")]
        public static void Create()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning("HIGHFLY A2 touch test already exists in this scene.");
                return;
            }

            var canvasObject = new GameObject(
                RootName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create HIGHFLY Touch Test");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2340, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // The camera surface must be created before button/joystick zones,
            // so later UI controls can appear above it in the hierarchy.
            var look = NewArea("Swipe Camera (right half)", canvasObject.transform,
                new Color(0f, 0f, 0f, 0.001f));
            look.anchorMin = new Vector2(0.5f, 0f);
            look.anchorMax = Vector2.one;
            look.offsetMin = Vector2.zero;
            look.offsetMax = Vector2.zero;
            look.gameObject.AddComponent<HighflyTouchLookArea>();

            var stick = NewArea("Move (left thumb)", canvasObject.transform,
                new Color(0.06f, 0.17f, 0.25f, 0.50f));
            stick.anchorMin = Vector2.zero;
            stick.anchorMax = Vector2.zero;
            stick.pivot = new Vector2(0.5f, 0.5f);
            stick.sizeDelta = new Vector2(260f, 260f);
            stick.anchoredPosition = new Vector2(210f, 205f);

            var knob = NewArea("Movement indicator (not a hit target)", stick,
                new Color(0.16f, 0.65f, 0.85f, 0.84f));
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(84f, 84f);
            knob.anchoredPosition = Vector2.zero;
            knob.GetComponent<Image>().raycastTarget = false;

            var move = stick.gameObject.AddComponent<HighflyTouchMoveArea>();
            var serialized = new SerializedObject(move);
            serialized.FindProperty("knob").objectReferenceValue = knob;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EventSystem existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing == null)
            {
                var eventObject = new GameObject(
                    "HIGHFLY InputSystem EventSystem", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(eventObject, "Create HIGHFLY EventSystem");
            }
            else if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning(
                    "HIGHFLY A2: existing EventSystem is not using InputSystemUIInputModule. " +
                    "Inspect the active UI input module before testing multi-touch.");
            }

            Selection.activeGameObject = canvasObject;
            EditorSceneManager.MarkSceneDirty(canvasObject.scene);
            Debug.Log(
                "HIGHFLY A2 touch harness added to this scene. Set AnyRPG GamePad " +
                "control mode and verify leftStick/rightStick input bindings. " +
                "No skill buttons or production HUD have been installed.");
        }

        private static RectTransform NewArea(
            string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return (RectTransform)go.transform;
        }
    }
}
#endif
