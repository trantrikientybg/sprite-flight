using UnityEngine;

public class MeteorAndStarManager : MonoBehaviour
{
    [Header("Stars")]
    [Min(0)] public int starCount = 80;
    [SerializeField] private Sprite starSprite;
    [Min(0f)] public float minStarScale = 0.04f;
    [Min(0f)] public float maxStarScale = 0.12f;
    [Min(0f)] public float minTwinkleSpeed = 0.7f;
    [Min(0f)] public float maxTwinkleSpeed = 1.8f;
    public Color starColor = new Color(0.72f, 0.86f, 1f, 1f);

    [Header("Meteors")]
    [SerializeField] private GameObject meteorPrefab;
    [Min(0)] public int maxMeteorCount = 2;
    [Min(0f)] public float minMeteorSpawnInterval = 2f;
    [Min(0f)] public float maxMeteorSpawnInterval = 5f;
    [Min(0f)] public float minMeteorSpeed = 4f;
    [Min(0f)] public float maxMeteorSpeed = 7f;
    [Min(0f)] public float minMeteorScale = 0.15f;
    [Min(0f)] public float maxMeteorScale = 0.3f;
    [Min(0.1f)] public float meteorTravelDistance = 7f;
    [Min(0.1f)] public float minMeteorLengthScale = 0.5f;
    [Min(0.1f)] public float maxMeteorLengthScale = 0.75f;
    [Min(0f)] public float meteorFadeInDuration = 0.25f;
    [Min(0f)] public float meteorFadeOutDuration = 0.9f;
    public int meteorSortingOrder = 1;

    private Camera mainCamera;
    private Sprite generatedStarSprite;
    private int activeMeteorCount;
    private float nextMeteorSpawnTime;
    private bool spawnErrorLogged;

    void Awake()
    {
        mainCamera = Camera.main;
        if (starSprite == null)
        {
            generatedStarSprite = CreateStarSprite();
            starSprite = generatedStarSprite;
        }
    }

    void Start()
    {
        SpawnStars();
        nextMeteorSpawnTime = Time.time;
    }

    void Update()
    {
        if (activeMeteorCount < Mathf.Max(0, maxMeteorCount)
            && Time.time >= nextMeteorSpawnTime)
        {
            if (TrySpawnMeteor())
            {
                float minInterval = Mathf.Max(0f, Mathf.Min(minMeteorSpawnInterval, maxMeteorSpawnInterval));
                float maxInterval = Mathf.Max(minInterval, Mathf.Max(minMeteorSpawnInterval, maxMeteorSpawnInterval));
                nextMeteorSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
            }
            else
            {
                nextMeteorSpawnTime = Time.time + 1f;
            }
        }
    }

