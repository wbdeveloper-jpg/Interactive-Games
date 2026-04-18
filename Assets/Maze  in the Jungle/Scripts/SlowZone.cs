using UnityEngine;
using DG.Tweening;
using System.Collections;

public class SlowZone : MonoBehaviour
{
    public float slowFactor = 0.5f;
    public float duration = 2f;

    private GameManager gameManager;
    private SpriteRenderer sr;
    private CameraShake camShake;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        sr = GetComponent<SpriteRenderer>();
        camShake = Camera.main.GetComponent<CameraShake>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            StartCoroutine(SlowEffect(player));

            // 🔥 Camera shake (main impact)
            if (camShake != null)
                camShake.Shake(0.25f, 0.2f);

            // 🔥 Floating text
            player.PlaySmokeEffect();
            player.SetSprite();
            AudioManager.Instance.PlaySFX(0);
            SpawnFloatingText(player.transform.position, "You are injured", Color.red);

            // 🔥 Fade out Shere Khan
            if (sr != null)
            {
                sr.DOFade(0f, 0.3f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    IEnumerator SlowEffect(PlayerMovement player)
    {
        float originalSpeed = player.moveSpeed;
        player.moveSpeed *= slowFactor;

        yield return new WaitForSeconds(duration);

        player.moveSpeed = originalSpeed;
    }

    void SpawnFloatingText(Vector3 pos, string message, Color color)
    {
        if (gameManager == null || gameManager.floatingTextPrefab == null) return;

        GameObject textObj = Instantiate(
            gameManager.floatingTextPrefab,
            gameManager.prefabParent
        );

        textObj.GetComponent<FloatingText>().Show(message, color);
    }
}