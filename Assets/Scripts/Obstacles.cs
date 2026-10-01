using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Obstacles : MonoBehaviour
{
    [Header("Min Max Scale")]
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    [Header("Min Max Speed")]
    public float minSpeed = 2.5f;
    public float maxSpeed = 5f;

    [Header("Spawn Rotation")]
    public bool randomizeSpawnRotation = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private ObstacleManager manager;
    private Color originalColor;
    private Vector3 targetScale;
    private float lifetime;
    private float slowSpeedThreshold;
    private float growDuration;
    private float fadeDuration;
    private float age;
    private float despawnTimer;
    private float effectTimer;
    private bool isDespawning;
    private bool isManaged;

    public void Initialize(
        ObstacleManager obstacleManager,
        float obstacleLifetime,
        float minSpeedToLive,
        float spawnDuration,
        float despawnDuration)
    {
        manager = obstacleManager;
        lifetime = obstacleLifetime;
        slowSpeedThreshold = Mathf.Max(0f, minSpeedToLive);
        growDuration = Mathf.Max(0f, spawnDuration);
        fadeDuration = Mathf.Max(0f, despawnDuration);
        isManaged = true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        float scaleMin = Mathf.Max(0f, Mathf.Min(minScale, maxScale));
        float scaleMax = Mathf.Max(scaleMin, Mathf.Max(minScale, maxScale));
        float scale = Random.Range(scaleMin, scaleMax);
        float speedMin = Mathf.Max(0f, Mathf.Min(minSpeed, maxSpeed));
        float speedMax = Mathf.Max(speedMin, Mathf.Max(minSpeed, maxSpeed));
        float speed = Random.Range(speedMin, speedMax);

        if (randomizeSpawnRotation)
        {
            rb.rotation = Random.Range(0f, 360f);
        }

        targetScale = transform.localScale * scale;
        transform.localScale = growDuration > 0f ? Vector3.zero : targetScale;
        if (growDuration <= 0f)
        {
            effectTimer = growDuration;
        }

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        rb.AddForce(direction * speed, ForceMode2D.Impulse);
    }

    void Update()
    {
        age += Time.deltaTime;
        UpdateSpawnEffect();

        if (!isDespawning)
        {
            if (age >= lifetime || rb.linearVelocity.magnitude < slowSpeedThreshold)
            {
                BeginDespawn();
            }
        }

        if (isDespawning)
        {
            UpdateDespawnEffect();
        }
    }

    void UpdateSpawnEffect()
    {
        if (effectTimer >= growDuration)
        {
            return;
        }

        effectTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(effectTimer / growDuration);
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        transform.localScale = targetScale * easedProgress;
    }

    void BeginDespawn()
    {
        isDespawning = true;
        despawnTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (fadeDuration <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void UpdateDespawnEffect()
    {
        if (fadeDuration <= 0f)
        {
            return;
        }

        despawnTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(despawnTimer / fadeDuration);
        Color color = originalColor;
        color.a = originalColor.a * (1f - progress);
        spriteRenderer.color = color;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (isManaged && manager != null)
        {
            manager.NotifyObstacleDestroyed();
        }
    }
}
