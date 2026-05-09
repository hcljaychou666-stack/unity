using System.Collections.Generic;
using UnityEngine;

public sealed class Level2Director : MonoBehaviour
{
    [System.Serializable]
    private sealed class FallingPlatform
    {
        [SerializeField] private Transform platform;
        [SerializeField] private Collider2D contactCollider;

        private Vector3 startPosition;
        private bool cached;
        private bool armed;
        private bool falling;
        private bool hidden;
        private float timer;
        private Collider2D[] colliders = new Collider2D[0];
        private SpriteRenderer[] renderers = new SpriteRenderer[0];
        private WaypointFollower waypointFollower;
        private bool waypointFollowerWasEnabled;

        public Transform Platform => platform;

        public void Cache()
        {
            if (platform == null)
            {
                return;
            }

            startPosition = platform.position;
            colliders = platform.GetComponentsInChildren<Collider2D>(true);
            renderers = platform.GetComponentsInChildren<SpriteRenderer>(true);
            waypointFollower = platform.GetComponent<WaypointFollower>();
            waypointFollowerWasEnabled = waypointFollower != null && waypointFollower.enabled;
            if (contactCollider == null)
            {
                contactCollider = platform.GetComponentInChildren<Collider2D>(true);
            }

            cached = true;
        }

        public void Tick(
            Transform player,
            Collider2D playerCollider,
            float fallDelay,
            float fallSpeed,
            float respawnDelay)
        {
            if (platform == null)
            {
                return;
            }

            if (!cached)
            {
                Cache();
            }

            if (hidden)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    Respawn();
                }

                return;
            }

            if (!falling && !armed && IsPlayerStandingOnPlatform(playerCollider))
            {
                armed = true;
                timer = Mathf.Max(0f, fallDelay);
            }

