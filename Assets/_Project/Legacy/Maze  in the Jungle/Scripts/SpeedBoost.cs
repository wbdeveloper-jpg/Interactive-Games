using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpeedBoost : MonoBehaviour
{
    public float boostAmount = 2f;
    public float duration = 2f;
    public GameObject floatingTextPrefab;
    public Transform prefabParent;
    public GameManager manager;

    private bool consumed;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        consumed = false;
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
        player.ResetSprite();
        PlayPickupSfx();

        if (manager != null)
        {
            manager.IncreasePoint(2);
        }

        player.ApplyTemporarySpeedMultiplier(boostAmount, duration, true);
        SpawnFloatingText("Speed Up!", Color.yellow);
        gameObject.SetActive(false);
    }

    private void PlayPickupSfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(1);
        }
    }

    private void SpawnFloatingText(string message, Color color)
    {
        GameObject prefab = floatingTextPrefab;
        Transform parent = prefabParent;

        if (prefab == null && manager != null)
        {
            prefab = manager.floatingTextPrefab;
            parent = manager.prefabParent;
        }

        if (prefab == null)
        {
            return;
        }

        GameObject textObj = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.Show(message, color);
        }
    }

    private void OnValidate()
    {
        boostAmount = Mathf.Max(0f, boostAmount);
        duration = Mathf.Max(0f, duration);
    }
}
