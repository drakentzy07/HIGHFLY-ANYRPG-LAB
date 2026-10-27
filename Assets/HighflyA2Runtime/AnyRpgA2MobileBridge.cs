using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Highfly
{
    // Browser touch UI feeds a real Input System virtual gamepad.
    // AnyRPG already reads Gamepad.current (left/right sticks, buttons) for movement and menus.
    public sealed class AnyRpgA2MobileBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HF_A2_NotifyScene(string scene);
#endif
        private Gamepad virtualPad;
        private string lastScene = "";
        private float nextSceneCheck = 0f;
        private Vector2 leftStick;
        private Vector2 rightStick;
        private readonly HashSet<string> down = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GameObject.Find("HighflyA2MobileBridge") != null) return;
            var go = new GameObject("HighflyA2MobileBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<AnyRpgA2MobileBridge>();
#endif
        }

        private void Awake()
        {
            virtualPad = InputSystem.AddDevice<Gamepad>("Gamepad");
            Debug.Log("[HIGHFLY A2] Virtual mobile gamepad registered.");
            ReportScene();
        }

        private void ReportScene()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var name = SceneManager.GetActiveScene().name;
            if (name == lastScene) return;
            lastScene = name;
            HF_A2_NotifyScene(name);
#endif
        }

        // Format: "x,y" with dot decimal separator; browser joystick range [-1, 1].
        public void SetStick(string coordinates)
        {
            leftStick = ParseStick(coordinates);
        }

        public void SetCamera(string coordinates)
        {
            rightStick = ParseStick(coordinates);
        }

        // Format: "A:1" for down, "A:0" for release.
        public void SetAction(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            string[] parts = value.Split(':');
            if (parts.Length != 2) return;
            if (parts[1] == "1") down.Add(parts[0]);
            else down.Remove(parts[0]);
        }

        private static Vector2 ParseStick(string s)
        {
            if (string.IsNullOrEmpty(s)) return Vector2.zero;
            var components = s.Split(',');
            if (components.Length != 2) return Vector2.zero;
            float x, y;
            if (!float.TryParse(components[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return Vector2.zero;
            if (!float.TryParse(components[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void Update()
        {
            if (Time.unscaledTime > nextSceneCheck) { nextSceneCheck = Time.unscaledTime + 0.35f; ReportScene(); }
            if (virtualPad == null) return;
            var state = new GamepadState
            {
                leftStick = leftStick,
                rightStick = rightStick
            };
            state = state.WithButton(GamepadButton.South, down.Contains("A"));
            state = state.WithButton(GamepadButton.East, down.Contains("B"));
            state = state.WithButton(GamepadButton.West, down.Contains("X"));
            state = state.WithButton(GamepadButton.North, down.Contains("Y"));
            state = state.WithButton(GamepadButton.Start, down.Contains("MENU"));
            state = state.WithButton(GamepadButton.LeftShoulder, down.Contains("L"));
            state = state.WithButton(GamepadButton.RightShoulder, down.Contains("R"));
            InputSystem.QueueStateEvent(virtualPad, state);
        }

        private void OnDestroy()
        {
            if (virtualPad != null && virtualPad.added) InputSystem.RemoveDevice(virtualPad);
        }
    }
}
