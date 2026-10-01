using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class StarTwinkle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float baseScale;
    private float twinkleSpeed;
    private float phase;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(float scale, float speed, float startPhase)
    {
        baseScale = scale;
        twinkleSpeed = speed;
        phase = startPhase;
        baseColor = spriteRenderer.color;
        transform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        float twinkle = (Mathf.Sin(Time.time * twinkleSpeed + phase) + 1f) * 0.5f;
        float brightness = Mathf.Lerp(0.3f, 1f, twinkle);
        float scaleMultiplier = Mathf.Lerp(0.8f, 1.2f, twinkle);

        Color color = baseColor;
        color.a = baseColor.a * brightness;
        spriteRenderer.color = color;
        transform.localScale = Vector3.one * baseScale * scaleMultiplier;
    }
}
