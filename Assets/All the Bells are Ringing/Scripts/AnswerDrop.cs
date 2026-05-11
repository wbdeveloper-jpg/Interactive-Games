using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnswerDrop : MonoBehaviour, IDropHandler
{
    public FillImage fillImage;

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

    private void OnEnable()
    {
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

        float draggableIntensity = NormalizeIntensity(draggable.intensity);
        float currentIntensity = NormalizeIntensity(fillImage.currentIntensity);

        if (Mathf.Approximately(draggableIntensity, currentIntensity))
        {
            Debug.Log("Intensity Matched");

            draggable.parentAfterDrag = transform;
            Draggable.SetStretchWithMargins(draggable.GetComponent<RectTransform>(), 35f);

            fillImage.ChangeEmotion(true, emojiFace, emojiHappy, emojiSad);
            fillImage.ChangeEmotion(true, mascotFace, mascotHappy, mascotSad);

            string intensityTxt = GetIntensityText(draggableIntensity) + " " + draggable.label;
            fillImage.SpawnFloatingText(intensityTxt);

            // New dynamic audio system.
            if (EmotionAudioMapper.Instance != null)
            {
                EmotionAudioMapper.Instance.PlayEmotionAudio(draggableIntensity, draggable.label);
            }
            else
            {
                Debug.LogWarning("AnswerDrop: EmotionAudioMapper missing in scene.");
            }

            fillImage.FadeAndDestroy(dropped, 1f, () =>
            {
                fillImage.IncreaseIntensity();
            });
        }
        else
        {
            Debug.Log("Intensity Doesn't Match");

            AddWrongDrop();

            AudioManager.Instance.PlaySFX(1);

            fillImage.ChangeEmotion(false, emojiFace, emojiHappy, emojiSad);
            fillImage.ChangeEmotion(false, mascotFace, mascotHappy, mascotSad);
        }
    }

    private void AddWrongDrop()
    {
        WrongDropCount++;
        UpdateWrongDropUI();

        Debug.Log("Wrong Drop Count: " + WrongDropCount);
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

    private string GetIntensityText(float intensity)
    {
        if (fillImage != null && fillImage.intensityDict != null)
        {
            foreach (var pair in fillImage.intensityDict)
            {
                if (Mathf.Approximately(NormalizeIntensity(pair.Key), intensity))
                    return pair.Value;
            }
        }

        return intensity.ToString("0.0");
    }

    private float NormalizeIntensity(float value)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(value) / 0.2f);
        step = Mathf.Clamp(step, 1, 5);
        return step * 0.2f;
    }
}