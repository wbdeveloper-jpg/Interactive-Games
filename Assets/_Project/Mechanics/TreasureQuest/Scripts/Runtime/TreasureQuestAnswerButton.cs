using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum TreasureQuestAnswerVisualState
{
    Normal,
    Correct,
    Wrong,
    Disabled
}

[RequireComponent(typeof(Button))]
public class TreasureQuestAnswerButton : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public Image backgroundImage;
    public TMP_Text answerText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = new Color(0.35f, 0.85f, 0.45f);
    public Color wrongColor = new Color(0.95f, 0.35f, 0.35f);
    public Color disabledColor = new Color(0.75f, 0.75f, 0.75f);

    private TreasureQuestQuizManager quizManager;
    private int answerIndex;
    private RectTransform rectTransform;

    private void Reset()
    {
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
        answerText = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (answerText == null) answerText = GetComponentInChildren<TMP_Text>();
        rectTransform = transform as RectTransform;
    }

    public void Setup(TreasureQuestQuizManager manager)
    {
        quizManager = manager;
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    public void SetData(int index, string text)
    {
        answerIndex = index;
        if (answerText != null)
        {
            answerText.text = text;
            answerText.enableAutoSizing = true;
            answerText.fontSizeMin = 22f;
            answerText.fontSizeMax = 34f;
            answerText.enableWordWrapping = true;
            answerText.overflowMode = TextOverflowModes.Ellipsis;
        }

        SetInteractable(true);
        SetState(TreasureQuestAnswerVisualState.Normal, false);
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    public void SetState(TreasureQuestAnswerVisualState state, bool animate = true)
    {
        if (backgroundImage != null)
        {
            switch (state)
            {
                case TreasureQuestAnswerVisualState.Correct:
                    backgroundImage.color = correctColor;
                    break;
                case TreasureQuestAnswerVisualState.Wrong:
                    backgroundImage.color = wrongColor;
                    break;
                case TreasureQuestAnswerVisualState.Disabled:
                    backgroundImage.color = disabledColor;
                    break;
                default:
                    backgroundImage.color = normalColor;
                    break;
            }
        }

        if (!animate || rectTransform == null) return;

        rectTransform.DOKill();
        if (state == TreasureQuestAnswerVisualState.Correct)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.7f);
        }
        else if (state == TreasureQuestAnswerVisualState.Wrong)
        {
            rectTransform.DOShakeAnchorPos(0.25f, 12f, 12, 90f);
        }
    }

    private void HandleClick()
    {
        quizManager?.OnAnswerSelected(answerIndex, this);
    }
}
