using UnityEngine;
using DG.Tweening;
using System.Collections;

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
        camShake = Camera.main.GetComponent<CameraShake>();
        gameManager = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null && !triggered)
        {
            triggered = true;

            // 🔥 Spawn near player
            Vector3 spawnPos = player.transform.position + new Vector3(appearDistance, 0, 0);

            shereKhan.transform.position = spawnPos;
            shereKhan.SetActive(true);

            // 🔥 Dramatic scale pop
            shereKhan.transform.localScale = Vector3.zero;
            shereKhan.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

            // 🔥 Rush slightly toward player
            shereKhan.transform.DOMove(player.transform.position, 0.25f).SetEase(Ease.OutQuad);

            // 🔥 CAMERA SHAKE (MAIN IMPACT)
            if (camShake != null)
                camShake.Shake(0.3f, 0.25f);

            // 🔥 Player reaction
            player.transform.DOShakePosition(0.2f, 0.1f);

            // 🔥 Floating text
            SpawnText(player.transform.position, "Shere Khan!", Color.red);

            // 🔥 Apply slow
            StartCoroutine(ApplySlow(player));
        }
    }

    IEnumerator ApplySlow(PlayerMovement player)
    {
        float originalSpeed = player.moveSpeed;
        player.moveSpeed *= slowFactor;

        yield return new WaitForSeconds(slowDuration);

        player.moveSpeed = originalSpeed;
    }

    void SpawnText(Vector3 pos, string message, Color color)
    {
        if (gameManager == null || gameManager.floatingTextPrefab == null) return;

        GameObject textObj = Instantiate(
            gameManager.floatingTextPrefab,
            gameManager.prefabParent
        );

        textObj.GetComponent<FloatingText>().Show(message, color);
    }
}