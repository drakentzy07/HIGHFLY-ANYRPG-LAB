// HIGHFLY FURY 360: independent touch finger for camera orbit.
// Native UI PointerEvents preserve multi-touch and never create virtual Gamepads.
using UnityEngine;
using UnityEngine.EventSystems;

namespace HIGHFLY.Mobile
{
    [AddComponentMenu("HIGHFLY/Mobile/Touch Look Area")]
    public sealed class HighflyTouchLookArea : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
    {
        [SerializeField, Range(.05f,2f)] private float sensitivity = .42f;
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        private int activePointer = int.MinValue;
        private Vector2 previousPosition;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != int.MinValue) return;
            activePointer = eventData.pointerId;
            previousPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (activePointer != eventData.pointerId) return;
            Vector2 delta = eventData.position - previousPosition;
            previousPosition = eventData.position;
            // Convert screen displacement into a bounded camera analogue pulse.
            // No time-based multiplication: stable across varying WebGL FPS.
            float scale = sensitivity * 60f / Mathf.Max(1f, Screen.height);
            Vector2 look = Vector2.ClampMagnitude(delta * scale, 1f);
            if (invertHorizontal) look.x = -look.x;
            if (invertVertical) look.y = -look.y;
            HighflyTouchInputState.Look(look);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer == eventData.pointerId) activePointer = int.MinValue;
        }

        public void OnCancel(BaseEventData eventData)
        {
            activePointer = int.MinValue;
        }

        private void OnDisable()
        {
            activePointer = int.MinValue;
        }
    }
}
