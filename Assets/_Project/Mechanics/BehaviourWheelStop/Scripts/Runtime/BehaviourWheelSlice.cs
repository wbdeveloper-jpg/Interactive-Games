using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public class BehaviourWheelSlice : MonoBehaviour
    {
        [Header("References")]
        public RectTransform contentRoot;
        public Image iconImage;
        public TMP_Text labelText;

        [Header("Icon Placeholder")]
        public bool showIconPlaceholderWhenEmpty = true;

        [Header("Runtime")]
        [SerializeField] private string answerText;
        [SerializeField] private int sliceIndex;

        public string AnswerText => answerText;
        public int SliceIndex => sliceIndex;

        public void SetIndex(int index)
        {
            sliceIndex = index;
            gameObject.name = $"Slice_{index}_Content";
        }

        public void SetOption(BehaviourWheelOptionData option, bool showIcon)
        {
            answerText = option != null ? option.answerText : string.Empty;

            if (labelText != null)
                labelText.text = answerText;

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(showIcon);

                Sprite icon = option != null ? option.icon : null;
                iconImage.sprite = icon;
                iconImage.enabled = showIcon && (icon != null || showIconPlaceholderWhenEmpty);
            }
        }

        public void ApplyFont(TMP_FontAsset font)
        {
            if (labelText != null && font != null)
                labelText.font = font;
        }
    }
}
