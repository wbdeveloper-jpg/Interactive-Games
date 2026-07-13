using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum TreasureQuestGateState
{
    Locked,
    Unlocked,
    Completed
}

[RequireComponent(typeof(Button))]
public class TreasureQuestGateButton : MonoBehaviour
{
    [Header("Gate")]
    [Range(1, 5)] public int gateNumber = 1;
    public bool showGateLabel = true;

    [Header("References")]
    public Button button;
    public Image gateImage;
    public TMP_Text gateLabel;

    private TreasureQuestLevelManager levelManager;
    private RectTransform rectTransform;
    private TreasureQuestGateState currentState;

    private void Reset()
    {
        button = GetComponent<Button>();
        gateImage = GetComponent<Image>();
        gateLabel = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (gateImage == null) gateImage = GetComponent<Image>();
        if (gateLabel == null) gateLabel = GetComponentInChildren<TMP_Text>();
        rectTransform = transform as RectTransform;
    }

    public void Setup(TreasureQuestLevelManager manager)
    {
        levelManager = manager;
        if (button == null) button = GetComponent<Button>();
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    public void ApplyState(TreasureQuestGateState state, Sprite lockedSprite, Sprite unlockedSprite, Sprite completedSprite)
    {
        currentState = state;

        if (gateImage != null)
        {
            if (state == TreasureQuestGateState.Locked && lockedSprite != null) gateImage.sprite = lockedSprite;
            if (state == TreasureQuestGateState.Unlocked && unlockedSprite != null) gateImage.sprite = unlockedSprite;
            if (state == TreasureQuestGateState.Completed && completedSprite != null) gateImage.sprite = completedSprite;

            gateImage.color = Color.white;
        }

        if (gateLabel != null)
        {
            gateLabel.gameObject.SetActive(showGateLabel);
            gateLabel.text = gateNumber.ToString();
        }
    }

    public void PlayLockedFeedback()
    {
        if (rectTransform == null) return;
        rectTransform.DOKill();
        rectTransform.DOShakeAnchorPos(0.25f, 10f, 12, 90f);
    }

    public void PlayUnlockFeedback()
    {
        if (rectTransform == null) return;
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one;
        rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.75f);
    }

    private void HandleClick()
    {
        levelManager?.TryOpenGate(gateNumber, this);
    }
}
