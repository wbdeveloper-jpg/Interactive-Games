using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnswerDrop : MonoBehaviour, IDropHandler
{
    public FillImage fillImage;

    [Header("Question Setup")]
    [Tooltip("Assign the SetQuestions object from the gameplay panel. Used for target emotion matching.")]
    [SerializeField] private SetQuestions setQuestions;

    [Tooltip("Correct answer must match both current intensity and selected target emotion.")]
    [SerializeField] private bool requireEmotionMatch = true;

    [Header("Hint Feedback")]
    [SerializeField] private InfoHintPanelController infoHintPanelController;

    [Header("Mascot")]
    public Image mascotFace;
    public Sprite mascotHappy;
    public Sprite mascotSad;

    [Header("Emoji")]
    public Image emojiFace;
    public Sprite emojiHappy;
    public Sprite emojiSad;

    [Header("Wrong Drop Counter")]
    [SerializeField] private bool resetWrongCountOnEnable = true;
    [SerializeField] private TextMeshProUGUI wrongDropCountText;

    public int WrongDropCount { get; private set; }

    private void Awake()
    {
        ResolveOptionalReferences();
    }

    private void OnEnable()
    {
        ResolveOptionalReferences();

        if (resetWrongCountOnEnable)
            ResetWrongDropCount();
        else
            UpdateWrongDropUI();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        GameObject dropped = eventData.pointerDrag;
        Draggable draggable = dropped.GetComponent<Draggable>();

        if (draggable == null || fillImage == null)
            return;

        float draggableIntensity = Draggable.NormalizeIntensity(draggable.intensity);
        float currentIntensity = Draggable.NormalizeIntensity(fillImage.currentIntensity);
        string targetEmotion = GetTargetEmotionLabel();

        bool intensityMatches = Mathf.Approximately(draggableIntensity, currentIntensity);
        bool emotionMatches = IsEmotionCorrect(draggable.label, targetEmotion);

        if (intensityMatches && emotionMatches)
        {
            HandleCorrectDrop(dropped, draggable, draggableIntensity);
        }
        else
        {
            HandleWrongDrop(draggable, intensityMatches, emotionMatches, targetEmotion);
        }
    }

    private void HandleCorrectDrop(GameObject dropped, Draggable draggable, float draggableIntensity)
    {
        Debug.Log("AnswerDrop: Correct answer. Intensity and emotion matched.", this);

        draggable.parentAfterDrag = transform;
        Draggable.SetStretchWithMargins(draggable.GetComponent<RectTransform>(), 35f);

        fillImage.ChangeEmotion(true, emojiFace, emojiHappy, emojiSad);
        fillImage.ChangeEmotion(true, mascotFace, mascotHappy, mascotSad);

        string intensityText = fillImage.GetIntensityText(draggableIntensity);
        string floatingText = intensityText + " " + draggable.label;
        fillImage.SpawnFloatingText(floatingText);

        if (EmotionAudioMapper.Instance != null)
        {
            EmotionAudioMapper.Instance.PlayEmotionAudio(draggableIntensity, draggable.label);
        }
        else
        {
            Debug.LogWarning("AnswerDrop: EmotionAudioMapper missing in scene.", this);
        }

        fillImage.FadeAndDestroy(dropped, 1f, () =>
        {
            if (fillImage != null)
                fillImage.IncreaseIntensity();
        });
    }

    private void HandleWrongDrop(Draggable draggable, bool intensityMatches, bool emotionMatches, string targetEmotion)
    {
        Debug.Log(
            "AnswerDrop: Wrong answer. IntensityMatch=" + intensityMatches +
            ", EmotionMatch=" + emotionMatches +
            ", TargetEmotion=" + targetEmotion +
            ", DroppedEmotion=" + (draggable != null ? draggable.label : "null"),
            this
        );

        AddWrongDrop();

        if (infoHintPanelController != null)
            infoHintPanelController.NotifyWrongDrop();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(1);

        fillImage.ChangeEmotion(false, emojiFace, emojiHappy, emojiSad);
        fillImage.ChangeEmotion(false, mascotFace, mascotHappy, mascotSad);
    }

    private string GetTargetEmotionLabel()
    {
        if (setQuestions == null)
            setQuestions = FindObjectOfType<SetQuestions>();

        if (setQuestions == null)
        {
            if (requireEmotionMatch)
                Debug.LogWarning("AnswerDrop: SetQuestions reference missing. Falling back to intensity-only validation.", this);

            return string.Empty;
        }

        return setQuestions.SelectedTargetEmotionLabel;
    }

    private bool IsEmotionCorrect(string droppedEmotion, string targetEmotion)
    {
        if (!requireEmotionMatch)
            return true;

        if (string.IsNullOrWhiteSpace(targetEmotion))
            return true;

        return Draggable.LabelsMatch(droppedEmotion, targetEmotion);
    }

    private void AddWrongDrop()
    {
        WrongDropCount++;
        UpdateWrongDropUI();
        Debug.Log("Wrong Drop Count: " + WrongDropCount, this);
    }

    public void ResetWrongDropCount()
    {
        WrongDropCount = 0;
        UpdateWrongDropUI();
    }

    private void UpdateWrongDropUI()
    {
        if (wrongDropCountText != null)
            wrongDropCountText.text = WrongDropCount.ToString();
    }

    private void ResolveOptionalReferences()
    {
        if (setQuestions == null)
            setQuestions = FindObjectOfType<SetQuestions>();

        if (infoHintPanelController == null)
            infoHintPanelController = FindObjectOfType<InfoHintPanelController>();
    }
}
