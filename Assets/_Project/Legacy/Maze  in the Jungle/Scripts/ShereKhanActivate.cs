using UnityEngine;

public class ShereKhanActivate : MonoBehaviour
{
    [Header("References")]
    public GameObject shereKhan;
    public Transform spawnPoint;
    public GameManager gameManager;

    [Header("Floating Text")]
    public string shereKhanVisibleMessage = "Shere Khan is here!";
    public Vector2 floatingTextAnchoredPosition = new Vector2(0f, -260f);
    public bool useAdaptiveLowerPosition = true;

    [Range(0.1f, 0.45f)]
    public float lowerScreenPercent = 0.28f;

    private bool triggered = false;
    private CameraShake camShake;

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

        player.PlayLeafEffect();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXWithVolume(0, 0.7f);

        if (gameManager != null)
            gameManager.ReducePoint(2);

        if (shereKhan != null && spawnPoint != null)
        {
            shereKhan.transform.position = spawnPoint.position;
            shereKhan.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{nameof(ShereKhanActivate)} on {name} is missing Shere Khan or Spawn Point reference.");
        }

        SpawnFloatingText(shereKhanVisibleMessage, Color.red);

        if (camShake != null)
            camShake.Shake(0.3f, 0.25f);

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