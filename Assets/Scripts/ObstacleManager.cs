using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Spawning")]
    public GameObject obstaclePrefab;
    public int targetObstacleCount = 5;
    public Transform player;
    public float minDistanceFromPlayer = 5f;
    [Min(1)] public int spawnPositionAttempts = 40;
    [Min(0f)] public float spawnRetryDelay = 1f;

    [Header("Obstacle Lifetime")]
    [Min(0f)] public float minObstacleLifetime = 12f;
    [Min(0f)] public float maxObstacleLifetime = 20f;
    [Min(0f)] public float despawnIfSpeedBelow = 0.25f;

    [Header("Spawn and Despawn Effects")]
    [Min(0f)] public float spawnGrowDuration = 0.35f;
    [Min(0f)] public float fadeOutDuration = 0.75f;

    private int obstacleCount;

    private Camera mainCamera;
    private float nextSpawnAttemptTime;
    private bool spawnErrorLogged;

    void Awake()
    {
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
        if (obstacleCount < Mathf.Max(0, targetObstacleCount)
            && Time.time >= nextSpawnAttemptTime)
        {
            FillObstacleCount();
        }
    }

    void FillObstacleCount()
    {
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

        GameObject obstacleObject = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
        if (!obstacleObject.TryGetComponent(out Obstacles obstacle))
        {
            Destroy(obstacleObject);
            LogSpawnError("The obstacle prefab must have an Obstacles component.");
            return false;
        }

        float minLifetime = Mathf.Max(0f, Mathf.Min(minObstacleLifetime, maxObstacleLifetime));
        float maxLifetime = Mathf.Max(minLifetime, Mathf.Max(minObstacleLifetime, maxObstacleLifetime));
        obstacle.Initialize(
            this,
            Random.Range(minLifetime, maxLifetime),
            despawnIfSpeedBelow,
            spawnGrowDuration,
            fadeOutDuration);
        obstacleCount++;
        return true;
    }

    bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        Transform playerTransform = player;
        if (playerTransform == null)
        {
            PlayerControl playerControl = FindAnyObjectByType<PlayerControl>();
            if (playerControl != null)
            {
                playerTransform = playerControl.transform;
                player = playerTransform;
            }
        }

        if (playerTransform == null)
        {
            LogSpawnError("Assign the Player Transform or make sure a PlayerControl exists before spawning obstacles.");
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
}
