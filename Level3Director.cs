using UnityEngine;

public sealed class Level3Director : MonoBehaviour
{
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
            var colliderList = new System.Collections.Generic.List<Collider2D>();
            var rendererList = new System.Collections.Generic.List<SpriteRenderer>();

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

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Relay Bridge")]
    [SerializeField] private Transform risingBridge;
    [SerializeField] private Transform risingBridgeTarget;
    [SerializeField] private float bridgeTriggerX = 36.5f;
    [SerializeField] private float bridgeMoveSpeed = 2.4f;

    [Header("Pulse Hazards")]
    [SerializeField] private float pulseStartX = 60f;
    [SerializeField] private float pulseEndX = 77f;
    [SerializeField] private float pulseWarningTime = 0.25f;
    [SerializeField] private Color pulseSafeTint = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color pulseWarningTint = new Color(1f, 0.45f, 0.2f, 0.75f);
    [SerializeField] private Color pulseActiveTint = Color.white;
    [SerializeField] private PulseHazardGroup[] pulseGroups = new PulseHazardGroup[0];

    [Header("Pressure Saw")]
    [SerializeField] private Transform pressureSaw;
    [SerializeField] private Transform[] pressureWaypoints = new Transform[0];
    [SerializeField] private float pressureTriggerX = 60.2f;
    [SerializeField] private float pressureStopX = 78f;
    [SerializeField] private float pressureStartDelay = 0.35f;
    [SerializeField] private float pressureSpeed = 3.4f;

    private bool bridgeReleased;
    private bool pressureActive;
    private float pressureTimer;
    private float pulseClock;
    private int pressureWaypointIndex;

    private void Awake()
    {
        ResolvePlayer();

        foreach (PulseHazardGroup group in pulseGroups)
        {
            if (group != null)
            {
                group.Cache();
                group.SetSafe(pulseSafeTint);
            }
        }

        if (pressureSaw != null)
        {
            WaypointFollower follower = pressureSaw.GetComponent<WaypointFollower>();
            if (follower != null)
            {
                follower.enabled = false;
            }

            if (pressureWaypoints.Length > 0 && pressureWaypoints[0] != null)
            {
                pressureSaw.position = pressureWaypoints[0].position;
            }

            pressureSaw.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        ResolvePlayer();
        if (player == null)
        {
            return;
        }

        UpdateBridge();
        UpdatePulseHazards();
        UpdatePressureSaw();
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

    private void UpdateBridge()
    {
        if (risingBridge == null || risingBridgeTarget == null)
        {
            return;
        }

        if (!bridgeReleased && player.position.x >= bridgeTriggerX)
        {
            bridgeReleased = true;
        }

        if (bridgeReleased)
        {
            risingBridge.position = Vector3.MoveTowards(
                risingBridge.position,
                risingBridgeTarget.position,
                bridgeMoveSpeed * Time.deltaTime);
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

    private void UpdatePressureSaw()
    {
        if (pressureSaw == null || pressureWaypoints.Length == 0)
        {
            return;
        }

        if (!pressureActive && player.position.x >= pressureTriggerX)
        {
            pressureActive = true;
            pressureTimer = pressureStartDelay;
            pressureWaypointIndex = Mathf.Min(1, pressureWaypoints.Length - 1);
            pressureSaw.position = pressureWaypoints[0].position;
            pressureSaw.gameObject.SetActive(true);
        }

        if (!pressureActive)
        {
            return;
        }

        if (player.position.x >= pressureStopX || pressureWaypointIndex >= pressureWaypoints.Length)
        {
            pressureSaw.gameObject.SetActive(false);
            pressureActive = false;
            return;
        }

        pressureTimer -= Time.deltaTime;
        if (pressureTimer > 0f)
        {
            return;
        }

        Transform target = pressureWaypoints[pressureWaypointIndex];
        if (target == null)
        {
            pressureWaypointIndex++;
            return;
        }

        pressureSaw.position = Vector3.MoveTowards(
            pressureSaw.position,
            target.position,
            pressureSpeed * Time.deltaTime);

        if (Vector2.Distance(pressureSaw.position, target.position) <= 0.05f)
        {
            pressureWaypointIndex++;
        }
    }
}
