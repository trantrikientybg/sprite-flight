using UnityEngine;

public class ParticlesController : MonoBehaviour
{
    [Header("Particle Prefab")]
    public GameObject particlePrefab;
    [Header("Max Particle")]
    public int maxParticle = 25;
    [Header("Min Max Scale")]
    public float minParticleScale = 0.05f;
    public float maxParticleScale = 0.15f;

    [Header("Min Max Speed")]
    public float minParticleSpeed = 0.5f;
    public float maxParticleSpeed = 2f;

    [Header("Lifetime")]
    public float minParticleLifetime = 5f;
    public float maxParticleLifetime = 7f;

    [Header("Scale Pulse")]
    public float pulseScaleRange = 0.05f;
    public float pulseScaleStep = 0.01f;

    public int particleCount = 0;
    private bool spawnConfigurationErrorLogged;
    private Camera mainCamera;

    void Awake()
    {
        particleCount = 0;
        mainCamera = Camera.main;
    }

    void Start()
    {
        for (int i = 0; i < maxParticle; i++)
        {
            ParticleSpawn();
        }
    }

    void Update()
    {
        if (particleCount < maxParticle)
        {
            ParticleSpawn();
        }
    }

    void ParticleSpawn()
    {
        if (particlePrefab == null)
        {
            LogSpawnConfigurationError("Particle Prefab is not assigned.");
            return;
        }

        if (!TryGetRandomCameraPosition(out Vector3 spawnPosition))
        {
            return;
        }

        GameObject newParticle = Instantiate(particlePrefab, spawnPosition, Quaternion.identity, transform);
        if (!newParticle.TryGetComponent(out Particles particle))
        {
            Destroy(newParticle);
            LogSpawnConfigurationError("Particle Prefab must have a Particles component.");
            return;
        }

        particle.Initialize(this);
        particleCount++;
    }

    public void NotifyParticleExpired()
    {
        particleCount = Mathf.Max(0, particleCount - 1);
    }

    void LogSpawnConfigurationError(string message)
    {
        if (spawnConfigurationErrorLogged)
        {
            return;
        }

        Debug.LogError(message, this);
        spawnConfigurationErrorLogged = true;
    }

    bool TryGetRandomCameraPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCamera == null)
        {
            LogSpawnConfigurationError("A MainCamera is required to spawn particles in the camera view.");
            return false;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(Random.value, Random.value, 0f));
        Plane particlePlane = new Plane(Vector3.forward, Vector3.zero);
        if (!particlePlane.Raycast(ray, out float distance))
        {
            LogSpawnConfigurationError("The camera view does not intersect the particle plane at z = 0.");
            return false;
        }

        position = ray.GetPoint(distance);
        return true;
    }
}
