using UnityEngine;

[DisallowMultipleComponent]
public class PixelEffectSpawner : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private bool enableEffects = false;
    [SerializeField] private bool detachSpawnedEffects = true;
    [SerializeField] private float fallbackDestroyDelay = 2f;

    public void Play(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (!enableEffects || prefab == null)
        {
            return;
        }

        Transform parent = detachSpawnedEffects ? null : transform;
        ParticleSystem instance = Instantiate(prefab, position, rotation, parent);
        instance.Play(true);

        float destroyDelay = instance.main.duration + instance.main.startLifetime.constantMax;
        Destroy(instance.gameObject, Mathf.Max(fallbackDestroyDelay, destroyDelay));
    }

    public void PlayAtTransform(ParticleSystem prefab, Transform target)
    {
        if (target == null)
        {
            return;
        }

        Play(prefab, target.position, target.rotation);
    }
}
