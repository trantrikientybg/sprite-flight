using UnityEngine;

public class Meteor : MonoBehaviour
{
    private static readonly Vector2 TravelDirection = new Vector2(-1f, -0.35f).normalized;
    private const float FadeFrontWidth = 0.15f;

    private MeteorAndStarManager manager;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float[] rendererCenters;
    private float tailLocalY;
    private float bodyLength;
    private float flightDuration;
    private float fadeInDuration;
    private float fadeOutDuration;
    private float age;
    private float fadeTimer;
    private float fullScaleY;
    private float scaleX;
    private float scaleZ;
    private Vector3 tailAnchor;
    private bool isFadingOut;

    void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public bool Initialize(
        MeteorAndStarManager meteorManager,
        float scaleMultiplier,
        float movementSpeed,
        float travelDistance,
        float fadeInTime,
        float fadeOutTime,
        int sortingOrder,
        Vector3 fixedTailPosition)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("The meteor prefab must contain at least one SpriteRenderer.", this);
            return false;
        }

        transform.rotation = Quaternion.FromToRotation(Vector3.up, -TravelDirection);
        transform.localScale *= Mathf.Max(0f, scaleMultiplier);
        tailAnchor = fixedTailPosition;
        fadeInDuration = Mathf.Max(0f, fadeInTime);
        fadeOutDuration = Mathf.Max(0f, fadeOutTime);
        flightDuration = movementSpeed <= Mathf.Epsilon
            ? 0f
            : Mathf.Max(0.1f, travelDistance) / movementSpeed;

        originalColors = new Color[spriteRenderers.Length];
        rendererCenters = new float[spriteRenderers.Length];
        Vector3 localMin = Vector3.one * float.PositiveInfinity;
        Vector3 localMax = Vector3.one * float.NegativeInfinity;
        bool hasSprite = false;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            originalColors[i] = spriteRenderer.color;
            spriteRenderer.sortingOrder = sortingOrder;
            if (spriteRenderer.sprite == null)
            {
                continue;
            }

            hasSprite = true;
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 localCorner = new Vector3(
                    (corner & 1) == 0 ? spriteBounds.min.x : spriteBounds.max.x,
                    (corner & 2) == 0 ? spriteBounds.min.y : spriteBounds.max.y,
                    (corner & 4) == 0 ? spriteBounds.min.z : spriteBounds.max.z);
                Vector3 rootLocalCorner = transform.InverseTransformPoint(
                    spriteRenderer.transform.TransformPoint(localCorner));
                localMin = Vector3.Min(localMin, rootLocalCorner);
                localMax = Vector3.Max(localMax, rootLocalCorner);
            }
        }

        bodyLength = localMax.y - localMin.y;
        if (!hasSprite || bodyLength <= Mathf.Epsilon)
        {
            Debug.LogError("The meteor prefab sprites must have a non-zero length along their local Y axis.", this);
            return false;
        }

        tailLocalY = localMax.y;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            rendererCenters[i] = spriteRenderers[i].sprite == null
                ? tailLocalY
                : GetRendererCenterY(spriteRenderers[i]);
        }

        scaleX = transform.localScale.x;
        scaleZ = transform.localScale.z;
        fullScaleY = Mathf.Max(0.1f, travelDistance) / bodyLength;
        manager = meteorManager;
        ApplyExpansion(0f);
        UpdateRendererColors(0f, 1f, 0f);
        return true;
    }

    void Update()
    {
        age += Time.deltaTime;
        float expansionProgress = flightDuration <= 0f ? 1f : Mathf.Clamp01(age / flightDuration);
        ApplyExpansion(expansionProgress);

        if (expansionProgress >= 1f)
        {
            isFadingOut = true;
            fadeTimer += Time.deltaTime;
        }

        float fadeIn = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(age / fadeInDuration);
        float tailToHeadFade = isFadingOut
            ? (fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(fadeTimer / fadeOutDuration))
            : 0f;
        float overallFadeOut = isFadingOut
            ? 1f - (fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(fadeTimer / fadeOutDuration))
            : 1f;
        UpdateRendererColors(fadeIn, overallFadeOut, tailToHeadFade);

        if (isFadingOut && fadeTimer >= fadeOutDuration)
        {
            Destroy(gameObject);
        }
    }

    void ApplyExpansion(float progress)
    {
        float currentScaleY = fullScaleY * progress;
        transform.localScale = new Vector3(scaleX, currentScaleY, scaleZ);
        transform.position = tailAnchor
            - transform.rotation * new Vector3(0f, tailLocalY * currentScaleY, 0f);
    }

    void UpdateRendererColors(float fadeIn, float overallFadeOut, float tailToHeadFade)
    {
        float fadeFront = tailToHeadFade * (1f + FadeFrontWidth);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            Color original = originalColors[i];
            float normalizedDistanceFromTail = Mathf.Clamp01((tailLocalY - rendererCenters[i]) / bodyLength);
            float tailFade = 1f - Mathf.SmoothStep(
                0f,
                FadeFrontWidth,
                fadeFront - normalizedDistanceFromTail);

            spriteRenderer.color = new Color(
                original.r,
                original.g,
                original.b,
                original.a * fadeIn * overallFadeOut * tailFade);
        }
    }

    float GetRendererCenterY(SpriteRenderer spriteRenderer)
    {
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(
            spriteRenderer.transform.TransformPoint(spriteBounds.center));
        return localCenter.y;
    }

    void OnDestroy()
    {
        if (manager != null)
        {
            manager.NotifyMeteorDestroyed();
        }
    }
}
