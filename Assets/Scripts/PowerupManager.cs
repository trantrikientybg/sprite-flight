using System.Collections.Generic;
using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    [Header("Power-up Prefabs")]
    [Tooltip("Use prefab root names: Blackhole, ControlReverse, Ghostship, MeteorGrow, MeteorShrink, Speed-up, PlusMeteor, Shield, SizeDown, SizeUp.")]
    public GameObject[] powerupPrefabs = new GameObject[0];

    [Header("Random Spawning")]
    [Min(0.1f)] public float initialSpawnInterval = 5f;
    [Min(0.1f)] public float minimumSpawnInterval = 3f;
    [Min(0.1f)] public float timeToMinimumSpawnInterval = 300f;
    [Range(0f, 1f)] public float spawnChance = 0.8f;
    [Min(0f)] public float minLifetime = 7f;
    [Min(0f)] public float maxLifetime = 10f;
    [Min(0f)] public float fadeOutDuration = 2f;
    [Min(0f)] public float spawnOutsideViewport = 0.08f;
    [Min(0f)] public float flySpeed = 3f;
    [Min(1f)] public float negativeEffectSpeedMultiplier = 1.5f;

    [Header("Timed Effects")]
    [Min(0f)] public float timedEffectDuration = 5f;

    private readonly List<GameObject> validPowerupPrefabs = new List<GameObject>();
    private Camera mainCamera;
    private PlayerControl player;
    private ObstacleManager obstacleManager;
    private float nextSpawnAttemptTime;
    private float elapsedPlayTime;
    private bool setupErrorLogged;

    void Start()
    {
        mainCamera = Camera.main;
        player = FindAnyObjectByType<PlayerControl>();
        obstacleManager = FindAnyObjectByType<ObstacleManager>();
        ValidatePowerupPrefabs();
        nextSpawnAttemptTime = Time.time + GetCurrentSpawnInterval();
    }

    void Update()
    {
        elapsedPlayTime += Time.deltaTime;
        if (Time.time < nextSpawnAttemptTime)
        {
            return;
        }

        nextSpawnAttemptTime = Time.time + GetCurrentSpawnInterval();
        if (validPowerupPrefabs.Count == 0 || Random.value >= spawnChance)
        {
            return;
        }

        TrySpawnPowerup();
    }

    void ValidatePowerupPrefabs()
    {
        validPowerupPrefabs.Clear();
        if (powerupPrefabs == null || powerupPrefabs.Length == 0)
        {
            LogSetupError("Assign the power-up prefabs to PowerupManager.powerupPrefabs.");
            return;
        }

        bool hasEmptySlot = false;
        for (int i = 0; i < powerupPrefabs.Length; i++)
        {
            GameObject prefab = powerupPrefabs[i];
            if (prefab == null)
            {
                hasEmptySlot = true;
                continue;
            }

            if (!TryParseEffect(prefab.name, out _))
            {
                Debug.LogError(
                    "Unsupported power-up prefab name '" + prefab.name + "'. Use one of the documented power-up names.",
                    prefab);
                continue;
            }

            validPowerupPrefabs.Add(prefab);
        }

        if (hasEmptySlot)
        {
            LogSetupError("Assign a prefab to each configured PowerupManager.powerupPrefabs slot.");
        }
    }

    void TrySpawnPowerup()
    {
        player = GetPlayer();
        if (player == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            LogSetupError("A MainCamera is required to spawn power-ups.");
            return;
        }

        GameObject prefab = validPowerupPrefabs[Random.Range(0, validPowerupPrefabs.Count)];
        bool hasNegativeEffect = TryParseEffect(prefab.name, out PowerupEffect effect)
            && IsNegativeEffect(effect);
        if (!TryGetSpawnAndTarget(hasNegativeEffect, out Vector3 spawnPosition, out Vector3 targetPosition))
        {
            LogSetupError("Could not find a position on the MainCamera view plane for a power-up.");
            return;
        }

        GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        PowerupPickup pickup = instance.GetComponent<PowerupPickup>();
        if (pickup == null)
        {
            pickup = instance.AddComponent<PowerupPickup>();
        }

        float lifetimeMin = Mathf.Max(0f, Mathf.Min(minLifetime, maxLifetime));
        float lifetimeMax = Mathf.Max(lifetimeMin, Mathf.Max(minLifetime, maxLifetime));
        float lifetime = Random.Range(lifetimeMin, lifetimeMax);
        if (!pickup.Initialize(
                this,
                lifetime,
                Mathf.Min(Mathf.Max(0f, fadeOutDuration), lifetime),
                targetPosition,
                hasNegativeEffect ? flySpeed * Mathf.Max(1f, negativeEffectSpeedMultiplier) : flySpeed,
                hasNegativeEffect ? player.transform : null))
        {
            Destroy(instance);
        }
    }

    bool TryGetSpawnAndTarget(bool targetPlayer, out Vector3 spawnPosition, out Vector3 targetPosition)
    {
        spawnPosition = Vector3.zero;
        targetPosition = Vector3.zero;

        float planeZ = player != null ? player.transform.position.z : 0f;
        float offset = Mathf.Max(0f, spawnOutsideViewport);
        int side = Random.Range(0, 4);
        Vector2 spawnViewport = new Vector2(Random.value, Random.value);
        if (side == 0)
        {
            spawnViewport.x = -offset;
        }
        else if (side == 1)
        {
            spawnViewport.x = 1f + offset;
        }
        else if (side == 2)
        {
            spawnViewport.y = -offset;
        }
        else
        {
            spawnViewport.y = 1f + offset;
        }

        if (!TryViewportToPlane(spawnViewport, planeZ, out spawnPosition))
        {
            return false;
        }

        if (targetPlayer)
        {
            targetPosition = player.transform.position;
            return true;
        }

        Vector2 targetViewport = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
        return TryViewportToPlane(targetViewport, planeZ, out targetPosition);
    }

    float GetCurrentSpawnInterval()
    {
        float initialInterval = Mathf.Max(0.1f, initialSpawnInterval);
        float minimumInterval = Mathf.Clamp(minimumSpawnInterval, 0.1f, initialInterval);
        float timeToMinimum = Mathf.Max(0.1f, timeToMinimumSpawnInterval);
        float progress = Mathf.Clamp01(elapsedPlayTime / timeToMinimum);
        return Mathf.Lerp(initialInterval, minimumInterval, progress);
    }

    PlayerControl GetPlayer()
    {
        if (player != null)
        {
            return player;
        }

        player = FindAnyObjectByType<PlayerControl>();
        return player;
    }

    static bool IsNegativeEffect(PowerupEffect effect)
    {
        return effect == PowerupEffect.ControlReverse
            || effect == PowerupEffect.MeteorGrow
            || effect == PowerupEffect.PlusMeteor
            || effect == PowerupEffect.SizeUp;
    }

    bool TryViewportToPlane(Vector2 viewportPosition, float planeZ, out Vector3 worldPosition)
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(viewportPosition.x, viewportPosition.y, 0f));
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        if (plane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }

    public void Collect(PowerupPickup pickup, PlayerControl collectingPlayer)
    {
        if (pickup == null || collectingPlayer == null)
        {
            return;
        }

        string prefabName = pickup.gameObject.name.Replace("(Clone)", "").Trim();
        if (!TryParseEffect(prefabName, out PowerupEffect effect))
        {
            Debug.LogError("Cannot apply unknown power-up '" + prefabName + "'.", pickup);
            return;
        }

        switch (effect)
        {
            case PowerupEffect.Blackhole:
                ApplyBlackhole();
                break;
            case PowerupEffect.ControlReverse:
                ApplyControlReverse(collectingPlayer);
                break;
            case PowerupEffect.Ghostship:
                ApplyGhostship(collectingPlayer);
                break;
            case PowerupEffect.MeteorGrow:
                ApplyMeteorGrow();
                break;
            case PowerupEffect.MeteorShrink:
                ApplyMeteorShrink();
                break;
            case PowerupEffect.SpeedUp:
                ApplySpeedUp(collectingPlayer);
                break;
            case PowerupEffect.PlusMeteor:
                ApplyPlusMeteor();
                break;
            case PowerupEffect.Shield:
                ApplyShield(collectingPlayer);
                break;
            case PowerupEffect.SizeDown:
                ApplySizeDown(collectingPlayer);
                break;
            case PowerupEffect.SizeUp:
                ApplySizeUp(collectingPlayer);
                break;
        }

        Destroy(pickup.gameObject);
    }

    void ApplyBlackhole()
    {
        if (GetObstacleManager() is ObstacleManager manager)
        {
            manager.ResetObstacleField();
        }
    }

    void ApplyControlReverse(PlayerControl collectingPlayer)
    {
        collectingPlayer.ActivateControlReverse(timedEffectDuration);
    }

    void ApplyGhostship(PlayerControl collectingPlayer)
    {
        collectingPlayer.ActivateGhostship(timedEffectDuration);
    }

    void ApplyMeteorGrow()
    {
        if (GetObstacleManager() is ObstacleManager manager)
        {
            manager.ScaleAllObstacles(1.2f);
        }
    }

    void ApplyMeteorShrink()
    {
        if (GetObstacleManager() is ObstacleManager manager)
        {
            manager.ScaleAllObstacles(1f / 1.2f);
        }
    }

    void ApplySpeedUp(PlayerControl collectingPlayer)
    {
        collectingPlayer.ActivateSpeedBoost(timedEffectDuration);
    }

    void ApplyPlusMeteor()
    {
        if (GetObstacleManager() is ObstacleManager manager)
        {
            manager.IncreaseTargetObstacleCount();
        }
    }

    void ApplyShield(PlayerControl collectingPlayer)
    {
        collectingPlayer.ActivateShield(timedEffectDuration);
    }

    void ApplySizeDown(PlayerControl collectingPlayer)
    {
        collectingPlayer.transform.localScale /= 1.2f;
    }

    void ApplySizeUp(PlayerControl collectingPlayer)
    {
        collectingPlayer.transform.localScale *= 1.2f;
    }

    ObstacleManager GetObstacleManager()
    {
        if (obstacleManager == null)
        {
            obstacleManager = FindAnyObjectByType<ObstacleManager>();
        }

        if (obstacleManager == null)
        {
            Debug.LogError("This power-up requires an active ObstacleManager.", this);
        }

        return obstacleManager;
    }

    void LogSetupError(string message)
    {
        if (setupErrorLogged)
        {
            return;
        }

        Debug.LogError(message, this);
        setupErrorLogged = true;
    }

    static bool TryParseEffect(string prefabName, out PowerupEffect effect)
    {
        switch (prefabName.Trim().ToLowerInvariant())
        {
            case "blackhole":
                effect = PowerupEffect.Blackhole;
                return true;
            case "controlreverse":
                effect = PowerupEffect.ControlReverse;
                return true;
            case "ghostship":
                effect = PowerupEffect.Ghostship;
                return true;
            case "meteorgrow":
                effect = PowerupEffect.MeteorGrow;
                return true;
            case "meteorshrink":
                effect = PowerupEffect.MeteorShrink;
                return true;
            case "speed-up":
                effect = PowerupEffect.SpeedUp;
                return true;
            case "plusmeteor":
                effect = PowerupEffect.PlusMeteor;
                return true;
            case "shield":
                effect = PowerupEffect.Shield;
                return true;
            case "sizedown":
                effect = PowerupEffect.SizeDown;
                return true;
            case "sizeup":
                effect = PowerupEffect.SizeUp;
                return true;
            default:
                effect = default;
                return false;
        }
    }

    enum PowerupEffect
    {
        Blackhole,
        ControlReverse,
        Ghostship,
        MeteorGrow,
        MeteorShrink,
        SpeedUp,
        PlusMeteor,
        Shield,
        SizeDown,
        SizeUp
    }
}
