using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SentenceWordSearchCell : MonoBehaviour
{
    [Header("References")]
    public RectTransform rectTransform;
    public Image backgroundImage;
    public Image previewOverlay;
    public Image solvedOverlay;
    public Image wrongOverlay;
    public Image hintOverlay;
    public TextMeshProUGUI letterText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color previewColor = new Color(1f, 0.84f, 0.18f, 0.8f);
    public Color solvedColor = new Color(0.26f, 0.95f, 0.48f, 0.65f);
    public Color wrongColor = new Color(1f, 0.25f, 0.25f, 0.75f);
    public Color hintColor = new Color(0.35f, 0.72f, 1f, 0.7f);

    public int Row { get; private set; }
    public int Column { get; private set; }
    public char Letter { get; private set; }

    private Tween hintTween;

    public void Setup(int row, int column, char letter)
    {
        Row = row;
        Column = column;
        Letter = char.ToUpperInvariant(letter);

        CacheRefs();

        if (letterText != null)
            letterText.text = Letter.ToString();

        ResetCell();
    }

    public void SetPreview(bool active)
    {
        CacheRefs();

        if (previewOverlay != null)
        {
            previewOverlay.gameObject.SetActive(active);
            previewOverlay.color = previewColor;
        }

        transform.DOKill(false);
        transform.DOScale(active ? 1.06f : 1f, 0.08f).SetEase(Ease.OutQuad);
    }

    public void SetSolved(bool active)
    {
        CacheRefs();

        if (solvedOverlay != null)
        {
            solvedOverlay.gameObject.SetActive(active);
            solvedOverlay.color = solvedColor;
        }

        if (active)
        {
            ClearHint();
            transform.DOKill(false);
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.12f, 0.22f, 6, 0.8f);
        }
    }

    public void SetWrong(bool active)
    {
        CacheRefs();

        if (wrongOverlay != null)
        {
            wrongOverlay.gameObject.SetActive(active);
            wrongOverlay.color = wrongColor;
        }

        if (active)
        {
            transform.DOKill(false);
            transform.DOShakePosition(0.18f, 7f, 14, 90f, false, true);
        }
    }

    public void SetHint(bool active)
    {
        CacheRefs();

        if (!active)
        {
            ClearHint();
            return;
        }

        if (hintOverlay != null)
        {
            hintOverlay.gameObject.SetActive(true);
            hintOverlay.color = hintColor;
        }

        hintTween?.Kill();
        transform.DOKill(false);
        transform.localScale = Vector3.one;
        hintTween = transform.DOScale(1.12f, 0.28f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public void ClearHint()
    {
        hintTween?.Kill();
        hintTween = null;

        if (hintOverlay != null)
            hintOverlay.gameObject.SetActive(false);

        transform.DOKill(false);
        transform.localScale = Vector3.one;
    }

    public void ClearTransient()
    {
        SetPreview(false);
        SetWrong(false);
    }

    public void ResetCell()
    {
        CacheRefs();

        hintTween?.Kill();
        hintTween = null;
        transform.DOKill(false);
        transform.localScale = Vector3.one;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        if (previewOverlay != null)
            previewOverlay.gameObject.SetActive(false);

        if (solvedOverlay != null)
            solvedOverlay.gameObject.SetActive(false);

        if (wrongOverlay != null)
            wrongOverlay.gameObject.SetActive(false);

        if (hintOverlay != null)
            hintOverlay.gameObject.SetActive(false);
    }

    public Vector3 GetWorldCenter()
    {
        CacheRefs();
        return rectTransform != null ? rectTransform.position : transform.position;
    }

    private void CacheRefs()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (letterText == null)
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
    }
}