    void SpawnStars()
    {
        if (starCount <= 0)
        {
            return;
        }

        if (starSprite == null)
        {
            LogSpawnError("Could not create a sprite for the stars.");
            return;
        }

        for (int i = 0; i < starCount; i++)
        {
            if (!TryGetRandomViewPosition(out Vector3 position))
            {
                return;
            }

            GameObject starObject = new GameObject("Star");
            starObject.transform.SetParent(transform, true);
            starObject.transform.position = position;

            SpriteRenderer spriteRenderer = starObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = starSprite;
            spriteRenderer.color = starColor;
            spriteRenderer.sortingOrder = -2;

            float minScale = Mathf.Max(0f, Mathf.Min(minStarScale, maxStarScale));
            float maxScale = Mathf.Max(minScale, Mathf.Max(minStarScale, maxStarScale));
            float minSpeed = Mathf.Max(0f, Mathf.Min(minTwinkleSpeed, maxTwinkleSpeed));
            float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(minTwinkleSpeed, maxTwinkleSpeed));

            starObject.AddComponent<StarTwinkle>().Initialize(
                Random.Range(minScale, maxScale),
                Random.Range(minSpeed, maxSpeed),
                Random.Range(0f, Mathf.PI * 2f));
        }
    }

    bool TrySpawnMeteor()
    {
        if (meteorPrefab == null)
        {
            LogSpawnError("Assign a meteor prefab to the MeteorAndStarManager.");
            return false;
        }

        if (!TryGetViewWorldPosition(Random.Range(0.35f, 1.05f), Random.Range(0.8f, 1.08f), out Vector3 spawnPosition))
        {
            return false;
        }

        float minSpeed = Mathf.Max(0f, Mathf.Min(minMeteorSpeed, maxMeteorSpeed));
        float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(minMeteorSpeed, maxMeteorSpeed));
        float minScale = Mathf.Max(0f, Mathf.Min(minMeteorScale, maxMeteorScale));
        float maxScale = Mathf.Max(minScale, Mathf.Max(minMeteorScale, maxMeteorScale));
        float minLengthScale = Mathf.Max(0.1f, Mathf.Min(minMeteorLengthScale, maxMeteorLengthScale));
        float maxLengthScale = Mathf.Max(minLengthScale, Mathf.Max(minMeteorLengthScale, maxMeteorLengthScale));

        GameObject meteorObject = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity, transform);
        meteorObject.name = "Meteor";
        Meteor meteor = meteorObject.GetComponent<Meteor>();
        if (meteor == null)
        {
            meteor = meteorObject.AddComponent<Meteor>();
        }

        float scale = Random.Range(minScale, maxScale);
        float referenceScale = Mathf.Abs(meteorPrefab.transform.localScale.x);
        float scaleMultiplier = referenceScale > Mathf.Epsilon ? scale / referenceScale : scale;
        bool initialized = meteor.Initialize(
            this,
            scaleMultiplier,
            Random.Range(minSpeed, maxSpeed),
            meteorTravelDistance * Random.Range(minLengthScale, maxLengthScale),
            meteorFadeInDuration,
            meteorFadeOutDuration,
            meteorSortingOrder,
            spawnPosition);
        if (!initialized)
        {
            Destroy(meteorObject);
            return false;
        }

        activeMeteorCount++;
        return true;
    }

    bool TryGetRandomViewPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            LogSpawnError("A MainCamera is required to create stars and meteors.");
            return false;
        }

        return TryGetViewWorldPosition(Random.value, Random.Range(0.5f, 1f), out position);
    }

    bool TryGetViewWorldPosition(float viewportX, float viewportY, out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            LogSpawnError("A MainCamera is required to create stars and meteors.");
            return false;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        Plane spawnPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!spawnPlane.Raycast(ray, out float distance))
        {
            LogSpawnError("The camera view does not intersect the 2D effects plane at z = 0.");
            return false;
        }

        position = ray.GetPoint(distance);
        return true;
    }

    Sprite CreateStarSprite()
    {
        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Generated Star",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float outerRadius = textureSize * 0.46f;
        float innerRadius = textureSize * 0.13f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float angle = Mathf.Atan2(offset.y, offset.x);
                float starRadius = Mathf.Lerp(
                    innerRadius,
                    outerRadius,
                    (Mathf.Cos(angle * 5f) + 1f) * 0.5f);
                float distance = offset.magnitude;
                float edge = 1f - Mathf.SmoothStep(starRadius - 1.5f, starRadius + 0.5f, distance);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - distance / (textureSize * 0.5f)), 3f) * 0.25f;
                float alpha = Mathf.Max(edge, glow);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }

    void LogSpawnError(string message)
    {
        if (spawnErrorLogged)
        {
            return;
        }

        Debug.LogError(message, this);
        spawnErrorLogged = true;
    }

    public void NotifyMeteorDestroyed()
    {
        activeMeteorCount = Mathf.Max(0, activeMeteorCount - 1);
    }

    void OnDestroy()
    {
        if (generatedStarSprite != null)
        {
            Destroy(generatedStarSprite);
            Destroy(generatedStarSprite.texture);
        }

    }
}
