using UnityEngine;

public class PowerupPickup : MonoBehaviour
{
    private PowerupManager manager;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float lifetime;
    private float fadeDuration;
    private float flySpeed;
    private float age;
    private Vector3 targetPosition;
    private Transform targetTransform;
    private bool initialized;
    private bool collected;

    public bool Initialize(
        PowerupManager powerupManager,
        float pickupLifetime,
        float pickupFadeDuration,
        Vector3 flyTarget,
        float movementSpeed,
        Transform movingTarget = null)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length == 0)
        {
            Debug.LogError("A power-up prefab must have a Collider2D.", this);
            return false;
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers.Length == 0)
        {
            Debug.LogError("A power-up prefab must have a SpriteRenderer.", this);
            return false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        manager = powerupManager;
        lifetime = Mathf.Max(0f, pickupLifetime);
        fadeDuration = Mathf.Min(Mathf.Max(0f, pickupFadeDuration), lifetime);
        targetPosition = flyTarget;
        targetTransform = movingTarget;
        flySpeed = Mathf.Max(0f, movementSpeed);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        initialized = true;
        return true;
    }

    void Update()
    {
        if (!initialized || collected)
        {
            return;
        }

        age += Time.deltaTime;
        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            flySpeed * Time.deltaTime);

        if (fadeDuration > 0f && age >= lifetime - fadeDuration)
        {
            float fadeProgress = Mathf.Clamp01((age - (lifetime - fadeDuration)) / fadeDuration);
            SetOpacity(1f - fadeProgress);
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryCollect(collision.gameObject);
    }

    void TryCollect(GameObject other)
    {
        if (collected || !initialized)
        {
            return;
        }

        PlayerControl collectingPlayer = other.GetComponentInParent<PlayerControl>();
        if (collectingPlayer == null)
        {
            return;
        }

        collected = true;
        manager.Collect(this, collectingPlayer);
    }

    void SetOpacity(float opacity)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color color = originalColors[i];
            color.a *= opacity;
            spriteRenderers[i].color = color;
        }
    }
}
