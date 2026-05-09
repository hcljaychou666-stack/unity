using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SceneCheckpointMarker : MonoBehaviour
{
    [SerializeField] private string checkpointId = "level1_mid_relay";
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource activateAudio;

    [Header("Activation Effects")]
    [SerializeField] private bool enableActivationEffects = false;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem checkpointActivateParticles;

    private string SceneName => gameObject.scene.name;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (activateAudio == null)
        {
            activateAudio = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        RefreshVisual(PlayerLifeFlow.Instance.IsCheckpointActive(SceneName, checkpointId));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryActivate(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryActivate(collision);
    }

    private void RefreshVisual(bool activated)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (activated && activatedSprite != null)
        {
            spriteRenderer.sprite = activatedSprite;
            spriteRenderer.color = Color.white;
            return;
        }

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }

        spriteRenderer.color = new Color(1f, 1f, 1f, 0.92f);
    }

    private static bool IsPlayer(Collider2D collision)
    {
        return collision.CompareTag("Player")
            || collision.GetComponentInParent<PlayerHealth2D>() != null;
    }

    private void TryActivate(Collider2D collision)
    {
        if (!IsPlayer(collision))
        {
            return;
        }

        if (PlayerLifeFlow.Instance.IsCheckpointActive(SceneName, checkpointId))
        {
            return;
        }

        PlayerLifeFlow.Instance.SetCheckpoint(SceneName, checkpointId, transform.position + respawnOffset);
        RefreshVisual(true);
        PlayActivationEffect();

        if (activateAudio != null && !activateAudio.isPlaying)
        {
            activateAudio.Play();
        }
    }

    private void PlayActivationEffect()
    {
        if (!enableActivationEffects || checkpointActivateParticles == null)
        {
            return;
        }

        PixelEffectSpawner spawner = effectSpawner != null ? effectSpawner : GetComponent<PixelEffectSpawner>();
        if (spawner != null)
        {
            spawner.Play(checkpointActivateParticles, transform.position, Quaternion.identity);
        }
    }
}
