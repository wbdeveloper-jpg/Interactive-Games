using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NarayanaGames.SpellBotRescue
{
    public class SpellBotWordCaretInput : MonoBehaviour, IPointerDownHandler
    {
        [Header("References")]
        public SpellBotRescueManager manager;
        public TMP_InputField targetInputField;
        public TextMeshProUGUI targetText;

        private void Reset()
        {
            manager = FindObjectOfType<SpellBotRescueManager>();
            targetInputField = GetComponent<TMP_InputField>();

            if (targetInputField != null)
            {
                targetText = targetInputField.textComponent as TextMeshProUGUI;
            }
            else
            {
                targetText = GetComponent<TextMeshProUGUI>();
            }
        }

        private void Awake()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<SpellBotRescueManager>();
            }

            if (targetInputField == null)
            {
                targetInputField = GetComponent<TMP_InputField>();
            }

            if (targetText == null && targetInputField != null)
            {
                targetText = targetInputField.textComponent as TextMeshProUGUI;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (manager == null)
            {
                return;
            }

            manager.MoveCaretFromScreenPoint(eventData.position, eventData.pressEventCamera);
        }
    }
}
