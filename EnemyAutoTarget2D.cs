using System.Collections;
using UnityEngine;

public class EnemyAutoTarget2D : MonoBehaviour, IDamageable
{
    private static Sprite barSprite;

    [Header("Health")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private float deathFadeDuration = 0.35f;

    [Header("Health Bar")]
    [SerializeField] private float minHealthBarWidth = 2.2f;
    [SerializeField] private float minHealthBarHeight = 0.32f;
    [SerializeField] private float healthBarTopPadding = 0.08f;

    [Header("Detect")]
    public float detectRadius = 6f;
    public LayerMask playerLayer;

    [Header("Patrol")]
    public float patrolDistance = 3f;
    public float patrolSpeed = 1.5f;
    [SerializeField] private float patrolWallCheckDistance = 0.18f;
    [SerializeField] private float patrolGroundCheckDistance = 0.55f;
    [SerializeField] private float patrolStuckDuration = 0.18f;
    [SerializeField] private float patrolMinMoveDelta = 0.005f;
    [SerializeField] private LayerMask patrolCollisionMask;

    [Header("Chase")]
    public float moveSpeed = 2.5f;

    [Header("Attack")]
    public float attackRange = 0.18f;
    public float attackCooldown = 1.0f;
    [SerializeField] private float attackWindupTime = 0.35f;
    [SerializeField] private float attackHitConfirmRange = 0.32f;
    [SerializeField] private float attackBoxExtraWidth = 0.14f;
    [SerializeField] private float attackBoxHeightMultiplier = 1.15f;
    [SerializeField] private float attackRecoverTime = 0.12f;
    [SerializeField] private Color attackTelegraphColor = new Color(1f, 0.62f, 0.32f, 1f);
    public int damage = 1;

    [Header("Feedback")]
    [SerializeField] private bool enableEnemyFeedback = false;
    [SerializeField] private float hitShakeIntensity = 0.07f;
    [SerializeField] private float hitShakeDuration = 0.08f;
    [SerializeField] private float deathShakeIntensity = 0.13f;
    [SerializeField] private float deathShakeDuration = 0.14f;
    [SerializeField] private CameraFollow cameraShakeTarget;

    [Header("Effects")]
    [SerializeField] private bool enableEnemyEffects = false;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deathParticles;

    private Rigidbody2D rb;
    private Transform target;
    private Transform fallbackPlayer;
    private float nextAttackTime;
    private Vector2 startPosition;
    private int patrolDirection = 1;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private BoxCollider2D hitbox;
    private Color baseColor = Color.white;
    private Vector3 baseScale;
    private int currentHp;
    private bool isDead;
    private bool isAttacking;
    private float desiredHorizontalSpeed;
    private float lastPatrolX;
    private float patrolStuckTimer;
    private Coroutine flashRoutine;
    private Coroutine attackRoutine;
    private Transform healthBarRoot;
    private SpriteRenderer healthBarBorder;
    private SpriteRenderer healthBarBackground;
    private SpriteRenderer healthBarFill;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int HasTargetHash = Animator.StringToHash("hasTarget");
    private static readonly int AttackHash = Animator.StringToHash("attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        hitbox = GetComponent<BoxCollider2D>();
        startPosition = transform.position;
        baseScale = transform.localScale;
        currentHp = Mathf.Max(1, maxHp);
        lastPatrolX = transform.position.x;

        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        if (patrolCollisionMask.value == 0)
        {
            patrolCollisionMask = LayerMask.GetMask("Ground");
        }

        CreateHealthBar();
        RefreshHealthBar();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        FindTarget();

        if (isAttacking)
        {
            StopMoving();
            FaceTarget();
            UpdateAnimation(false, target != null);
            return;
        }

        if (target == null)
        {
            Patrol();
            return;
        }

        float dist = GetDistanceToTarget();

        if (dist > attackRange)
        {
            ChaseTarget();
        }
        else
        {
            StopMoving();
            FaceTarget();
            UpdateAnimation(false, true);

            if (Time.time >= nextAttackTime)
            {
                BeginAttack();
            }
        }

    }

    private void FixedUpdate()
    {
        if (isDead || rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        rb.velocity = new Vector2(desiredHorizontalSpeed, rb.velocity.y);
    }

    private void LateUpdate()
    {
        UpdateHealthBarPosition();
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHp -= Mathf.Max(0, amount);
        PlayEffect(hitParticles);

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashHit());
        RefreshHealthBar();
        TriggerCameraShake(transform.position, hitShakeIntensity, hitShakeDuration);
    }

    private void Patrol()
    {
        float leftEdge = startPosition.x - patrolDistance;
        float rightEdge = startPosition.x + patrolDistance;
        float movedDistance = Mathf.Abs(transform.position.x - lastPatrolX);

        if (movedDistance <= patrolMinMoveDelta)
        {
            patrolStuckTimer += Time.deltaTime;
        }
        else
        {
            patrolStuckTimer = 0f;
        }

        lastPatrolX = transform.position.x;

        if (transform.position.x <= leftEdge)
        {
            patrolDirection = 1;
        }
        else if (transform.position.x >= rightEdge)
        {
            patrolDirection = -1;
        }

        if (ShouldReversePatrol())
        {
            patrolDirection *= -1;
            patrolStuckTimer = 0f;
        }
        else if (patrolStuckTimer >= patrolStuckDuration)
        {
            patrolDirection *= -1;
            patrolStuckTimer = 0f;
        }

        desiredHorizontalSpeed = patrolDirection * patrolSpeed;
        UpdateFacing(patrolDirection);
        UpdateAnimation(Mathf.Abs(desiredHorizontalSpeed) > 0.01f, false);
    }

    private bool ShouldReversePatrol()
    {
        if (hitbox == null || patrolDirection == 0)
        {
            return false;
        }

        Bounds bounds = hitbox.bounds;
        float direction = Mathf.Sign(patrolDirection);

        Vector2 wallOrigin = new Vector2(
            bounds.center.x + direction * (bounds.extents.x + 0.03f),
            bounds.center.y);
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallOrigin,
            Vector2.right * direction,
            patrolWallCheckDistance,
            patrolCollisionMask);

        Vector2 groundOrigin = new Vector2(
            bounds.center.x + direction * (bounds.extents.x + 0.06f),
            bounds.min.y + 0.05f);
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundOrigin,
            Vector2.down,
            patrolGroundCheckDistance,
            patrolCollisionMask);

