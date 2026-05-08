using UnityEngine;
using DG.Tweening;

public class SlowZone : MonoBehaviour
{
    [Header("Slow Settings")]
    public float slowFactor = 0.5f;
    public float duration = 2f;

    [Header("Floating Text")]
    public string injuredMessage = "I am injured!";
    public Vector2 floatingTextAnchoredPosition = new Vector2(0f, -260f);
    public bool useAdaptiveLowerPosition = true;

    [Range(0.1f, 0.45f)]
    public float lowerScreenPercent = 0.28f;

    private GameManager gameManager;
    private SpriteRenderer sr;
    private CameraShake camShake;
    private bool triggered;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        sr = GetComponent<SpriteRenderer>();

        if (Camera.main != null)
            camShake = Camera.main.GetComponent<CameraShake>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player == null)
            return;

        triggered = true;

        if (gameManager != null)
            gameManager.ReducePoint(3);

        ApplySlowSafely(player);

        if (camShake != null)
            camShake.Shake(0.25f, 0.2f);

        player.PlaySmokeEffect();
        player.SetSprite();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(0);

        SpawnFloatingText(injuredMessage, Color.red);

        FadeAndDisable();
    }

    private void ApplySlowSafely(PlayerMovement player)
    {
        if (player == null)
            return;

        player.ApplyTemporarySpeedMultiplier(slowFactor, duration, this);
    }

    private void SpawnFloatingText(string message, Color color)
    {
        if (gameManager == null || gameManager.floatingTextPrefab == null || gameManager.prefabParent == null)
            return;

        GameObject textObj = Instantiate(gameManager.floatingTextPrefab, gameManager.prefabParent);

        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        if (floatingText == null)
        {
            Destroy(textObj);
            return;
        }

        floatingText.Show(message, color, GetLowerFloatingTextPosition());
    }

    private Vector2 GetLowerFloatingTextPosition()
    {
        RectTransform parentRect = gameManager != null
            ? gameManager.prefabParent as RectTransform
            : null;

        if (useAdaptiveLowerPosition && parentRect != null && parentRect.rect.height > 1f)
        {
            return new Vector2(
                floatingTextAnchoredPosition.x,
                -parentRect.rect.height * lowerScreenPercent
            );
        }

        return floatingTextAnchoredPosition;
    }

    private void FadeAndDisable()
    {
        if (sr != null)
        {
            sr.DOKill();
            sr.DOFade(0f, 0.3f)
                .OnComplete(() => gameObject.SetActive(false))
                .SetLink(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (sr != null)
            sr.DOKill();
    }
}