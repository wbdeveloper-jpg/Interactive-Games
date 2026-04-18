using UnityEngine;
using System.Collections;

public class MonkeyRedirect : MonoBehaviour
{
    public Transform[] pathPoints;
    public float moveSpeed = 5f;

    public Vector3 offset = new Vector3(0.5f, 0.5f, 0); // monkey position relative to player

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            AudioManager.Instance.PlaySFX(4);
            StartCoroutine(MovePlayer(player));
        }
    }

    IEnumerator MovePlayer(PlayerMovement player)
    {
        player.StopMovement();
        player.enabled = false;

        // Activate monkey (in case it's hidden)
        gameObject.SetActive(true);

        foreach (Transform point in pathPoints)
        {
            while (Vector3.Distance(player.transform.position, point.position) > 0.05f)
            {
                // Move player
                player.transform.position = Vector3.MoveTowards(
                    player.transform.position,
                    point.position,
                    moveSpeed * Time.deltaTime
                );

                // 🔥 Move monkey with player (drag effect)
                transform.position = player.transform.position + offset;

                yield return null;
            }
        }

        // 🔥 Hide monkey after reaching destination
        gameObject.SetActive(false);

        player.enabled = true;
    }
}