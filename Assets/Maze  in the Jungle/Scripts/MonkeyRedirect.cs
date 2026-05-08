using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class MonkeyRedirect : MonoBehaviour
{
    public Transform[] pathPoints;
    public GameManager gameManager;
    public float moveSpeed = 5f;
    public Vector3 offset = new Vector3(0.5f, 0.5f, 0f);

    private bool isRedirecting;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isRedirecting)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        PlayMonkeySfx();

        if (gameManager != null)
        {
            gameManager.ReducePoint(2);
        }

        StartCoroutine(MovePlayer(player));
    }

    private IEnumerator MovePlayer(PlayerMovement player)
    {
        isRedirecting = true;

        bool playerWasEnabled = player.enabled;
        player.StopMovement();
        player.enabled = false;
        gameObject.SetActive(true);

        if (pathPoints != null)
        {
            foreach (Transform point in pathPoints)
            {
                if (point == null)
                {
                    continue;
                }

                while (player != null && Vector3.Distance(player.transform.position, point.position) > 0.05f)
                {
                    player.transform.position = Vector3.MoveTowards(
                        player.transform.position,
                        point.position,
                        Mathf.Max(0f, moveSpeed) * Time.deltaTime
                    );

                    transform.position = player.transform.position + offset;
                    yield return null;
                }
            }
        }

        if (player != null)
        {
            player.enabled = playerWasEnabled;
        }

        isRedirecting = false;
        gameObject.SetActive(false);
    }

    private void PlayMonkeySfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(4);
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
}
