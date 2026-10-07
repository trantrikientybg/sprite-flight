using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Spawning")]
    public GameObject obstaclePrefab;
    [Tooltip("Drag a folder of Sprite-imported images here. Sprites in subfolders are included.")]
    [SerializeField] private Object obstacleSpriteFolder;
    [SerializeField] private Sprite[] obstacleSprites = new Sprite[0];
    public int targetObstacleCount = 5;
    public Transform player;
    public float minDistanceFromPlayer = 5f;
    [Min(1)] public int spawnPositionAttempts = 40;
    [Min(0f)] public float spawnRetryDelay = 1f;

    [Header("Obstacle Lifetime")]
    [Min(0f)] public float minObstacleLifetime = 12f;
    [Min(0f)] public float maxObstacleLifetime = 20f;
    [Min(0f)] public float despawnIfSpeedBelow = 0.25f;

    [Header("Accumulated Obstacle Speed")]
    [Min(0f)] public float initialObstacleSpeed = 3.5f;
    [Min(0f)] public float speedIncreasePerSecond = 0.025f;
    [Min(0f)] public float maximumObstacleSpeed = 6f;

    [Header("Spawn and Despawn Effects")]
    [Min(0f)] public float spawnGrowDuration = 0.35f;
    [Min(0f)] public float fadeOutDuration = 0.75f;

    private readonly List<Obstacles> activeObstacles = new List<Obstacles>();
    private int obstacleCount;
    private int startingObstacleCount;

    private Camera mainCamera;
    private float nextSpawnAttemptTime;
    private float accumulatedPlayTime;
    private float currentObstacleSpeed;
    private bool spawnErrorLogged;

    public float CurrentObstacleSpeed => currentObstacleSpeed;

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshObstacleSprites();
    }

    [ContextMenu("Refresh Obstacle Sprites From Folder")]
    void RefreshObstacleSprites()
    {
        obstacleSprites = new Sprite[0];
        if (obstacleSpriteFolder == null)
        {
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(obstacleSpriteFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Obstacle Sprite Folder must reference a folder inside the Unity project.", this);
            return;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        HashSet<Sprite> discoveredSprites = new HashSet<Sprite>();
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int j = 0; j < assets.Length; j++)
            {
                if (assets[j] is Sprite sprite)
                {
                    discoveredSprites.Add(sprite);
                }
            }
        }

        obstacleSprites = new Sprite[discoveredSprites.Count];
        discoveredSprites.CopyTo(obstacleSprites);
        System.Array.Sort(obstacleSprites, (a, b) => string.CompareOrdinal(a.name, b.name));

        if (obstacleSprites.Length == 0)
        {
            Debug.LogError("No Sprite assets were found in the Obstacle Sprite Folder. Set the images' Texture Type to Sprite (2D and UI).", this);
        }
    }
