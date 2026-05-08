using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.25f;

    [Header("Effects")]
    public ParticleSystem leafEffect;
    public ParticleSystem smokeEffect;
    public ParticleSystem auraEffect;

    private Vector2 moveDirection = Vector2.zero;

    [Header("Movement Animation")]
    private Vector3 originalScale;
    public float scaleAmount = 0.08f;
    public float animationSpeed = 10f;

    [Header("Sprite State")]
    public SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    public Sprite injuredSprite;

    private TrailRenderer trailRenderer;
    private float baseMoveSpeed;
    private int nextSpeedModifierId;
    private readonly Dictionary<int, float> activeSpeedModifiers = new Dictionary<int, float>();

    private void Awake()
    {
        originalScale = transform.localScale;
        baseMoveSpeed = Mathf.Max(0f, moveSpeed);
        trailRenderer = GetComponent<TrailRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
    }

    private void Start()
    {
        SetTrailVisible(false);
    }

    private void Update()
    {
        HandleKeyboardInput();
        MoveAndAnimate();
    }

    private void OnDisable()
    {
        StopMovement();
        ResetMovementVisuals();
    }

    private void MoveAndAnimate()
    {
        if (moveDirection == Vector2.zero)
        {
            ResetMovementVisuals();
            return;
        }

        Vector3 moveDir3 = new Vector3(moveDirection.x, moveDirection.y, 0f);
        Vector3 move = moveDir3 * moveSpeed * Time.deltaTime;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDirection, wallCheckDistance, wallLayer);
        if (hit.collider == null)
        {
            transform.position += move;
            ApplyMovingVisuals();
        }
        else
        {
            transform.localScale = originalScale * 0.95f;
        }
    }

    private void ApplyMovingVisuals()
    {
        float scale = 1f + Mathf.Sin(Time.time * animationSpeed) * scaleAmount;
        transform.localScale = originalScale * scale;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    private void ResetMovementVisuals()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
    }

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
    }

    public void StopMovement()
    {
        moveDirection = Vector2.zero;
    }

    public void PlayLeafEffect()
    {
        PlayParticle(leafEffect);
    }

    public void PlayAuraEffect()
    {
        PlayParticle(auraEffect);
    }

    public void PlaySmokeEffect()
    {
        PlayParticle(smokeEffect);
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle != null)
        {
            particle.Play();
        }
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
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }
    }

    public void SetBaseMoveSpeed(float newBaseSpeed)
    {
        baseMoveSpeed = Mathf.Max(0f, newBaseSpeed);
        RecalculateMoveSpeed();
    }

    public Coroutine ApplyTemporarySpeedMultiplier(float multiplier, float duration, bool showTrail = false)
    {
        multiplier = Mathf.Max(0f, multiplier);
        duration = Mathf.Max(0f, duration);
        return StartCoroutine(TemporarySpeedRoutine(multiplier, duration, showTrail));
    }

    private IEnumerator TemporarySpeedRoutine(float multiplier, float duration, bool showTrail)
    {
        int id = ++nextSpeedModifierId;
        activeSpeedModifiers[id] = multiplier;
        RecalculateMoveSpeed();

        if (showTrail)
        {
            SetTrailVisible(true);
        }

        yield return new WaitForSeconds(duration);

        activeSpeedModifiers.Remove(id);
        RecalculateMoveSpeed();

        if (showTrail && !HasAnySpeedBoostAboveBase())
        {
            SetTrailVisible(false);
        }
    }

    private void RecalculateMoveSpeed()
    {
        float finalSpeed = baseMoveSpeed;
        foreach (float modifier in activeSpeedModifiers.Values)
        {
            finalSpeed *= modifier;
        }

        moveSpeed = finalSpeed;
    }

    private bool HasAnySpeedBoostAboveBase()
    {
        foreach (float modifier in activeSpeedModifiers.Values)
        {
            if (modifier > 1f)
            {
                return true;
            }
        }

        return false;
    }

    private void SetTrailVisible(bool visible)
    {
        if (trailRenderer != null)
        {
            trailRenderer.enabled = visible;
        }
    }

    private void HandleKeyboardInput()
    {
        Vector2 dir = Vector2.zero;

        if (Input.GetKey(KeyCode.W)) dir = Vector2.up;
        else if (Input.GetKey(KeyCode.S)) dir = Vector2.down;
        else if (Input.GetKey(KeyCode.A)) dir = Vector2.left;
        else if (Input.GetKey(KeyCode.D)) dir = Vector2.right;
        else if (Input.GetKey(KeyCode.UpArrow)) dir = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) dir = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = Vector2.right;

        if (dir != Vector2.zero)
        {
            moveDirection = dir;
        }
    }
}
