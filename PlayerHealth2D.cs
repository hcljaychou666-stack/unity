using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth2D : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHp = 3;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;

    [Header("Bounds")]
    [SerializeField] private float voidDeathY = -6f;

    [Header("Audio")]
    [SerializeField] private AudioSource deathSoundEffect;

    [Header("Feedback")]
    [SerializeField] private bool enableDamageShake = false;
    [SerializeField] private float damageShakeIntensity = 0.12f;
    [SerializeField] private float damageShakeDuration = 0.12f;
    [SerializeField] private float deathShakeIntensity = 0.22f;
    [SerializeField] private float deathShakeDuration = 0.18f;
    [SerializeField] private CameraFollow cameraShakeTarget;

    [Header("Effects")]
    [SerializeField] private bool enableHealthEffects = false;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem hitParticles;

    private int currentHp;
    private bool isDead;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerMovement movement;
    private PlayerDash dash;
    private bool restartQueued;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        dash = GetComponent<PlayerDash>();
        currentHp = maxHp;
    }

    private void Start()
    {
        AllControl.GameManager.Instance.BeginRunIfNeeded();
        PlayerLifeFlow.Instance.RegisterGameplayScene(SceneManager.GetActiveScene().name, startingLives);
    }

    private void Update()
    {
        if (!isDead && transform.position.y <= voidDeathY)
        {
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDead && !IsDashInvincible() && IsTrap(collision.collider))
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDead && !IsDashInvincible() && IsTrap(collision))
        {
            Die();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (IsDashInvincible()) return;

        currentHp -= amount;
        PlayHitEffects();

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
            return;
        }

        TriggerCameraShake(transform.position, damageShakeIntensity, damageShakeDuration);
    }

    private static bool IsTrap(Collider2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        Transform current = collision.transform;
        while (current != null)
        {
            if (current.CompareTag("Trap"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHp = 0;
        AllControl.GameManager.Instance.ResetScoreAfterDeath();
        TriggerCameraShake(transform.position, deathShakeIntensity, deathShakeDuration);

        if (deathSoundEffect != null)
        {
            deathSoundEffect.Play();
        }

        if (movement != null) movement.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        if (anim != null) anim.SetTrigger("death");

        if (!restartQueued)
        {
            restartQueued = true;
            Invoke(nameof(LoadLifeBroadcast), 0.8f);
        }
    }

    private void LoadLifeBroadcast()
    {
        if (!PlayerLifeFlow.Instance.TryBeginDeathTransition(SceneManager.GetActiveScene().name, startingLives))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private bool IsDashInvincible()
    {
        return dash != null && dash.IsInvincible;
    }

    private void TriggerCameraShake(Vector3 origin, float intensity, float duration)
    {
        if (!enableDamageShake || intensity <= 0f || duration <= 0f)
        {
            return;
        }

        CameraFollow shakeTarget = cameraShakeTarget != null ? cameraShakeTarget : FindObjectOfType<CameraFollow>();
        if (shakeTarget != null)
        {
            shakeTarget.TriggerShake(origin, intensity, duration);
        }
    }

    private void PlayHitEffects()
    {
        if (!enableHealthEffects || hitParticles == null)
        {
            return;
        }

        PixelEffectSpawner spawner = effectSpawner != null ? effectSpawner : GetComponent<PixelEffectSpawner>();
        if (spawner == null)
        {
            spawner = FindObjectOfType<PixelEffectSpawner>();
        }

        if (spawner != null)
        {
            spawner.Play(hitParticles, transform.position, Quaternion.identity);
        }
    }
}
