// HIGHFLY FURY: independent touch pointer per ability.
// No simulated mouse, no Unity gamepad; the native AnyRPG ground projector
// receives a mobile cursor while the left joystick remains independent.
using UnityEngine;
using UnityEngine.EventSystems;

namespace HIGHFLY.Mobile
{
    public static class HighflyTouchAimState
    {
        public static bool Active;
        public static Vector2 ScreenPosition;
        public static Vector3 WorldPosition;
    }

    public sealed class HighflySkillPointer : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
    {
        public HighflyAnyRpgMobileHud Hud;
        public int Slot;
        private int currentPointer = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (currentPointer != int.MinValue || Hud == null) return;
            currentPointer = eventData.pointerId;
            Hud.BeginSkillTouch(Slot, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != currentPointer || Hud == null) return;
            Hud.DragSkillTouch(Slot, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != currentPointer || Hud == null) return;
            currentPointer = int.MinValue;
            Hud.EndSkillTouch(Slot, eventData.position);
        }

        public void OnCancel(BaseEventData eventData)
        {
            currentPointer = int.MinValue;
            if (Hud != null) Hud.CancelSkillAim();
        }

        private void OnDisable()
        {
            currentPointer = int.MinValue;
            if (Hud != null) Hud.CancelSkillAim();
        }
    }
}
