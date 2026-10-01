using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Particles : MonoBehaviour
{
    private ParticlesController controller;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector2 movementDirection;
    private float speed;
    private float particleScale;
    private float particleLifetime;
    private float elapsedLifetime;
    private float currentScale;
    private float pulseTargetScale;
    private bool isGrowing = true;
    private bool hasExpired;

    public void Initialize(ParticlesController particleController)
    {
        controller = particleController;
    }

    void Start()
    {
        if (controller == null)
        {
            Debug.LogError("Particles must be spawned by a ParticlesController.", this);
            Destroy(gameObject);
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        float minScale = Mathf.Max(0f, Mathf.Min(controller.minParticleScale, controller.maxParticleScale));
        float maxScale = Mathf.Max(minScale, Mathf.Max(controller.minParticleScale, controller.maxParticleScale));
        particleScale = Random.Range(minScale, maxScale);
        currentScale = particleScale;
        transform.localScale = Vector3.one * particleScale;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        movementDirection = Random.insideUnitCircle.normalized;
        if (movementDirection == Vector2.zero)
        {
            movementDirection = Vector2.right;
        }

        float minSpeed = Mathf.Max(0f, Mathf.Min(controller.minParticleSpeed, controller.maxParticleSpeed));
        float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(controller.minParticleSpeed, controller.maxParticleSpeed));
        speed = Random.Range(minSpeed, maxSpeed);

        float minLifetime = Mathf.Max(0f, Mathf.Min(controller.minParticleLifetime, controller.maxParticleLifetime));
        float maxLifetime = Mathf.Max(minLifetime, Mathf.Max(controller.minParticleLifetime, controller.maxParticleLifetime));
        particleLifetime = Random.Range(minLifetime, maxLifetime);

        ChoosePulseTarget();
    }

    void Update()
    {
        if (controller == null || hasExpired)
        {
            return;
        }

        elapsedLifetime += Time.deltaTime;
        if (elapsedLifetime >= particleLifetime)
        {
            Expire();
            return;
        }

        transform.position += (Vector3)(movementDirection * speed * Time.deltaTime);
        UpdatePulse();

        float lifePercent = elapsedLifetime / particleLifetime;
        Color color = originalColor;
        color.a = originalColor.a * (1f - lifePercent);
        spriteRenderer.color = color;
    }

    void UpdatePulse()
    {
        float pulseStep = Mathf.Max(0f, controller.pulseScaleStep);
        currentScale = Mathf.MoveTowards(currentScale, pulseTargetScale, pulseStep);
        transform.localScale = Vector3.one * currentScale;

        if (!Mathf.Approximately(currentScale, pulseTargetScale))
        {
            return;
        }

        isGrowing = !isGrowing;
        ChoosePulseTarget();
    }

    void ChoosePulseTarget()
    {
        float pulseRange = Mathf.Max(0f, controller.pulseScaleRange);
        if (isGrowing)
        {
            pulseTargetScale = Random.Range(particleScale, particleScale + pulseRange);
        }
        else
        {
            pulseTargetScale = Random.Range(Mathf.Max(0f, particleScale - pulseRange), particleScale);
        }
    }

    void Expire()
    {
        hasExpired = true;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (controller != null)
        {
            controller.NotifyParticleExpired();
        }
    }
}
