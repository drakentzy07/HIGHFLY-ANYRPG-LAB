// HIGHFLY A2 - experimental mobile input adapters.
// MIT-compatible original code, isolated from AnyRPG engine sources.
// Add to UI Images with raycastTarget enabled and an InputSystemUIInputModule.
using UnityEngine;
using AnyRPG;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace HIGHFLY.Mobile
{
    /// <summary>
    /// Large fixed-area analog stick. Feeds a virtual Gamepad left stick;
    /// it does not replace or modify the AnyRPG movement controller.
    /// </summary>
    [AddComponentMenu("HIGHFLY/Mobile/Touch Move Area")]
    public sealed class HighflyTouchMoveArea : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, InputControl(layout = "Vector2")]
        private string inputControlPath = "<Gamepad>/leftStick";

        [SerializeField] private RectTransform knob;
        [SerializeField, Min(1f)] private float radius = 94f;
        [SerializeField, Range(0f, 0.4f)] private float deadZone = 0.07f;

        private int activePointer = int.MinValue;

        protected override string controlPathInternal
        {
            get { return inputControlPath; }
            set { inputControlPath = value; }
        }

        public void SetKnob(RectTransform value) { knob = value; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != int.MinValue) return;
            activePointer = eventData.pointerId;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;

            RectTransform area = transform as RectTransform;
            if (area == null) return;

            Vector2 localPosition;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area, eventData.position, eventData.pressEventCamera, out localPosition))
            {
                return;
            }

            // Center-relative even if the designer changes the area's pivot.
            Vector2 center = new Vector2(
                (0.5f - area.pivot.x) * area.rect.width,
                (0.5f - area.pivot.y) * area.rect.height);
            Vector2 displacement = Vector2.ClampMagnitude(localPosition - center, radius);
            Vector2 value = displacement / radius;
            if (value.sqrMagnitude < deadZone * deadZone) value = Vector2.zero;

            if (knob != null) knob.anchoredPosition = displacement;
            HighflyTouchInputState.Move = value;
            SendValueToControl(value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer != eventData.pointerId) return;
            ReleaseInput();
        }

        private void ReleaseInput()
        {
            activePointer = int.MinValue;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            HighflyTouchInputState.Move = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }

        protected override void OnDisable()
        {
            // The OnScreenControl base removes the virtual device.
            activePointer = int.MinValue;
            HighflyTouchInputState.Move = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            base.OnDisable();
        }
    }

    /// <summary>
    /// Invisible right-side drag surface. Each pointer owns its gesture, so
    /// moving the left joystick does not steal the camera touch.
    /// Maps drag velocity to the virtual Gamepad right stick.
    /// </summary>

    // AnyRPG polls analogs in InputManager.RegisterInput(). Its WebGL input
    // actions may ignore an OnScreenControl-created virtual XInput controller.
    // This isolated mobile bridge inserts values at the exact same native poll.
    public static class HighflyTouchInputState
    {
        public static bool Enabled;
        public static Vector2 Move;
        private static Vector2 lookImpulse;
        private static bool jumpQueued;
        private static bool rollQueued;
        private static bool releaseJumpNext;
        private static bool releaseRollNext;

        public static void Look(Vector2 value)
        {
            lookImpulse = Vector2.ClampMagnitude(lookImpulse + value, 1f);
        }

        public static void TapJump() { if (Enabled) jumpQueued = true; }
        public static void TapRoll() { if (Enabled) rollQueued = true; }

        public static void Reset()
        {
            Move = Vector2.zero;
            lookImpulse = Vector2.zero;
            jumpQueued = false;
            rollQueued = false;
        }

        public static void ApplyTo(InputManager manager)
        {
            if (manager == null) return;
            if (releaseJumpNext && manager.InputActionNodes.TryGetValue("JUMP", out var previousJump))
                previousJump.RegisterKeyUp();
            if (releaseRollNext && manager.InputActionNodes.TryGetValue("ROLL", out var previousRoll))
                previousRoll.RegisterKeyUp();
            releaseJumpNext = releaseRollNext = false;
            if (!Enabled) { Reset(); return; }

            manager.leftAnalogHorizontal = Mathf.Clamp(Move.x, -1f, 1f);
            manager.leftAnalogVertical = Mathf.Clamp(Move.y, -1f, 1f);
            manager.rightAnalogHorizontal = Mathf.Clamp(lookImpulse.x, -1f, 1f);
            manager.rightAnalogVertical = Mathf.Clamp(lookImpulse.y, -1f, 1f);
            lookImpulse = Vector2.zero;

            if (jumpQueued && manager.InputActionNodes.TryGetValue("JUMP", out var jump))
            {
                jump.RegisterKeyPress();
                releaseJumpNext = true;
            }
            if (rollQueued && manager.InputActionNodes.TryGetValue("ROLL", out var roll))
            {
                roll.RegisterKeyPress();
                releaseRollNext = true;
            }
            jumpQueued = rollQueued = false;
        }
    }
}
