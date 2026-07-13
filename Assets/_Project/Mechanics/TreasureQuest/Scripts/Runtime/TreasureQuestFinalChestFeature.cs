using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class TreasureQuestFinalChestFeature : MonoBehaviour
{
    [Header("Managers")]
    public TreasureQuestUIManager uiManager;
    public TreasureQuestLevelManager levelManager;
    public TreasureQuestAudioManager audioManager;

    [Header("Chest References")]
    public Button chestButton;
    public Image chestImage;
    public RectTransform chestRect;

    [Header("Completed Panel")]
    public GameObject completedPanel;
    public RectTransform completedCardRect;
    public Image completedTreasureImage;
    public Sprite completedTreasureSprite;
    public TMP_Text completedTitleText;
    public TMP_Text completedDetailsText;
    public Button playAgainResetButton;
    public Button closeButton;

    [Header("Coin / Treasure FX")]
    public ParticleSystem coinParticleSystem;
    public RectTransform uiCoinFxRoot;
    public Sprite uiCoinSprite;
    public bool useUiCoinFallback = true;
    [Range(6, 60)] public int uiCoinBurstCount = 24;
    public float uiCoinBurstDuration = 0.85f;

    [Header("Text")]
    public string lockedChestMessage = "Complete all 5 gates to open the treasure chest!";
    public string completedTitle = "Treasure Complete!";
    public string completedDetails = "You opened every gate and found the treasure.\nTap Play Again to reset the map.";

    [Header("Animation")]
    public float lockedShakeDuration = 0.25f;
    public float lockedShakeStrength = 14f;
    public float openPunchStrength = 0.12f;

    private bool isBound;
    private Coroutine coinBurstRoutine;

    private void Reset()
    {
        AutoFindReferences();
    }

    private void Awake()
    {
        AutoFindReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        if (!isBound)
            BindButtons();
    }

    private void OnDestroy()
    {
        if (chestButton != null)
            chestButton.onClick.RemoveListener(HandleChestClicked);

        if (playAgainResetButton != null)
            playAgainResetButton.onClick.RemoveListener(ResetEverythingAndPlayAgain);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HideCompletedPanel);
    }

    [ContextMenu("Auto Find References")]
    public void AutoFindReferences()
    {
        if (chestButton == null) chestButton = GetComponent<Button>();
        if (chestImage == null) chestImage = GetComponent<Image>();
        if (chestRect == null) chestRect = transform as RectTransform;

        if (uiManager == null) uiManager = FindObjectOfType<TreasureQuestUIManager>(true);
        if (levelManager == null) levelManager = FindObjectOfType<TreasureQuestLevelManager>(true);
        if (audioManager == null) audioManager = FindObjectOfType<TreasureQuestAudioManager>(true);
    }

    public void BindButtons()
    {
        if (chestButton == null)
            chestButton = GetComponent<Button>();

        if (chestButton != null)
        {
            chestButton.onClick.RemoveListener(HandleChestClicked);
            chestButton.onClick.AddListener(HandleChestClicked);
            chestButton.interactable = true;
        }

        if (playAgainResetButton != null)
        {
            playAgainResetButton.onClick.RemoveListener(ResetEverythingAndPlayAgain);
            playAgainResetButton.onClick.AddListener(ResetEverythingAndPlayAgain);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideCompletedPanel);
            closeButton.onClick.AddListener(HideCompletedPanel);
        }

        isBound = true;
    }

    public void HandleChestClicked()
    {
        AutoFindReferences();

        bool unlocked = levelManager != null && levelManager.IsFinalTreasureUnlocked();
        if (!unlocked)
        {
            audioManager?.PlayLocked();
            PlayLockedShake();
            uiManager?.ShowLockedGateFeedback(lockedChestMessage);
            return;
        }

        audioManager?.PlayUnlock();
        PlayOpenedChestFeedback();
        PlayTreasureFx();
        ShowCompletedPanel();
    }

    public void PlayLockedShake()
    {
        RectTransform target = chestRect != null ? chestRect : transform as RectTransform;
        if (target == null) return;

        target.DOKill();
        target.DOShakeAnchorPos(lockedShakeDuration, lockedShakeStrength, 12, 90f, false, true).SetUpdate(true);
    }

    public void PlayOpenedChestFeedback()
    {
        Transform target = chestRect != null ? chestRect : transform;
        if (target == null) return;

        target.DOKill();
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * openPunchStrength, 0.32f, 8, 0.75f).SetUpdate(true);
    }

    public void PlayTreasureFx()
    {
        if (coinParticleSystem != null)
        {
            PositionParticleNearChest();
            coinParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            coinParticleSystem.Play(true);
        }

        if (useUiCoinFallback && uiCoinFxRoot != null)
        {
            if (coinBurstRoutine != null)
                StopCoroutine(coinBurstRoutine);

            coinBurstRoutine = StartCoroutine(PlayUiCoinBurst());
        }
    }

    public void ShowCompletedPanel()
    {
        if (completedTitleText != null)
            completedTitleText.text = completedTitle;

        if (completedDetailsText != null)
            completedDetailsText.text = completedDetails;

        if (completedTreasureImage != null)
        {
            if (completedTreasureSprite != null)
                completedTreasureImage.sprite = completedTreasureSprite;

            completedTreasureImage.preserveAspect = true;
            completedTreasureImage.color = Color.white;
        }

        if (completedPanel == null) return;

        completedPanel.SetActive(true);
        RectTransform rect = completedPanel.transform as RectTransform;
        if (rect == null) return;

        rect.DOKill();
        rect.localScale = Vector3.one * 0.96f;
        rect.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void HideCompletedPanel()
    {
        if (completedPanel != null)
            completedPanel.SetActive(false);
    }

    public void ResetEverythingAndPlayAgain()
    {
        audioManager?.PlayClick();
        TreasureQuestSaveManager.ResetProgress();

        if (levelManager != null)
        {
            levelManager.LoadProgress();
            levelManager.RefreshMenu();
        }

        HideCompletedPanel();
    }

    private void PositionParticleNearChest()
    {
        if (coinParticleSystem == null || chestRect == null) return;

        Canvas canvas = chestRect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, chestRect.position);
        Camera targetCamera = Camera.main;
        if (targetCamera == null) return;

        float depth = Mathf.Abs(targetCamera.transform.position.z) + 8f;
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        coinParticleSystem.transform.position = worldPoint;
    }

    private IEnumerator PlayUiCoinBurst()
    {
        ClearUiCoinFxRoot();

        Vector2 origin = GetChestLocalPointInsideFxRoot();
        int count = Mathf.Max(1, uiCoinBurstCount);

        for (int i = 0; i < count; i++)
        {
            Image coin = CreateRuntimeCoinImage(i);
            if (coin == null) continue;

            RectTransform coinRect = coin.rectTransform;
            coinRect.SetParent(uiCoinFxRoot, false);
            coinRect.anchoredPosition = origin;
            coinRect.localScale = Vector3.one * Random.Range(0.55f, 0.95f);

            Vector2 randomEnd = origin + new Vector2(Random.Range(-280f, 280f), Random.Range(110f, 310f));
            Vector2 fallEnd = randomEnd + new Vector2(Random.Range(-80f, 80f), Random.Range(-280f, -420f));
            float duration = Random.Range(uiCoinBurstDuration * 0.75f, uiCoinBurstDuration * 1.15f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(coinRect.DOAnchorPos(randomEnd, duration * 0.42f).SetEase(Ease.OutQuad));
            sequence.Append(coinRect.DOAnchorPos(fallEnd, duration * 0.58f).SetEase(Ease.InQuad));
            sequence.Join(coinRect.DORotate(new Vector3(0f, 0f, Random.Range(-280f, 280f)), duration, RotateMode.FastBeyond360));
            sequence.Join(coin.DOFade(0f, duration * 0.35f).SetDelay(duration * 0.65f));
            sequence.OnComplete(() =>
            {
                if (coin != null)
                    Destroy(coin.gameObject);
            });
        }

        yield return new WaitForSecondsRealtime(uiCoinBurstDuration + 0.35f);
        ClearUiCoinFxRoot();
    }

    private Image CreateRuntimeCoinImage(int index)
    {
        GameObject coinObject = new GameObject("RuntimeCoin_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image coin = coinObject.GetComponent<Image>();
        coin.sprite = uiCoinSprite;
        coin.color = new Color(1f, 0.78f, 0.18f, 1f);
        coin.raycastTarget = false;

        RectTransform rect = coin.rectTransform;
        rect.sizeDelta = new Vector2(42f, 42f);
        return coin;
    }

    private Vector2 GetChestLocalPointInsideFxRoot()
    {
        if (uiCoinFxRoot == null || chestRect == null)
            return Vector2.zero;

        Canvas canvas = uiCoinFxRoot.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, chestRect.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCoinFxRoot, screenPoint, camera, out Vector2 localPoint);
        return localPoint;
    }

    private void ClearUiCoinFxRoot()
    {
        if (uiCoinFxRoot == null) return;

        for (int i = uiCoinFxRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = uiCoinFxRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