#endif

    void Awake()
    {
        startingObstacleCount = Mathf.Max(0, targetObstacleCount);
        currentObstacleSpeed = Mathf.Max(0f, initialObstacleSpeed);
        mainCamera = Camera.main;
        if (player == null)
        {
            PlayerControl playerControl = FindAnyObjectByType<PlayerControl>();
            if (playerControl != null)
            {
                player = playerControl.transform;
            }
        }
    }

    void Start()
    {
        FillObstacleCount();
    }

    void Update()
    {
        accumulatedPlayTime += Time.deltaTime;
        float nextObstacleSpeed = Mathf.Min(
            Mathf.Max(0f, initialObstacleSpeed)
                + Mathf.Max(0f, speedIncreasePerSecond) * accumulatedPlayTime,
            Mathf.Max(initialObstacleSpeed, maximumObstacleSpeed));
        if (!Mathf.Approximately(nextObstacleSpeed, currentObstacleSpeed))
        {
            currentObstacleSpeed = nextObstacleSpeed;
            ApplyCurrentSpeedToObstacles();
        }

        if (obstacleCount < Mathf.Max(0, targetObstacleCount)
            && Time.time >= nextSpawnAttemptTime)
        {
            FillObstacleCount();
        }
    }

    void FillObstacleCount()
    {
        if (!TryGetPlayerTransform())
        {
            nextSpawnAttemptTime = Time.time + Mathf.Max(0.1f, spawnRetryDelay);
            return;
        }

        int targetCount = Mathf.Max(0, targetObstacleCount);
        while (obstacleCount < targetCount)
        {
            if (!TrySpawnObstacle())
            {
                nextSpawnAttemptTime = Time.time + Mathf.Max(0.1f, spawnRetryDelay);
                return;
            }
        }
    }

    bool TrySpawnObstacle()
    {
        if (obstaclePrefab == null)
        {
            LogSpawnError("Assign an obstacle prefab to the ObstacleManager.");
            return false;
        }

        if (obstacleSprites == null || obstacleSprites.Length == 0)
        {
            LogSpawnError("Assign a folder containing Sprite assets to the ObstacleManager's Obstacle Sprite Folder.");
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            LogSpawnError("A MainCamera is required to spawn obstacles in the camera view.");
            return false;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            return false;
        }

        Sprite sprite = obstacleSprites[Random.Range(0, obstacleSprites.Length)];
        GameObject obstacleObject = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
        if (!obstacleObject.TryGetComponent(out Obstacles obstacle))
        {
            Destroy(obstacleObject);
            LogSpawnError("The obstacle prefab must have an Obstacles component.");
            return false;
        }

        if (!obstacle.SetSprite(sprite))
        {
            Destroy(obstacleObject);
            LogSpawnError("Could not build the obstacle PolygonCollider2D from the selected sprite's Physics Shape.");
            return false;
        }

        float minLifetime = Mathf.Max(0f, Mathf.Min(minObstacleLifetime, maxObstacleLifetime));
        float maxLifetime = Mathf.Max(minLifetime, Mathf.Max(minObstacleLifetime, maxObstacleLifetime));
        obstacle.Initialize(
            this,
            Random.Range(minLifetime, maxLifetime),
            despawnIfSpeedBelow,
            spawnGrowDuration,
            fadeOutDuration,
            currentObstacleSpeed);
        activeObstacles.Add(obstacle);
        obstacleCount++;
        if (player != null && player.TryGetComponent(out PlayerControl playerControl))
        {
            playerControl.IgnoreObstacleDuringGhostship(obstacle);
        }

        return true;
    }

    bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        Transform playerTransform = TryGetPlayerTransform();
        if (playerTransform == null)
        {
            return false;
        }

        Plane spawnPlane = new Plane(Vector3.forward, Vector3.zero);
        int attempts = Mathf.Max(1, spawnPositionAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(Random.value, Random.value, 0f));
            if (!spawnPlane.Raycast(ray, out float distance))
            {
                continue;
            }

            Vector3 candidate = ray.GetPoint(distance);
            if (Vector2.Distance(candidate, playerTransform.position) < Mathf.Max(0f, minDistanceFromPlayer))
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        LogSpawnError("Could not find an obstacle spawn position in the camera view outside the player's minimum distance.");
        return false;
    }

    Transform TryGetPlayerTransform()
    {
        if (player != null && player.GetComponentInParent<PlayerControl>() != null)
        {
            return player;
        }

        PlayerControl playerControl = FindAnyObjectByType<PlayerControl>();
        if (playerControl != null)
        {
            player = playerControl.transform;
            return player;
        }

        player = null;
        return null;
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

    public void NotifyObstacleDestroyed()
    {
        obstacleCount = Mathf.Max(0, obstacleCount - 1);
    }

    public void NotifyObstacleDestroyed(Obstacles obstacle)
    {
        activeObstacles.Remove(obstacle);
        NotifyObstacleDestroyed();
    }

    public void ScaleAllObstacles(float scaleMultiplier)
    {
        Obstacles[] obstacles = FindObjectsByType<Obstacles>();
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].ApplyScaleMultiplier(scaleMultiplier);
        }
    }

    public void IncreaseTargetObstacleCount()
    {
        targetObstacleCount = Mathf.Max(0, targetObstacleCount) + 1;
        FillObstacleCount();
    }

    public void ResetObstacleField()
    {
        Obstacles[] obstacles = FindObjectsByType<Obstacles>();
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].DisassociateFromManager();
            Destroy(obstacles[i].gameObject);
        }

        activeObstacles.Clear();
        obstacleCount = 0;
        targetObstacleCount = startingObstacleCount;
        accumulatedPlayTime = 0f;
        currentObstacleSpeed = Mathf.Max(0f, initialObstacleSpeed);
        nextSpawnAttemptTime = 0f;
        FillObstacleCount();
    }

    void ApplyCurrentSpeedToObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            activeObstacles[i].SetMovementSpeed(currentObstacleSpeed);
        }
    }
}
