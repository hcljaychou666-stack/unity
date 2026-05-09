using UnityEngine;

[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    private static Sprite fallbackSprite;

    private Rigidbody2D rb;
    private Collider2D hitbox;
    private GameObject ownerRoot;
    private float direction = 1f;
    private float speed = 14f;
    private int damage = 1;
    private bool isInitialized;

    [Header("Hit Effects")]
    [SerializeField] private bool enableHitEffects = false;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem hitParticles;

    private void Awake()
    {
        EnsureComponents();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || rb == null)
        {
            return;
        }

        float travelDistance = speed * Time.fixedDeltaTime;
        Vector2 startPosition = rb.position;

        if (TryCastHit(startPosition, travelDistance))
        {
            return;
        }

        rb.MovePosition(startPosition + Vector2.right * direction * travelDistance);
    }

    public void Initialize(float travelDirection, float travelSpeed, float lifetime, GameObject owner, int projectileDamage = 1)
    {
        EnsureComponents();

        direction = Mathf.Approximately(travelDirection, 0f) ? 1f : Mathf.Sign(travelDirection);
        speed = Mathf.Max(0.1f, travelSpeed);
        damage = Mathf.Max(0, projectileDamage);
        ownerRoot = owner != null ? owner.transform.root.gameObject : null;

        ConfigurePhysics();
        IgnoreOwnerCollisions();

        isInitialized = true;

        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (!isInitialized || other == null)
        {
            return;
        }

        if (ownerRoot != null && other.transform.root.gameObject == ownerRoot)
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable = other.GetComponentInParent<IDamageable>();
        }

            if (damageable != null)
            {
                if (damage > 0)
                {
                    damageable.TakeDamage(damage);
                }

                PlayHitEffect(transform.position);
                Destroy(gameObject);
                return;
            }

        if (other.isTrigger)
        {
            return;
        }

        PlayHitEffect(transform.position);
        Destroy(gameObject);
    }

    private void EnsureComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        if (hitbox == null)
        {
            hitbox = GetComponent<Collider2D>();

            if (hitbox == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                hitbox = boxCollider;
            }
        }

        ConfigurePhysics();
    }

    private void ConfigurePhysics()
    {
        if (rb == null)
        {
            return;
        }

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.zero;
    }

    private void IgnoreOwnerCollisions()
    {
        if (ownerRoot == null || hitbox == null)
        {
            return;
        }

        Collider2D[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(hitbox, ownerCollider, true);
            }
        }
    }

    public static GameObject CreateVisualProjectile(Sprite templateSprite = null, Color? tint = null)
    {
        GameObject projectileObject = new GameObject("PlayerProjectile_Runtime");
        SpriteRenderer spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = templateSprite != null ? templateSprite : GetFallbackSprite();
        spriteRenderer.color = tint ?? new Color(1f, 0.9f, 0.45f, 1f);

        projectileObject.AddComponent<Rigidbody2D>();

        BoxCollider2D boxCollider = projectileObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.28f, 0.14f);

        projectileObject.AddComponent<Projectile>();
        projectileObject.transform.localScale = templateSprite != null ? new Vector3(2.9f, 2.9f, 1f) : new Vector3(1.65f, 0.42f, 1f);
        return projectileObject;
    }

    private bool TryCastHit(Vector2 startPosition, float distance)
    {
        Vector2 castSize = hitbox != null ? hitbox.bounds.size : new Vector2(0.28f, 0.14f);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(startPosition, castSize, 0f, Vector2.right * direction, distance);

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D targetCollider = hit.collider;

            if (targetCollider == null)
            {
                continue;
            }

            if (ownerRoot != null && targetCollider.transform.root.gameObject == ownerRoot)
            {
                continue;
            }

            IDamageable damageable = targetCollider.GetComponent<IDamageable>();

            if (damageable == null)
            {
                damageable = targetCollider.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                if (damage > 0)
                {
                    damageable.TakeDamage(damage);
                }

                transform.position = hit.point != Vector2.zero ? (Vector3)hit.point : targetCollider.bounds.center;
                PlayHitEffect(transform.position);
                Destroy(gameObject);
                return true;
            }

            if (!targetCollider.isTrigger)
            {
                transform.position = hit.point != Vector2.zero ? (Vector3)hit.point : startPosition;
                PlayHitEffect(transform.position);
                Destroy(gameObject);
                return true;
            }
        }

        return false;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        Texture2D texture = new Texture2D(32, 12, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color border = Color.white;
        Color core = new Color(1f, 0.95f, 0.55f, 1f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int x = 2; x < 24; x++)
        {
            for (int y = 3; y < 9; y++)
            {
                bool isBorder = x == 2 || x == 23 || y == 3 || y == 8;
                texture.SetPixel(x, y, isBorder ? border : core);
            }
        }

        for (int x = 24; x < 31; x++)
        {
            int tipInset = x - 24;
            int minY = 3 + tipInset / 2;
            int maxY = 8 - tipInset / 2;

            for (int y = minY; y <= maxY; y++)
            {
                bool isBorder = x == 30 || y == minY || y == maxY;
                texture.SetPixel(x, y, isBorder ? border : core);
            }
        }

        texture.Apply();

        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return fallbackSprite;
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (!enableHitEffects || hitParticles == null)
        {
            return;
        }

        PixelEffectSpawner spawner = effectSpawner != null ? effectSpawner : GetComponent<PixelEffectSpawner>();
        if (spawner != null)
        {
            spawner.Play(hitParticles, position, Quaternion.identity);
        }
    }
}
