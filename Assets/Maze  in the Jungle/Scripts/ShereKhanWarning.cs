using UnityEngine;

public class ShereKhanWarning : MonoBehaviour
{
    public GameManager gameManager;

    private CameraShake camShake;

    private void Start()
    {
        camShake = Camera.main.GetComponent<CameraShake>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.PlayLeafEffect();
            AudioManager.Instance.PlaySFXWithVolume(0, 0.5f);
            // 🔥 Floating text (FIXED position)
            GameObject textObj = Instantiate(
                gameManager.floatingTextPrefab,
                gameManager.prefabParent
            );

            textObj.GetComponent<FloatingText>().Show("Danger!", Color.yellow);

            // 🔥 Subtle camera shake (warning, not strong)
            if (camShake != null)
            {
                camShake.Shake(0.15f, 0.1f);
            }
            gameObject.SetActive(false);    
        }
    }
}