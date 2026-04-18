using UnityEngine;
using DG.Tweening;

public class TimeBoost : MonoBehaviour
{
    public float timeAmount = 15f;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.PlayAuraEffect();
            AudioManager.Instance.PlaySFX(1);
            // 🔥 Reduce elapsed time
            gameManager.ReduceTime(timeAmount);

            // 🔥 Player feedback (spin effect)
            player.transform.DORotate(new Vector3(0, 0, 360), 0.4f, RotateMode.FastBeyond360);

            // 🔥 Floating text (-15 sec)
            SpawnFloatingText(player.transform.position, "-15 sec", Color.cyan);

            // 🔥 Small scale pop for Kaa object
            transform.DOScale(1.3f, 0.2f).SetLoops(2, LoopType.Yoyo);

            // Disable after short delay (so animation plays)
            Invoke(nameof(DisableSelf), 0.2f);
        }
    }

    void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    void SpawnFloatingText(Vector3 pos, string message, Color color)
    {
        GameObject prefab = gameManager.floatingTextPrefab;
        if (prefab == null) return;

        GameObject textObj = Instantiate(prefab, gameManager.prefabParent);
        textObj.GetComponent<FloatingText>().Show(message, color);
    }
}