using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Highfly
{
    // A2.1 graphics isolation test: does not alter any AnyRPG scenes, NPCs, or gameplay.
    // Its safe profile can be toggled back to original quality from the mobile WebGL page.
    public sealed class A2GraphicsDiagnostics : MonoBehaviour
    {
        private bool safe = true;
        private bool showDiagnostics = true;
        private int originalQuality;
        private Camera cameraUnderTest;
        private CameraClearFlags savedClearFlags;
        private Color savedBackground;
        private bool savedHDR;
        private bool savedOcclusion;
        private bool savedPostProcessing;
        private bool hasSavedCamera;
        private float nextCheck;
        private string diagnostics = "Recopilando diagnóstico de la cámara...";
        private GUIStyle diagnosticStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GameObject.Find("HighflyA2GraphicsDiagnostics") != null) return;
            var go = new GameObject("HighflyA2GraphicsDiagnostics");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<A2GraphicsDiagnostics>();
#endif
        }

        private void Awake()
        {
            originalQuality = QualitySettings.GetQualityLevel();
            ApplyQuality();
        }

        public void SetGraphicsMode(string mode)
        {
            bool next = !string.Equals(mode, "ORIGINAL", System.StringComparison.OrdinalIgnoreCase);
            if (safe == next) return;
            RestoreCamera();
            safe = next;
            ApplyQuality();
            nextCheck = 0;
        }

        public void SetDiagnostics(string mode)
        {
            showDiagnostics = mode != "0";
        }

        private void ApplyQuality()
        {
            if (QualitySettings.names.Length == 0) return;
            int target = safe ? Mathf.Min(1, QualitySettings.names.Length - 1) : originalQuality;
            if (QualitySettings.GetQualityLevel() != target)
                QualitySettings.SetQualityLevel(target, true);
            Debug.Log("[HIGHFLY A2.1] Graphics mode: " + (safe ? "SAFE" : "ORIGINAL") +
                " / quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 3f;
            Camera cam = Camera.main;
            if (cam != cameraUnderTest)
            {
                RestoreCamera();
                cameraUnderTest = cam;
                hasSavedCamera = false;
            }
            if (cam != null && safe) ApplySafeCamera(cam);
            RebuildDiagnostics(cam);
        }

        private void ApplySafeCamera(Camera cam)
        {
            var post = cam.GetComponent<UniversalAdditionalCameraData>();
            if (!hasSavedCamera)
            {
                savedClearFlags = cam.clearFlags;
                savedBackground = cam.backgroundColor;
                savedHDR = cam.allowHDR;
                savedOcclusion = cam.useOcclusionCulling;
                savedPostProcessing = post != null && post.renderPostProcessing;
                hasSavedCamera = true;
            }

            // An intentionally blue clear makes it easy to tell "no world meshes"
            // from "camera not rendering / another full-screen canvas covers it".
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.19f, 0.30f, 1f);
            cam.allowHDR = false;
            cam.useOcclusionCulling = false;
            if (post != null) post.renderPostProcessing = false;
        }

        private void RestoreCamera()
        {
            if (cameraUnderTest != null && hasSavedCamera)
            {
                cameraUnderTest.clearFlags = savedClearFlags;
                cameraUnderTest.backgroundColor = savedBackground;
                cameraUnderTest.allowHDR = savedHDR;
                cameraUnderTest.useOcclusionCulling = savedOcclusion;
                var post = cameraUnderTest.GetComponent<UniversalAdditionalCameraData>();
                if (post != null) post.renderPostProcessing = savedPostProcessing;
            }
            hasSavedCamera = false;
        }

        private void RebuildDiagnostics(Camera cam)
        {
            string pipelineName = GraphicsSettings.currentRenderPipeline == null
                ? "BUILT-IN"
                : GraphicsSettings.currentRenderPipeline.name;
            int meshes = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length;
            int skinned = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Length;
            int terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length;
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            string camInfo = cam == null ? "CAMERA MAIN = NULL" :
                "cam=" + cam.name +
                " enabled=" + cam.enabled +
                " active=" + cam.gameObject.activeInHierarchy +
                " mask=" + cam.cullingMask +
                " pos=" + cam.transform.position.ToString("F0") +
                " targetRT=" + (cam.targetTexture != null);
            diagnostics =
                "A2.1 [" + (safe ? "GFX SEGURO" : "GFX ORIGINAL") + "] / " +
                SceneManager.GetActiveScene().name + "\n" +
                camInfo + "\n" +
                "Mesh=" + meshes + " Skinned=" + skinned + " Terrain=" + terrains +
                " CamCount=" + cameras + "\n" +
                "URP=" + pipelineName + "  Quality=" +
                QualitySettings.names[QualitySettings.GetQualityLevel()];
            Debug.Log("[HIGHFLY A2.1] " + diagnostics.Replace("\n", " | "));
        }

        private void OnGUI()
        {
            if (!showDiagnostics) return;
            if (diagnosticStyle == null)
            {
                diagnosticStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = Mathf.Clamp(Screen.height / 66, 11, 16),
                    wordWrap = false
                };
                diagnosticStyle.normal.textColor = Color.white;
            }
            float width = Mathf.Min(Screen.width * 0.62f, 850f);
            float height = Mathf.Min(Screen.height * 0.23f, 100f);
            GUI.Box(new Rect((Screen.width - width) / 2f, 85f, width, height), diagnostics, diagnosticStyle);
        }

        private void OnDestroy()
        {
            RestoreCamera();
        }
    }
}
