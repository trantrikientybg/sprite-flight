using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class Obstacles : MonoBehaviour
{
    [Header("Min Max Scale")]
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    [Header("Spawn Rotation")]
    public bool randomizeSpawnRotation = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polygonCollider;
    private readonly List<Vector2> physicsShape = new List<Vector2>();
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
    private float initialMovementSpeed;
    private bool isDespawning;
    private bool isManaged;

    public void Initialize(
        ObstacleManager obstacleManager,
        float obstacleLifetime,
        float minSpeedToLive,
        float spawnDuration,
        float despawnDuration,
        float movementSpeed)
    {
        manager = obstacleManager;
        lifetime = obstacleLifetime;
        slowSpeedThreshold = Mathf.Max(0f, minSpeedToLive);
        growDuration = Mathf.Max(0f, spawnDuration);
        fadeDuration = Mathf.Max(0f, despawnDuration);
        initialMovementSpeed = Mathf.Max(0f, movementSpeed);
        isManaged = true;
    }

    public bool SetSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("Cannot assign a null sprite to an obstacle.", this);
            return false;
        }

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
        {
            Debug.LogError("The obstacle sprite has no Physics Shape. Generate or define one in the Sprite Editor.", this);
            return false;
        }

        List<Vector2[]> paths = new List<Vector2[]>(shapeCount);
        for (int i = 0; i < shapeCount; i++)
        {
            physicsShape.Clear();
            sprite.GetPhysicsShape(i, physicsShape);
            if (physicsShape.Count < 3)
            {
                Debug.LogError("The obstacle sprite contains an invalid Physics Shape with fewer than three points.", this);
                return false;
            }

            paths.Add(physicsShape.ToArray());
        }

        spriteRenderer.sprite = sprite;
        polygonCollider.pathCount = paths.Count;
        for (int i = 0; i < paths.Count; i++)
        {
            polygonCollider.SetPath(i, paths[i]);
        }

        return true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        float scaleMin = Mathf.Max(0f, Mathf.Min(minScale, maxScale));
        float scaleMax = Mathf.Max(scaleMin, Mathf.Max(minScale, maxScale));
        float scale = Random.Range(scaleMin, scaleMax);

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

        rb.linearVelocity = direction * initialMovementSpeed;
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

    public void SetMovementSpeed(float speed)
    {
        if (rb == null || isDespawning)
        {
            return;
        }

        Vector2 direction = rb.linearVelocity.sqrMagnitude > Mathf.Epsilon
            ? rb.linearVelocity.normalized
            : Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector2.right;
        }

        rb.linearVelocity = direction * Mathf.Max(0f, speed);
    }

    public void ApplyScaleMultiplier(float multiplier)
    {
        if (multiplier > 0f)
        {
            transform.localScale *= multiplier;
        }
    }

    public void DisassociateFromManager()
    {
        isManaged = false;
        manager = null;
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
            manager.NotifyObstacleDestroyed(this);
        }
    }
}
