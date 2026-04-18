using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public LayerMask wallLayer;
    public ParticleSystem leafEffect;
    public ParticleSystem smokeEffect;
    public ParticleSystem auraEffect;
    private Vector2 moveDirection = Vector2.zero;

    // Animation
    private Vector3 originalScale;
    public float scaleAmount = 0.08f;
    public float animationSpeed = 10f;

    //sprite
    public SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    public Sprite injuredSprite;

    void Start()
    {
        originalScale = transform.localScale;
        GetComponent<TrailRenderer>().enabled = false;

        //spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
    }

    void Update()
    {
        HandleKeyboardInput();

        if (moveDirection != Vector2.zero)
        {
            Vector3 moveDir3 = new Vector3(moveDirection.x, moveDirection.y, 0);
            Vector3 move = moveDir3 * moveSpeed * Time.deltaTime;

            // Better collision check
            float checkDistance = 0.25f;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDirection, checkDistance, wallLayer);

            if (hit.collider == null)
            {
                transform.position += move;

                // Bounce animation
                float scale = 1 + Mathf.Sin(Time.time * animationSpeed) * scaleAmount;
                transform.localScale = originalScale * scale;

                // Smooth rotation
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
            else
            {
                // Small feedback on wall hit
                transform.localScale = originalScale * 0.95f;
            }
        }
        else
        {
            // Reset when idle
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir;
    }

    public void StopMovement()
    {
        moveDirection = Vector2.zero;
    }

    public void PlayLeafEffect()
    {
        leafEffect.Play();
    }
    public void PlayAuraEffect()
    {
        auraEffect.Play();  
    }

    public void PlaySmokeEffect()
    {
        smokeEffect.Play();
    }

    public void MoveUp() => SetDirection(Vector2.up);
    public void MoveDown() => SetDirection(Vector2.down);
    public void MoveLeft() => SetDirection(Vector2.left);
    public void MoveRight() => SetDirection(Vector2.right);

    public void SetSprite()
    {
        if (spriteRenderer != null && injuredSprite != null)
        {
            spriteRenderer.sprite = injuredSprite;
        }
    }

    public void ResetSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = originalSprite;
        }
    }

    void HandleKeyboardInput()
    {
        Vector2 dir = Vector2.zero;

        // WASD
        if (Input.GetKey(KeyCode.W)) dir = Vector2.up;
        else if (Input.GetKey(KeyCode.S)) dir = Vector2.down;
        else if (Input.GetKey(KeyCode.A)) dir = Vector2.left;
        else if (Input.GetKey(KeyCode.D)) dir = Vector2.right;

        // Arrow keys (fallback)
        else if (Input.GetKey(KeyCode.UpArrow)) dir = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) dir = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = Vector2.right;

        // 🔥 Apply only if keyboard is used
        if (dir != Vector2.zero)
        {
            moveDirection = dir;
        }
    }
}