        return wallHit.collider != null || groundHit.collider == null;
    }

    private void ChaseTarget()
    {
        float dirX = Mathf.Sign(target.position.x - transform.position.x);
        desiredHorizontalSpeed = dirX * moveSpeed;
        UpdateFacing(dirX);
        UpdateAnimation(Mathf.Abs(desiredHorizontalSpeed) > 0.01f, true);
    }

    private void StopMoving()
    {
        desiredHorizontalSpeed = 0f;

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    private void FaceTarget()
    {
        if (target == null)
        {
            return;
        }

        float dirX = target.position.x - transform.position.x;
        UpdateFacing(dirX);
    }

    private void FindTarget()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerLayer);
        if (hit != null)
        {
            target = hit.transform;
            return;
        }

        if (fallbackPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            fallbackPlayer = playerObject != null ? playerObject.transform : null;
        }

        if (fallbackPlayer == null || !fallbackPlayer.gameObject.activeInHierarchy)
        {
            target = null;
            return;
        }

        float horizontalDistance = Mathf.Abs(fallbackPlayer.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(fallbackPlayer.position.y - transform.position.y);
        target = horizontalDistance <= detectRadius && verticalDistance <= detectRadius * 0.75f
            ? fallbackPlayer
            : null;
    }

    private void BeginAttack()
    {
        if (isAttacking || isDead)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        StopMoving();
        FaceTarget();
        UpdateAnimation(false, target != null);

        if (anim != null)
        {
            anim.SetTrigger(AttackHash);
        }

        if (spriteRenderer != null)
        {
            Color windupColor = attackTelegraphColor;
            windupColor.a = baseColor.a;
            spriteRenderer.color = windupColor;
            transform.localScale = baseScale * 1.08f;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackWindupTime));

        if (!isDead)
        {
            TryDealDamageInAttackWindow();
        }

        if (!isDead)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            transform.localScale = baseScale;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackRecoverTime));

        isAttacking = false;
        attackRoutine = null;
    }

    private bool TryDealDamageInAttackWindow()
    {
        Collider2D hit = FindPlayerColliderInAttackWindow();
        IDamageable dmg = hit != null ? GetDamageable(hit.transform) : null;

        if (dmg == null && target != null && GetDistanceToTarget() <= Mathf.Max(attackRange, attackHitConfirmRange))
        {
            dmg = GetTargetDamageable();
        }

        if (dmg == null)
        {
            return false;
        }

        dmg.TakeDamage(damage);
        return true;
    }

    private Collider2D FindPlayerColliderInAttackWindow()
    {
        Bounds enemyBounds = GetEnemyBounds();
        float direction = GetAttackDirection();
        float reach = Mathf.Max(0.08f, Mathf.Max(attackRange, attackHitConfirmRange) + attackBoxExtraWidth);
        float height = Mathf.Max(0.35f, enemyBounds.size.y * attackBoxHeightMultiplier);

        Vector2 center = new Vector2(
            enemyBounds.center.x + direction * (enemyBounds.extents.x + reach * 0.5f),
            enemyBounds.center.y);
        Vector2 size = new Vector2(reach, height);

        int mask = GetPlayerLayerMask();
        Collider2D[] hits = mask != 0
            ? Physics2D.OverlapBoxAll(center, size, 0f, mask)
            : Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (Collider2D hit in hits)
        {
            if (IsPlayerCollider(hit))
            {
                return hit;
            }
        }

        return null;
    }

    private float GetAttackDirection()
    {
        if (target != null && Mathf.Abs(target.position.x - transform.position.x) > 0.01f)
        {
            return Mathf.Sign(target.position.x - transform.position.x);
        }

        return spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
    }

    private int GetPlayerLayerMask()
    {
        if (playerLayer.value != 0)
        {
            return playerLayer.value;
        }

        return LayerMask.GetMask("Player");
    }

    private static bool IsPlayerCollider(Collider2D hit)
    {
        if (hit == null)
        {
            return false;
        }

        return hit.CompareTag("Player")
            || hit.GetComponentInParent<PlayerHealth2D>() != null;
    }

    private static IDamageable GetDamageable(Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return null;
        }

        IDamageable dmg = hitTransform.GetComponent<IDamageable>();

        if (dmg == null)
        {
            dmg = hitTransform.GetComponentInParent<IDamageable>();
        }

        if (dmg == null)
        {
            dmg = hitTransform.GetComponentInChildren<IDamageable>();
        }

        return dmg;
    }

    private IDamageable GetTargetDamageable()
    {
        if (target == null)
        {
            return null;
        }

        return GetDamageable(target);
    }

    private float GetDistanceToTarget()
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        Bounds enemyBounds = GetEnemyBounds();
        Collider2D targetCollider = target.GetComponent<Collider2D>();

        if (targetCollider == null)
        {
            targetCollider = target.GetComponentInChildren<Collider2D>();
        }

        if (targetCollider == null)
        {
            return Vector2.Distance(transform.position, target.position);
        }

        Vector2 enemyPoint = enemyBounds.ClosestPoint(targetCollider.bounds.center);
        Vector2 targetPoint = targetCollider.bounds.ClosestPoint(enemyBounds.center);
        return Vector2.Distance(enemyPoint, targetPoint);
    }

    private void UpdateFacing(float dirX)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (dirX > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (dirX < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void UpdateAnimation(bool isMoving, bool hasTarget)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(IsMovingHash, isMoving);
        anim.SetBool(HasTargetHash, hasTarget);
    }

    private IEnumerator FlashHit()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.color = new Color(1f, 0.55f, 0.55f, baseColor.a);
        transform.localScale = baseScale * 1.08f;
        yield return new WaitForSeconds(hitFlashDuration);

        if (!isDead)
        {
            spriteRenderer.color = baseColor;
            transform.localScale = baseScale;
        }

        flashRoutine = null;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        target = null;
        nextAttackTime = float.PositiveInfinity;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (anim != null)
        {
            anim.ResetTrigger(AttackHash);
            anim.SetBool(IsMovingHash, false);
            anim.SetBool(HasTargetHash, false);
        }

        if (rb != null)
        {
            desiredHorizontalSpeed = 0f;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }

        RefreshHealthBar();
        transform.localScale = baseScale * 1.1f;
        PlayEffect(deathParticles);
        TriggerCameraShake(transform.position, deathShakeIntensity, deathShakeDuration);
        StartCoroutine(FadeOutAndDestroy());
    }

    private void TriggerCameraShake(Vector3 origin, float intensity, float duration)
    {
        if (!enableEnemyFeedback || intensity <= 0f || duration <= 0f)
        {
            return;
        }

        CameraFollow shakeTarget = cameraShakeTarget != null ? cameraShakeTarget : FindObjectOfType<CameraFollow>();
        if (shakeTarget != null)
        {
            shakeTarget.TriggerShake(origin, intensity, duration);
        }
    }

    private void PlayEffect(ParticleSystem particles)
    {
        if (!enableEnemyEffects || particles == null)
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
            spawner.Play(particles, transform.position, Quaternion.identity);
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color startColor = spriteRenderer.color;
        Vector3 startScale = transform.localScale;
        float duration = Mathf.Max(0.05f, deathFadeDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            spriteRenderer.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            transform.localScale = Vector3.Lerp(startScale, baseScale * 0.2f, t);

            if (healthBarRoot != null)
            {
                SetHealthBarAlpha(1f - t);
            }

            yield return null;
        }

        if (healthBarRoot != null)
        {
            Destroy(healthBarRoot.gameObject);
        }

        Destroy(gameObject);
    }

    private void CreateHealthBar()
    {
        if (healthBarRoot != null)
        {
            return;
        }

        healthBarRoot = new GameObject("EnemyHealthBar").transform;
        healthBarRoot.SetParent(null, false);

        healthBarBorder = CreateBarRenderer("Border", new Color(1f, 1f, 1f, 0.98f), 0);
        healthBarBackground = CreateBarRenderer("Background", new Color(0.08f, 0.08f, 0.08f, 0.96f), 1);
        healthBarFill = CreateBarRenderer("Fill", new Color(0.33f, 0.93f, 0.48f, 1f), 2);

        UpdateHealthBarPosition();
    }

    private SpriteRenderer CreateBarRenderer(string name, Color color, int sortingOrder)
    {
        GameObject barObject = new GameObject(name);
        barObject.transform.SetParent(healthBarRoot, false);

        SpriteRenderer renderer = barObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBarSprite();
        renderer.color = color;
        renderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        renderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 80 + sortingOrder : 80 + sortingOrder;
        return renderer;
    }

    private void UpdateHealthBarPosition()
    {
        if (healthBarRoot == null)
        {
            return;
        }

        Bounds enemyBounds = GetEnemyBounds();
        float barHeight = GetHealthBarHeight(enemyBounds);
        float yOffset = healthBarTopPadding + (barHeight * 0.55f);
        healthBarRoot.position = new Vector3(enemyBounds.center.x, enemyBounds.max.y + yOffset, transform.position.z);
    }

    private void RefreshHealthBar()
    {
        if (healthBarBorder == null || healthBarBackground == null || healthBarFill == null)
        {
            return;
        }

        Bounds enemyBounds = GetEnemyBounds();
        float totalWidth = GetHealthBarWidth(enemyBounds);
        float borderHeight = GetHealthBarHeight(enemyBounds);
        float innerWidth = totalWidth * 0.9f;
        float backgroundHeight = borderHeight * 0.76f;
        float fillHeight = backgroundHeight * 0.82f;
        float ratio = Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));

        healthBarBorder.transform.localPosition = Vector3.zero;
        SetBarWorldSize(healthBarBorder, totalWidth, borderHeight);

        healthBarBackground.transform.localPosition = Vector3.zero;
        SetBarWorldSize(healthBarBackground, innerWidth, backgroundHeight);

        healthBarFill.transform.localPosition = new Vector3((ratio - 1f) * innerWidth * 0.5f, 0f, 0f);
        SetBarWorldSize(healthBarFill, Mathf.Max(0.0001f, innerWidth * ratio), fillHeight);
        healthBarFill.color = Color.Lerp(new Color(0.95f, 0.25f, 0.25f, 1f), new Color(0.33f, 0.93f, 0.48f, 1f), ratio);
        healthBarRoot.gameObject.SetActive(!isDead || currentHp > 0);
        UpdateHealthBarPosition();
    }

    private void SetHealthBarAlpha(float alpha)
    {
        if (healthBarBorder != null)
        {
            Color borderColor = healthBarBorder.color;
            borderColor.a = 0.98f * alpha;
            healthBarBorder.color = borderColor;
        }

        if (healthBarBackground != null)
        {
            Color backgroundColor = healthBarBackground.color;
            backgroundColor.a = 0.92f * alpha;
            healthBarBackground.color = backgroundColor;
        }

        if (healthBarFill != null)
        {
            Color fillColor = healthBarFill.color;
            fillColor.a = alpha;
            healthBarFill.color = fillColor;
        }
    }

    private void SetBarWorldSize(SpriteRenderer renderer, float width, float height)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float safeWidth = Mathf.Max(0.0001f, spriteSize.x);
        float safeHeight = Mathf.Max(0.0001f, spriteSize.y);
        renderer.transform.localScale = new Vector3(width / safeWidth, height / safeHeight, 1f);
    }

    private float GetHealthBarWidth(Bounds enemyBounds)
    {
        return Mathf.Max(minHealthBarWidth, enemyBounds.size.x * 1.35f, enemyBounds.size.y * 0.9f);
    }

    private float GetHealthBarHeight(Bounds enemyBounds)
    {
        return Mathf.Max(minHealthBarHeight, enemyBounds.size.y * 0.18f);
    }

    private Bounds GetEnemyBounds()
    {
        if (hitbox != null)
        {
            return hitbox.bounds;
        }

        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds;
        }

        return new Bounds(transform.position, Vector3.one);
    }

    private static Sprite GetBarSprite()
    {
        if (barSprite != null)
        {
            return barSprite;
        }

        barSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return barSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
