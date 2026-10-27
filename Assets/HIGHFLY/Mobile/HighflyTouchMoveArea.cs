// HIGHFLY A2 - experimental mobile input adapters.
// MIT-compatible original code, isolated from AnyRPG engine sources.
// Add to UI Images with raycastTarget enabled and an InputSystemUIInputModule.
using UnityEngine;
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
            SendValueToControl(Vector2.zero);
        }

        protected override void OnDisable()
        {
            // The OnScreenControl base removes the virtual device.
            activePointer = int.MinValue;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            base.OnDisable();
        }
    }

    /// <summary>
    /// Invisible right-side drag surface. Each pointer owns its gesture, so
    /// moving the left joystick does not steal the camera touch.
    /// Maps drag velocity to the virtual Gamepad right stick.
    /// </summary>
}