            if (armed && !falling)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    falling = true;
                    if (waypointFollower != null)
                    {
                        waypointFollower.enabled = false;
                    }
                }
            }

            if (!falling)
            {
                return;
            }

            platform.position += Vector3.down * Mathf.Max(0f, fallSpeed) * Time.deltaTime;
            if (platform.position.y <= startPosition.y - 5f)
            {
                Hide(player, respawnDelay);
            }
        }

        private bool IsPlayerStandingOnPlatform(Collider2D playerCollider)
        {
            if (playerCollider == null)
            {
                return false;
            }

            Collider2D platformCollider = contactCollider != null ? contactCollider : FirstCollider();
            if (platformCollider == null)
            {
                return false;
            }

            Bounds playerBounds = playerCollider.bounds;
            Bounds platformBounds = platformCollider.bounds;
            bool overlapsX = playerBounds.max.x > platformBounds.min.x + 0.05f
                && playerBounds.min.x < platformBounds.max.x - 0.05f;
            bool closeAbove = playerBounds.min.y >= platformBounds.max.y - 0.22f
                && playerBounds.min.y <= platformBounds.max.y + 0.32f;
            return overlapsX && closeAbove;
        }

        private Collider2D FirstCollider()
        {
            foreach (Collider2D col in colliders)
            {
                if (col != null)
                {
                    return col;
                }
            }

            return null;
        }

        private void Hide(Transform player, float respawnDelay)
        {
            if (player != null && player.parent == platform)
            {
                player.SetParent(null);
            }

            platform.gameObject.SetActive(false);
            hidden = true;
            falling = false;
            armed = false;
            timer = Mathf.Max(0.05f, respawnDelay);
        }

        private void Respawn()
        {
            platform.position = startPosition;
            platform.gameObject.SetActive(true);

            foreach (Collider2D col in colliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            if (waypointFollower != null)
            {
                waypointFollower.enabled = waypointFollowerWasEnabled;
            }

            hidden = false;
            falling = false;
            armed = false;
            timer = 0f;
        }
    }

    [System.Serializable]
    private sealed class PulseHazardGroup
    {
        [SerializeField] private GameObject[] hazards = new GameObject[0];
        [SerializeField] private float activeDuration = 0.85f;
        [SerializeField] private float safeDuration = 0.85f;
        [SerializeField] private float phaseOffset;

        private Collider2D[] colliders = new Collider2D[0];
        private SpriteRenderer[] renderers = new SpriteRenderer[0];

        public void Cache()
        {
            var colliderList = new List<Collider2D>();
            var rendererList = new List<SpriteRenderer>();

            foreach (GameObject hazard in hazards)
            {
                if (hazard == null)
                {
                    continue;
                }

                colliderList.AddRange(hazard.GetComponentsInChildren<Collider2D>(true));
                rendererList.AddRange(hazard.GetComponentsInChildren<SpriteRenderer>(true));
            }

            colliders = colliderList.ToArray();
            renderers = rendererList.ToArray();
        }

        public void SetSafe(Color safeTint)
        {
            SetColliders(false);
            SetRenderers(safeTint);
        }

        public void Evaluate(float time, float warningTime, Color safeTint, Color warningTint, Color activeTint)
        {
            float cycle = Mathf.Max(0.1f, activeDuration + safeDuration);
            float localTime = Mathf.Repeat(time + phaseOffset, cycle);
            bool active = localTime < activeDuration;
            bool warning = !active && localTime >= cycle - Mathf.Max(0f, warningTime);

            SetColliders(active);
            SetRenderers(active ? activeTint : warning ? warningTint : safeTint);
        }

        private void SetColliders(bool enabled)
        {
            foreach (Collider2D col in colliders)
            {
                if (col != null)
                {
                    col.enabled = enabled;
                }
            }
        }

        private void SetRenderers(Color tint)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.color = tint;
                }
            }
        }
    }

    [System.Serializable]
    private sealed class DimZone
    {
        [SerializeField] private float startX;
        [SerializeField] private float endX;
        [SerializeField] private SpriteRenderer overlay;
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 0.45f;
        [SerializeField] private float fadeSpeed = 4f;

        public void Tick(float playerX)
        {
            if (overlay == null)
            {
                return;
            }

            float minX = Mathf.Min(startX, endX);
            float maxX = Mathf.Max(startX, endX);
            float targetAlpha = playerX >= minX && playerX <= maxX ? activeAlpha : 0f;
            Color color = overlay.color;
            color.a = Mathf.MoveTowards(color.a, targetAlpha, Mathf.Max(0f, fadeSpeed) * Time.deltaTime);
            overlay.color = color;
            overlay.enabled = color.a > 0.01f;
        }
    }

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Falling Platforms")]
    [SerializeField] private FallingPlatform[] fallingPlatforms = new FallingPlatform[0];
    [SerializeField] private float fallDelay = 0.6f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float respawnDelay = 3f;

    [Header("Rising Hazards")]
    [SerializeField] private Transform[] risingHazards = new Transform[0];
    [SerializeField] private float riseTriggerX = 60f;
    [SerializeField] private float riseSpeed = 1.8f;
    [SerializeField] private float riseStartY = -7f;
    [SerializeField] private float riseStopY = -3f;

    [Header("Shoot Switches")]
    [SerializeField] private GameObject[] shootSwitches = new GameObject[0];
    [SerializeField] private Transform[] switchTargetPlatforms = new Transform[0];
    [SerializeField] private float switchMoveSpeed = 3f;
    [SerializeField] private Transform[] switchTargetPositions = new Transform[0];

    [Header("Pulse Hazards")]
    [SerializeField] private float pulseStartX = 60f;
    [SerializeField] private float pulseEndX = 82f;
    [SerializeField] private float pulseWarningTime = 0.25f;
    [SerializeField] private Color pulseSafeTint = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color pulseWarningTint = new Color(1f, 0.45f, 0.2f, 0.75f);
    [SerializeField] private Color pulseActiveTint = Color.white;
    [SerializeField] private PulseHazardGroup[] pulseGroups = new PulseHazardGroup[0];

    [Header("Dim Zones")]
    [SerializeField] private DimZone[] dimZones = new DimZone[0];

    private bool risingHazardsReleased;
    private bool[] switchActivated = new bool[0];
    private float pulseClock;
    private Collider2D playerCollider;

    private void Awake()
    {
        ResolvePlayer();
        CachePlayerCollider();
        CacheFallingPlatforms();
        CachePulseHazards();
        CacheShootSwitches();
        ResetRisingHazards();
    }

    private void Update()
    {
        ResolvePlayer();
        CachePlayerCollider();

        if (player == null)
        {
            return;
        }

        UpdateFallingPlatforms();
        UpdateRisingHazards();
        UpdateShootSwitchTargets();
        UpdatePulseHazards();
        UpdateDimZones();
    }

    public void ActivateShootSwitch(int switchIndex)
    {
        if (switchIndex < 0 || switchIndex >= switchActivated.Length)
        {
            return;
        }

        switchActivated[switchIndex] = true;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void CachePlayerCollider()
    {
        if (player == null)
        {
            playerCollider = null;
            return;
        }

        if (playerCollider == null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }
    }

    private void CacheFallingPlatforms()
    {
        foreach (FallingPlatform platform in fallingPlatforms)
        {
            if (platform != null)
            {
                platform.Cache();
            }
        }
    }

    private void CachePulseHazards()
    {
        foreach (PulseHazardGroup group in pulseGroups)
        {
            if (group != null)
            {
                group.Cache();
                group.SetSafe(pulseSafeTint);
            }
        }
    }

    private void CacheShootSwitches()
    {
        switchActivated = new bool[shootSwitches != null ? shootSwitches.Length : 0];

        for (int i = 0; i < switchActivated.Length; i++)
        {
            GameObject switchObject = shootSwitches[i];
            if (switchObject == null)
            {
                continue;
            }

            Level2ShootSwitch shootSwitch = switchObject.GetComponent<Level2ShootSwitch>();
            if (shootSwitch == null)
            {
                shootSwitch = switchObject.AddComponent<Level2ShootSwitch>();
            }

            shootSwitch.Configure(this, i);
        }
    }

    private void ResetRisingHazards()
    {
        foreach (Transform hazard in risingHazards)
        {
            if (hazard != null)
            {
                hazard.position = new Vector3(hazard.position.x, riseStartY, hazard.position.z);
            }
        }
    }

    private void UpdateFallingPlatforms()
    {
        foreach (FallingPlatform platform in fallingPlatforms)
        {
            if (platform != null)
            {
                platform.Tick(player, playerCollider, fallDelay, fallSpeed, respawnDelay);
            }
        }
    }

    private void UpdateRisingHazards()
    {
        if (!risingHazardsReleased && player.position.x >= riseTriggerX)
        {
            risingHazardsReleased = true;
        }

        if (!risingHazardsReleased)
        {
            return;
        }

        foreach (Transform hazard in risingHazards)
        {
            if (hazard == null)
            {
                continue;
            }

            Vector3 target = new Vector3(hazard.position.x, riseStopY, hazard.position.z);
            hazard.position = Vector3.MoveTowards(hazard.position, target, Mathf.Max(0f, riseSpeed) * Time.deltaTime);
        }
    }

    private void UpdateShootSwitchTargets()
    {
        if (switchActivated == null)
        {
            return;
        }

        int count = Mathf.Min(switchActivated.Length, switchTargetPlatforms.Length, switchTargetPositions.Length);
        for (int i = 0; i < count; i++)
        {
            if (!switchActivated[i] || switchTargetPlatforms[i] == null || switchTargetPositions[i] == null)
            {
                continue;
            }

            switchTargetPlatforms[i].position = Vector3.MoveTowards(
                switchTargetPlatforms[i].position,
                switchTargetPositions[i].position,
                Mathf.Max(0f, switchMoveSpeed) * Time.deltaTime);
        }
    }

    private void UpdatePulseHazards()
    {
        if (player.position.x < pulseStartX || player.position.x > pulseEndX)
        {
            foreach (PulseHazardGroup group in pulseGroups)
            {
                if (group != null)
                {
                    group.SetSafe(pulseSafeTint);
                }
            }

            return;
        }

        pulseClock += Time.deltaTime;

        foreach (PulseHazardGroup group in pulseGroups)
        {
            if (group != null)
            {
                group.Evaluate(pulseClock, pulseWarningTime, pulseSafeTint, pulseWarningTint, pulseActiveTint);
            }
        }
    }

    private void UpdateDimZones()
    {
        foreach (DimZone dimZone in dimZones)
        {
            if (dimZone != null)
            {
                dimZone.Tick(player.position.x);
            }
        }
    }
}

public sealed class Level2ShootSwitch : MonoBehaviour, IDamageable
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color inactiveTint = new Color(0.95f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color activatedTint = new Color(0.35f, 1f, 0.72f, 1f);

    private Level2Director director;
    private int switchIndex = -1;
    private bool activated;

    private void Awake()
    {
        CacheRenderer();
        RefreshVisual();
    }

    public void Configure(Level2Director owner, int index)
    {
        director = owner;
        switchIndex = index;
        CacheRenderer();
        RefreshVisual();
    }

    public void TakeDamage(int amount)
    {
        if (activated || amount <= 0)
        {
            return;
        }

        activated = true;
        if (director != null)
        {
            director.ActivateShootSwitch(switchIndex);
        }

        RefreshVisual();
    }

    private void CacheRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = activated ? activatedTint : inactiveTint;
        }
    }
}
