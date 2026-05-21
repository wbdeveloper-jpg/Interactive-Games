using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class ShereKhanAttack : MonoBehaviour
{
    public GameObject shereKhan;
    public float appearDistance = 1.5f;

    public float slowFactor = 0.5f;
    public float slowDuration = 2f;

    private bool triggered = false;
    private CameraShake camShake;
    private GameManager gameManager;

    private void Start()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            camShake = mainCamera.GetComponent<CameraShake>();
        }

        gameManager = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        triggered = true;
        PlayAttackSequence(player);
        SpawnText(player.transform.position, "Shere Khan!", Color.red);
        player.ApplyTemporarySpeedMultiplier(slowFactor, slowDuration, false);
    }

    private void PlayAttackSequence(PlayerMovement player)
    {
        if (shereKhan != null)
        {
            Vector3 spawnPos = player.transform.position + new Vector3(appearDistance, 0f, 0f);
            shereKhan.transform.DOKill(false);
            shereKhan.transform.position = spawnPos;
            shereKhan.SetActive(true);
            shereKhan.transform.localScale = Vector3.zero;

            shereKhan.transform.DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
                .SetLink(shereKhan);

            shereKhan.transform.DOMove(player.transform.position, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(shereKhan);
        }

        if (camShake != null)
        {
            camShake.Shake(0.3f, 0.25f);
        }

        player.transform.DOKill(false);
        player.transform.DOShakePosition(0.2f, 0.1f).SetLink(player.gameObject);
    }

    private void SpawnText(Vector3 pos, string message, Color color)
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

    private void OnValidate()
    {
        appearDistance = Mathf.Max(0f, appearDistance);
        slowFactor = Mathf.Max(0f, slowFactor);
        slowDuration = Mathf.Max(0f, slowDuration);
    }
}
