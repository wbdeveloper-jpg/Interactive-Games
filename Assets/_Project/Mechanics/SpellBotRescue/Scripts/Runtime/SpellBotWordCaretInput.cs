using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NarayanaGames.SpellBotRescue
{
    [DisallowMultipleComponent]
    public sealed class SpellBotWordCaretInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [Header("References")]
        public SpellBotRescueManager manager;
        public TMP_InputField targetInputField;
        public TextMeshProUGUI targetText;

        private int lastPointerDownFrame = -1;

        private void Reset()
        {
            targetInputField = GetComponent<TMP_InputField>();

            if (targetInputField != null)
            {
                targetText = targetInputField.textComponent as TextMeshProUGUI;
            }

            if (manager == null)
            {
                manager = FindObjectOfType<SpellBotRescueManager>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastPointerDownFrame = Time.frameCount;
            ForwardPointer(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            ForwardPointer(eventData);
        }

        private void ForwardPointer(PointerEventData eventData)
        {
            if (manager == null)
            {
                manager = FindObjectOfType<SpellBotRescueManager>();
            }

            if (manager == null || eventData == null)
            {
                return;
            }

            manager.MoveCaretFromScreenPoint(eventData.position, eventData.pressEventCamera);
        }
    }
}
