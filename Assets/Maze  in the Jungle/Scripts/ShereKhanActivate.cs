using UnityEngine;

public class ShereKhanActivate : MonoBehaviour
{
    public GameObject shereKhan;
    public Transform spawnPoint;

    private bool triggered = false;

    private CameraShake camShake;

    private void Start()
    {
        camShake = Camera.main.GetComponent<CameraShake>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null && !triggered)
        {
            player.PlayLeafEffect();
            AudioManager.Instance.PlaySFXWithVolume(0, 0.7f);

            triggered = true;

            // 🔥 Place Shere Khan
            shereKhan.transform.position = spawnPoint.position;

            // 🔥 Activate Shere Khan
            shereKhan.SetActive(true);

            // 🔥 CAMERA SHAKE (impact moment)
            if (camShake != null)
            {
                camShake.Shake(0.3f, 0.25f);
            }

            gameObject.SetActive(false);
        }
    }

    
}