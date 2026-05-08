using UnityEngine;

public class ShereKhanWarning : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    [Header("Floating Text")]
    public string warningMessage = "Danger!";
    public Vector2 floatingTextAnchoredPosition = new Vector2(0f, -260f);
    public bool useAdaptiveLowerPosition = true;

    [Range(0.1f, 0.45f)]
    public float lowerScreenPercent = 0.28f;

    private CameraShake camShake;
    private bool triggered;

    private void Start()
    {
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
            gameManager.ReducePoint(1);

        player.PlayLeafEffect();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXWithVolume(0, 0.5f);

        SpawnFloatingText(warningMessage, Color.yellow);

        if (camShake != null)
            camShake.Shake(0.15f, 0.1f);

        gameObject.SetActive(false);
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
}