using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridAdventureCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Cell")]
    public string coordinate = "A1";
    public RectTransform placedItemRoot;
    [Tooltip("Coordinate text is disabled by default. The grid still uses this coordinate internally for matching.")]
    public bool showCoordinateLabel = false;
    public TextMeshProUGUI coordinateLabel;

    [Header("Visuals")]
    public Image backgroundImage;
    public Image activeOutlineImage;
    public Color normalColor = new Color(0.94f, 0.94f, 0.88f, 1f);
    public Color activeColor = new Color(0.78f, 0.91f, 1f, 1f);
    public Color completedColor = new Color(0.78f, 0.95f, 0.78f, 1f);
    public Color flashColor = Color.white;
    public float activePulseScale = 1.04f;
    [Range(0f, 1f)] public float activeOutlineAlpha = 0.35f;

    public bool IsCompleted { get; private set; }

    private GridAdventureManager manager;
    private Tween pulseTween;
    private RectTransform rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public void Init(GridAdventureManager owner)
    {
        manager = owner;
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (placedItemRoot == null) placedItemRoot = RectTransform;
        ApplyCoordinateLabelState();
        SetNormalVisual();
    }

    public void SetCoordinate(string newCoordinate)
    {
        coordinate = newCoordinate;
        ApplyCoordinateLabelState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null || IsCompleted) return;
        manager.SelectCell(this, true);
    }

    public void SetActiveVisual(bool active)
    {
        KillPulse();

        if (activeOutlineImage != null)
        {
            activeOutlineImage.gameObject.SetActive(true);
            Color color = activeOutlineImage.color;
            color.a = active ? 0f : 0f;
            activeOutlineImage.color = color;

            if (active)
                activeOutlineImage.DOFade(activeOutlineAlpha, 0.18f).SetEase(Ease.OutQuad);
            else
                activeOutlineImage.DOFade(0f, 0.12f).SetEase(Ease.OutQuad);
        }

        if (backgroundImage != null && !IsCompleted)
            backgroundImage.DOColor(active ? activeColor : normalColor, 0.15f);

        if (active)
        {
            RectTransform.localScale = Vector3.one;
            pulseTween = RectTransform.DOScale(Vector3.one * activePulseScale, 0.55f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            RectTransform.localScale = Vector3.one;
        }
    }

    public void SetCompletedVisual()
    {
        IsCompleted = true;
        KillPulse();
        RectTransform.localScale = Vector3.one;

        if (activeOutlineImage != null)
            activeOutlineImage.DOFade(0f, 0.12f);

        if (backgroundImage != null)
            backgroundImage.DOColor(completedColor, 0.2f);
    }

    public void PlayCorrectFlash()
    {
        if (backgroundImage == null) return;

        Color baseColor = backgroundImage.color;
        Sequence flash = DOTween.Sequence();
        flash.Append(backgroundImage.DOColor(flashColor, 0.08f));
        flash.Append(backgroundImage.DOColor(baseColor, 0.16f));
    }

    public void PlayNextCellPop(System.Action onComplete = null)
    {
        KillPulse();
        RectTransform.localScale = Vector3.one;

        Sequence pop = DOTween.Sequence();
        pop.Append(RectTransform.DOScale(1.1f, 0.15f).SetEase(Ease.OutBack));
        pop.Append(RectTransform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        pop.OnComplete(() => onComplete?.Invoke());
    }

    private void ApplyCoordinateLabelState()
    {
        if (coordinateLabel == null) return;

        coordinateLabel.text = coordinate;
        coordinateLabel.gameObject.SetActive(showCoordinateLabel);
    }

    private void SetNormalVisual()
    {
        IsCompleted = false;
        KillPulse();
        RectTransform.localScale = Vector3.one;

        if (backgroundImage != null) backgroundImage.color = normalColor;

        if (activeOutlineImage != null)
        {
            Color color = activeOutlineImage.color;
            color.a = 0f;
            activeOutlineImage.color = color;
            activeOutlineImage.gameObject.SetActive(true);
        }
    }

    private void KillPulse()
    {
        if (pulseTween != null && pulseTween.IsActive())
            pulseTween.Kill();

        pulseTween = null;
        RectTransform.DOKill();
    }
}
