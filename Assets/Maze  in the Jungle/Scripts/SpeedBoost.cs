using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    public float boostAmount = 2f;
    public float duration = 2f;
    public GameObject floatingTextPrefab;
    public Transform prefabParent;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("It is being trigged");
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.PlayAuraEffect();
            player.ResetSprite();
            AudioManager.Instance.PlaySFX(1);
            player.StartCoroutine(ApplyBoost(player));
            gameObject.SetActive(false);
        }
    }

    System.Collections.IEnumerator ApplyBoost(PlayerMovement player)
    {
        float originalSpeed = player.moveSpeed;
        player.GetComponent<TrailRenderer>().enabled = true;
        player.moveSpeed *= boostAmount;
        Instantiate(floatingTextPrefab, prefabParent)
        .GetComponent<FloatingText>()
        .Show("Speed Up!", Color.yellow);
        yield return new WaitForSeconds(duration);

        player.moveSpeed = originalSpeed;
        player.GetComponent<TrailRenderer>().enabled = false;
    }
}