using UnityEngine;

[DisallowMultipleComponent]
public class AmbientParticleZone : MonoBehaviour
{
    [System.Serializable]
    private sealed class AmbientParticleVariant
    {
        [SerializeField] private ParticleSystem prefab;
        [SerializeField] private float weight = 1f;

        public ParticleSystem Prefab => prefab;
        public float Weight => Mathf.Max(0f, weight);
    }

    [Header("Zone")]
    [SerializeField] private Vector2 zoneSize = new Vector2(20f, 10f);
    [SerializeField] private bool followMainCamera = true;
    [SerializeField] private Color gizmoColor = new Color(0.4f, 0.7f, 0.3f, 0.3f);

    [Header("Particles")]
    [SerializeField] private bool enableEffects = true;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private AmbientParticleVariant[] particlePrefabs = new AmbientParticleVariant[0];
    [SerializeField] private ParticleSystem floatingParticlePrefab;
    [SerializeField] private float spawnInterval = 0.65f;
    [SerializeField] private int maxActiveParticles = 18;
    [SerializeField] private float particleLifetime = 4f;

    private float nextSpawnTime;
    private int activeCount;

    private void Update()
    {
        if (!enableEffects) return;

        ParticleSystem selectedPrefab = ChooseParticlePrefab();
        if (selectedPrefab == null) return;

        float time = Application.isPlaying ? Time.time : 0f;
        if (time < nextSpawnTime) return;
        if (activeCount >= maxActiveParticles) return;

        nextSpawnTime = time + Mathf.Max(0.02f, spawnInterval);
        SpawnParticle(selectedPrefab);
    }

    private void SpawnParticle(ParticleSystem selectedPrefab)
    {
        PixelEffectSpawner spawner = effectSpawner != null
            ? effectSpawner
            : GetComponent<PixelEffectSpawner>();

        if (spawner == null) return;

        float halfW = zoneSize.x * 0.5f;
        float halfH = zoneSize.y * 0.5f;
        Vector3 randomOffset = new Vector3(
            Random.Range(-halfW, halfW),
            Random.Range(-halfH, halfH),
            0f);
        Vector3 spawnPos = GetZoneCenter() + randomOffset;

        spawner.Play(selectedPrefab, spawnPos, Quaternion.identity);
        activeCount++;

        StartCoroutine(DecrementAfterDelay(particleLifetime));
    }

    private ParticleSystem ChooseParticlePrefab()
    {
        if (particlePrefabs == null || particlePrefabs.Length == 0)
        {
            return floatingParticlePrefab;
        }

        float totalWeight = 0f;
        for (int i = 0; i < particlePrefabs.Length; i++)
        {
            AmbientParticleVariant variant = particlePrefabs[i];
            if (variant != null && variant.Prefab != null)
            {
                totalWeight += variant.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return floatingParticlePrefab;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < particlePrefabs.Length; i++)
        {
            AmbientParticleVariant variant = particlePrefabs[i];
            if (variant == null || variant.Prefab == null)
            {
                continue;
            }

            roll -= variant.Weight;
            if (roll <= 0f)
            {
                return variant.Prefab;
            }
        }

        return floatingParticlePrefab;
    }

    private Vector3 GetZoneCenter()
    {
        if (!followMainCamera)
        {
            return transform.position;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return transform.position;
        }

        Vector3 center = mainCamera.transform.position + transform.localPosition;
        center.z = transform.position.z;
        return center;
    }

    private System.Collections.IEnumerator DecrementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        activeCount = Mathf.Max(0, activeCount - 1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, zoneSize);
    }
}
