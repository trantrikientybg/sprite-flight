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
    }

    void Update()
    {
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

        Vector2 targetVelocity = moveInput * Mathf.Max(0f, moveSpeed);
        float rate = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            Mathf.Max(0f, rate) * Time.fixedDeltaTime);

        if (moveInput.sqrMagnitude > 0f)
        {
            float targetRotation = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            float rotation = Mathf.MoveTowardsAngle(
                rb.rotation,
                targetRotation,
                Mathf.Max(0f, rotationSpeed) * Time.fixedDeltaTime);
            rb.MoveRotation(rotation);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameOverTriggered && (collision.gameObject.CompareTag("Obstacle")
                                || collision.gameObject.CompareTag("Border")))
        {
            HandleDeath();
        }
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
