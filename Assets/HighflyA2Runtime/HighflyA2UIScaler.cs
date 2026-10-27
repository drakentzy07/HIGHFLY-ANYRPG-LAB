using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Highfly
{
    // Opt-in reversible UI scaling for touchscreen WebGL. No prefab edits.
    public sealed class HighflyA2UIScaler : MonoBehaviour
    {
        private struct Original
        {
            public CanvasScaler.ScaleMode mode;
            public Vector2 resolution;
            public CanvasScaler.ScreenMatchMode matchMode;
            public float match;
            public float constantScale;
        }

        private readonly Dictionary<CanvasScaler, Original> previous = new Dictionary<CanvasScaler, Original>();
        private bool mobile = false;
        private float checkAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GameObject.Find("HighflyA2UIScaler") != null) return;
            GameObject go = new GameObject("HighflyA2UIScaler");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyA2UIScaler>();
#endif
        }

        public void SetUIMode(string mode)
        {
            bool requested = mode == "MOBILE";
            if (mobile == requested && previous.Count > 0) return;
            mobile = requested;
            Apply();
        }

        private void Update()
        {
            if (Time.unscaledTime < checkAt) return;
            checkAt = Time.unscaledTime + 1.5f;
            Apply();
        }

        private void Apply()
        {
            if (!mobile)
            {
                foreach (var kv in previous)
                {
                    CanvasScaler s = kv.Key;
                    if (s == null) continue;
                    var v = kv.Value;
                    s.uiScaleMode = v.mode;
                    s.referenceResolution = v.resolution;
                    s.screenMatchMode = v.matchMode;
                    s.matchWidthOrHeight = v.match;
                    s.scaleFactor = v.constantScale;
                }
                previous.Clear();
                return;
            }
            int changed = 0;
            foreach (CanvasScaler s in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s == null || s.gameObject == gameObject) continue;
                var c = s.GetComponent<Canvas>();
                if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
                if (!previous.ContainsKey(s))
                {
                    previous.Add(s, new Original
                    {
                        mode = s.uiScaleMode,
                        resolution = s.referenceResolution,
                        matchMode = s.screenMatchMode,
                        match = s.matchWidthOrHeight,
                        constantScale = s.scaleFactor
                    });
                    changed++;
                }
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1600, 900);
                s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                s.matchWidthOrHeight = 0.5f;
            }
            if (changed > 0) Debug.Log("[HIGHFLY A2.2] Mobile CanvasScaler configured: " + changed + " canvases.");
        }
    }
}
