using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class TimeBoost : MonoBehaviour
{
    public float timeAmount = 15f;

    private GameManager gameManager;
    private bool consumed;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    private void OnEnable()
    {
        consumed = false;
        transform.localScale = originalScale == Vector3.zero ? transform.localScale : originalScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        consumed = true;

        player.PlayAuraEffect();
        PlayPickupSfx();

        if (gameManager != null)
        {
            gameManager.ReduceTime(timeAmount);
            gameManager.IncreasePoint(1);
        }

        player.transform.DOKill(false);
        player.transform.DORotate(new Vector3(0f, 0f, 360f), 0.4f, RotateMode.FastBeyond360)
            .SetLink(player.gameObject);

        SpawnFloatingText(player.transform.position, "-" + Mathf.RoundToInt(timeAmount) + " sec", Color.cyan);

        transform.DOKill(false);
        transform.DOScale(originalScale * 1.3f, 0.2f)
            .SetLoops(2, LoopType.Yoyo)
            .SetLink(gameObject);

        Invoke(nameof(DisableSelf), 0.4f);
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void PlayPickupSfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(1);
        }
    }

    private void SpawnFloatingText(Vector3 pos, string message, Color color)
    {
        if (gameManager == null || gameManager.floatingTextPrefab == null)
        {
            return;
        }

        GameObject textObj = gameManager.prefabParent != null
            ? Instantiate(gameManager.floatingTextPrefab, gameManager.prefabParent)
            : Instantiate(gameManager.floatingTextPrefab);

        if (gameManager.prefabParent == null)
        {
            textObj.transform.position = pos;
        }

        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.Show(message, color);
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableSelf));
        transform.DOKill(false);
    }

    private void OnValidate()
    {
        timeAmount = Mathf.Max(0f, timeAmount);
    }
}
