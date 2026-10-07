using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 60f;
    public float deceleration = 100f;
    public float rotationSpeed = 720f;

    [Header("Game Over Sound")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool gameOverTriggered;
    private bool controlsReversed;
    private bool ghostshipActive;
    private bool shieldActive;
    private bool speedBoostActive;
    private float reverseUntil;
    private float ghostshipUntil;
    private float shieldUntil;
    private float speedBoostUntil;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;
    private Collider2D[] playerColliders;
    private readonly HashSet<Collider2D> ignoredObstacleColliders = new HashSet<Collider2D>();

    public struct TimedEffectStatus
    {
        public string displayName;
        public float secondsRemaining;

        public TimedEffectStatus(string effectName, float remainingTime)
        {
            displayName = effectName;
            secondsRemaining = remainingTime;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerControl requires a Rigidbody2D component.", this);
            enabled = false;
            return;
        }

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        playerColliders = GetComponentsInChildren<Collider2D>(true);
        originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
        }
    }

    void Update()
    {
        UpdateTimedEffects();
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                         - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }

    void FixedUpdate()
    {
        if (gameOverTriggered)
        {
            return;
        }

        Vector2 effectiveInput = controlsReversed ? -moveInput : moveInput;
        Vector2 targetVelocity = effectiveInput * Mathf.Max(0f, moveSpeed) * (speedBoostActive ? 2f : 1f);
        float rate = effectiveInput.sqrMagnitude > 0f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            Mathf.Max(0f, rate) * Time.fixedDeltaTime);

        if (effectiveInput.sqrMagnitude > 0f)
        {
            float targetRotation = Mathf.Atan2(effectiveInput.y, effectiveInput.x) * Mathf.Rad2Deg - 90f;
            float rotation = Mathf.MoveTowardsAngle(
                rb.rotation,
                targetRotation,
                Mathf.Max(0f, rotationSpeed) * Time.fixedDeltaTime);
            rb.MoveRotation(rotation);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    void HandleCollision(Collision2D collision)
    {
        bool hitObstacle = collision.gameObject.CompareTag("Obstacle");
        bool hitBorder = collision.gameObject.CompareTag("Border");
        if (!gameOverTriggered
            && ((hitObstacle && !ghostshipActive && !shieldActive)
                || (hitBorder && !shieldActive)))
        {
            HandleDeath();
        }
    }

    public void ActivateControlReverse(float duration)
    {
        controlsReversed = true;
        reverseUntil = Time.time + Mathf.Max(0f, duration);
    }

    public void ActivateGhostship(float duration)
    {
        ghostshipActive = true;
        ghostshipUntil = Time.time + Mathf.Max(0f, duration);
        SetPlayerOpacity(0.4f);
        Obstacles[] obstacles = FindObjectsByType<Obstacles>();
        for (int i = 0; i < obstacles.Length; i++)
        {
            IgnoreObstacleDuringGhostship(obstacles[i]);
        }
    }

    public void IgnoreObstacleDuringGhostship(Obstacles obstacle)
    {
        if (!ghostshipActive || obstacle == null)
        {
            return;
        }

        Collider2D[] obstacleColliders = obstacle.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < obstacleColliders.Length; i++)
        {
            Collider2D obstacleCollider = obstacleColliders[i];
            if (obstacleCollider == null || !ignoredObstacleColliders.Add(obstacleCollider))
            {
                continue;
            }

            for (int j = 0; j < playerColliders.Length; j++)
            {
                if (playerColliders[j] != null)
                {
                    Physics2D.IgnoreCollision(playerColliders[j], obstacleCollider, true);
                }
            }
        }
    }

    public void ActivateShield(float duration)
    {
        shieldActive = true;
        shieldUntil = Time.time + Mathf.Max(0f, duration);
    }

    public void ActivateSpeedBoost(float duration)
    {
        speedBoostActive = true;
        speedBoostUntil = Time.time + Mathf.Max(0f, duration);
    }

    public void GetActiveTimedEffects(List<TimedEffectStatus> effects)
    {
        effects.Clear();
        if (controlsReversed)
        {
            effects.Add(new TimedEffectStatus("Controls reversed", reverseUntil - Time.time));
        }

        if (ghostshipActive)
        {
            effects.Add(new TimedEffectStatus("Ghostship: pass through obstacles", ghostshipUntil - Time.time));
        }

        if (shieldActive)
        {
            effects.Add(new TimedEffectStatus("Shield: invulnerable", shieldUntil - Time.time));
        }

        if (speedBoostActive)
        {
            effects.Add(new TimedEffectStatus("Speed boost: 2x movement", speedBoostUntil - Time.time));
        }
    }

    void UpdateTimedEffects()
    {
        if (controlsReversed && Time.time >= reverseUntil)
        {
            controlsReversed = false;
        }

        if (ghostshipActive && Time.time >= ghostshipUntil)
        {
            ghostshipActive = false;
            RestoreGhostshipCollisions();
            RestorePlayerOpacity();
        }

        if (shieldActive && Time.time >= shieldUntil)
        {
            shieldActive = false;
        }

        if (speedBoostActive && Time.time >= speedBoostUntil)
        {
            speedBoostActive = false;
        }
    }

    void SetPlayerOpacity(float opacity)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color color = originalSpriteColors[i];
            color.a *= opacity;
            spriteRenderers[i].color = color;
        }
    }

    void RestorePlayerOpacity()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = originalSpriteColors[i];
            }
        }
    }

    void RestoreGhostshipCollisions()
    {
        foreach (Collider2D obstacleCollider in ignoredObstacleColliders)
        {
            if (obstacleCollider == null)
            {
                continue;
            }

            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] != null)
                {
                    Physics2D.IgnoreCollision(playerColliders[i], obstacleCollider, false);
                }
            }
        }

        ignoredObstacleColliders.Clear();
    }

    private void HandleDeath()
    {
        gameOverTriggered = true;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.GameOver();
        }
        else
        {
            Debug.LogError("Player died, but no active ScoreManager was found to show the GameOver panel.", this);
            MusicController musicController = FindAnyObjectByType<MusicController>();
            if (musicController != null)
            {
                musicController.StopMusic();
            }
        }

        Debug.Log("Game Over", this);
        Destroy(gameObject);
    }
}
