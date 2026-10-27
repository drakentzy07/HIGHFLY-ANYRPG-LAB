// HIGHFLY FURY 360 mobile bridge. No synthetic Gamepad:
// Unity WebGL's virtual controller can interfere with simultaneous touches.
// All inputs enter AnyRPG's native InputManager at RegisterInput().
using AnyRPG;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HIGHFLY.Mobile
{
    [AddComponentMenu("HIGHFLY/Mobile/Touch Move Area")]
    public sealed class HighflyTouchMoveArea : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
    {
        [SerializeField] private RectTransform knob;
        [SerializeField, Min(1f)] private float radius = 94f;
        [SerializeField, Range(0f, 0.4f)] private float deadZone = .07f;
        private int activePointer = int.MinValue;

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
            var area = transform as RectTransform;
            if (area == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area, eventData.position, eventData.pressEventCamera, out var point)) return;
            var center = new Vector2((.5f-area.pivot.x)*area.rect.width,
                (.5f-area.pivot.y)*area.rect.height);
            var delta = Vector2.ClampMagnitude(point - center, radius);
            var value = delta/radius;
            if (value.sqrMagnitude < deadZone*deadZone) value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = delta;
            HighflyTouchInputState.Move = value;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer == eventData.pointerId) ResetStick();
        }

        public void OnCancel(BaseEventData eventData) { ResetStick(); }

        private void ResetStick()
        {
            activePointer = int.MinValue;
            HighflyTouchInputState.Move = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void OnDisable() { ResetStick(); }
    }

    // FURY input state is read by the ORIGINAL AnyRPG InputManager once per frame.
    public static class HighflyTouchInputState
    {
        public static bool Enabled;
        public static Vector2 Move;
        private static Vector2 lookImpulse;
        private static bool jumpQueued;
        private static bool jumpRelease;
        private static bool dashQueued;
        private static float nextDashTime;
        public static bool DashActive { get; private set; }
        public static Vector2 DashDirection { get; private set; }
        public static float DashUntil { get; private set; }
        public const float DashSpeed = 10f;

        public static void Look(Vector2 value)
        {
            if (Enabled) lookImpulse = Vector2.ClampMagnitude(lookImpulse + value, 1f);
        }

        public static void TapJump()
        {
            if (Enabled) jumpQueued = true;
        }

        public static void TapDash()
        {
            if (!Enabled || Time.time < nextDashTime) return;
            nextDashTime = Time.time + .85f;
            DashDirection = Move.sqrMagnitude > .025f ? Move.normalized : Vector2.up;
            DashUntil = Time.time + .18f;
            DashActive = true;
            dashQueued = true;
        }

        public static void Reset()
        {
            Move = Vector2.zero;
            lookImpulse = Vector2.zero;
            jumpQueued = false;
            dashQueued = false;
            DashActive = false;
        }

        public static void ApplyTo(InputManager manager)
        {
            if (manager == null) return;
            if (jumpRelease && manager.InputActionNodes.TryGetValue("JUMP", out var release))
                release.RegisterKeyUp();
            jumpRelease = false;
            if (!Enabled) { Reset(); return; }

            manager.leftAnalogHorizontal = Move.x;
            manager.leftAnalogVertical = Move.y;
            manager.rightAnalogHorizontal = lookImpulse.x;
            manager.rightAnalogVertical = lookImpulse.y;
            lookImpulse = Vector2.zero;

            if (jumpQueued && manager.InputActionNodes.TryGetValue("JUMP", out var jump))
            {
                jump.RegisterKeyPress();
                jumpRelease = true;
            }
            jumpQueued = false;
            if (Time.time >= DashUntil) DashActive = false;
            dashQueued = false;
        }
    }
}
