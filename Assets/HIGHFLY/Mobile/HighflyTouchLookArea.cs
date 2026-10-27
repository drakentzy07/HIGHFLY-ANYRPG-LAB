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
    [AddComponentMenu("HIGHFLY/Mobile/Touch Look Area")]
    public sealed class HighflyTouchLookArea : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, InputControl(layout = "Vector2")]
        private string inputControlPath = "<XInputController>/rightStick";

        [SerializeField, Range(0.05f, 2f)] private float sensitivity = 0.38f;
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;

        private int activePointer = int.MinValue;
        private Vector2 previousPosition;
        private int lastDragFrame = -100;

        protected override string controlPathInternal
        {
            get { return inputControlPath; }
            set { inputControlPath = value; }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != int.MinValue) return;
            activePointer = eventData.pointerId;
            previousPosition = eventData.position;
            lastDragFrame = Time.frameCount;
            SendValueToControl(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (activePointer != eventData.pointerId) return;

            Vector2 pixelDelta = eventData.position - previousPosition;
            previousPosition = eventData.position;

            // Normalize by display height and frame duration; cap to stick range.
            float seconds = Mathf.Max(Time.unscaledDeltaTime, 1f / 120f);
            float scale = sensitivity / (Mathf.Max(Screen.height, 1) * seconds);
            Vector2 look = pixelDelta * scale;
            if (invertHorizontal) look.x = -look.x;
            if (invertVertical) look.y = -look.y;

            lastDragFrame = Time.frameCount;
            SendValueToControl(Vector2.ClampMagnitude(look, 1f));
        }

        private void LateUpdate()
        {
            // Let the player's Update process the drag, then release its axis.
            // Never keep rotating after the user stops dragging.
            if (activePointer == int.MinValue || Time.frameCount > lastDragFrame)
                SendValueToControl(Vector2.zero);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer != eventData.pointerId) return;
            activePointer = int.MinValue;
            SendValueToControl(Vector2.zero);
        }

        protected override void OnDisable()
        {
            activePointer = int.MinValue;
            base.OnDisable();
        }
    }
}
