using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SentenceWordSearchCell : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    public Image backgroundImage;
    public Image solvedOverlayImage;
    public Image previewOverlayImage;
    public Image hintRingImage;
    public TextMeshProUGUI letterText;
    public Button button;

    [Header("Default Colors")]
    public Color normalColor = new Color(1f, 0.96f, 0.96f, 1f);
    public Color textColor = new Color(0.22f, 0.18f, 0.18f, 1f);
    public Color previewColor = new Color(0.95f, 0.35f, 0.32f, 0.45f);
    public Color solvedColor = new Color(0.45f, 0.86f, 0.55f, 0.55f);
    public Color wrongColor = new Color(1f, 0.18f, 0.18f, 0.65f);
    public Color hintColor = new Color(0.95f, 0.22f, 0.22f, 0.95f);

    public int Row { get; private set; }
    public int Column { get; private set; }
    public char Letter { get; private set; }

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            return rectTransform;
        }
    }

    private Sequence hintSequence;
    private Tween wrongTween;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        ClearRuntimeVisuals();
    }

    public void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (letterText == null)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(int row, int column, char letter, TMP_FontAsset fontAsset)
    {
        CacheReferences();

        Row = row;
        Column = column;
        Letter = letter;

        if (letterText != null)
        {
            letterText.text = letter.ToString();
            letterText.color = textColor;

            if (fontAsset != null)
                letterText.font = fontAsset;
        }

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        ClearRuntimeVisuals();
    }

    public void ApplyFont(TMP_FontAsset fontAsset)
    {
        if (letterText != null && fontAsset != null)
            letterText.font = fontAsset;
    }

    public void SetPreview(bool active)
    {
        if (previewOverlayImage == null)
            return;

        previewOverlayImage.DOKill();
        previewOverlayImage.gameObject.SetActive(active);
        previewOverlayImage.color = previewColor;

        if (active)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOScale(1.06f, 0.08f).SetLoops(2, LoopType.Yoyo);
        }
    }

    public void SetSolved(bool active)
    {
        if (solvedOverlayImage == null)
            return;

        solvedOverlayImage.DOKill();
        solvedOverlayImage.gameObject.SetActive(active);
        solvedOverlayImage.color = solvedColor;

        if (active)
        {
            solvedOverlayImage.color = new Color(solvedColor.r, solvedColor.g, solvedColor.b, 0f);
            solvedOverlayImage.DOFade(solvedColor.a, 0.18f);
        }
    }

    public void FlashWrong(float duration)
    {
        if (previewOverlayImage == null)
            return;

        if (wrongTween != null)
            wrongTween.Kill();

        previewOverlayImage.gameObject.SetActive(true);
        previewOverlayImage.color = wrongColor;

        transform.DOKill();
        transform.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOShakePosition(0.16f, 8f, 12, 90f, false, true));
        sequence.Join(previewOverlayImage.DOFade(0f, Mathf.Max(0.05f, duration)));
        sequence.OnComplete(() =>
        {
            if (previewOverlayImage != null)
                previewOverlayImage.gameObject.SetActive(false);

            transform.localScale = Vector3.one;
        });

        wrongTween = sequence;
    }

    public void PulseHint(float duration)
    {
        if (hintRingImage == null)
            return;

        StopHintPulse();

        hintRingImage.gameObject.SetActive(true);
        hintRingImage.color = hintColor;
        hintRingImage.transform.localScale = Vector3.one;

        hintSequence = DOTween.Sequence();
        hintSequence.Append(hintRingImage.transform.DOScale(1.18f, 0.28f).SetEase(Ease.OutSine));
        hintSequence.Join(hintRingImage.DOFade(0.35f, 0.28f));
        hintSequence.Append(hintRingImage.transform.DOScale(1f, 0.28f).SetEase(Ease.InSine));
        hintSequence.Join(hintRingImage.DOFade(hintColor.a, 0.28f));
        hintSequence.SetLoops(Mathf.Max(1, Mathf.CeilToInt(duration / 0.56f)));
        hintSequence.OnComplete(StopHintPulse);
    }

    public void StopHintPulse()
    {
        if (hintSequence != null)
        {
            hintSequence.Kill();
            hintSequence = null;
        }

        if (hintRingImage != null)
        {
            hintRingImage.DOKill();
            hintRingImage.transform.localScale = Vector3.one;
            hintRingImage.gameObject.SetActive(false);
        }
    }

    public void ClearRuntimeVisuals()
    {
        StopHintPulse();

        if (previewOverlayImage != null)
        {
            previewOverlayImage.DOKill();
            previewOverlayImage.gameObject.SetActive(false);
        }

        if (solvedOverlayImage != null)
        {
            solvedOverlayImage.DOKill();
            solvedOverlayImage.gameObject.SetActive(false);
        }

        if (hintRingImage != null)
        {
            hintRingImage.DOKill();
            hintRingImage.gameObject.SetActive(false);
        }

        transform.DOKill();
        transform.localScale = Vector3.one;
    }
}
