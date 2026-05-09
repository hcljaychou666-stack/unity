using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class DynamicLighting2D : MonoBehaviour
{
    private const string GeneratedRootName = "__DynamicLightingGenerated";
    private static Sprite pixelSprite;

    [Header("Coverage")]
    [SerializeField] private Vector2 coverageCenter = new Vector2(16.5f, 1f);
    [SerializeField] private Vector2 coverageSize = new Vector2(120f, 18f);

    [Header("Ambient")]
    [SerializeField] private Color ambientColor = new Color(0.08f, 0.12f, 0.18f, 0.35f);
    [SerializeField] private string sortingLayerName = "Background";
    [SerializeField] private int ambientSortingOrder = 3;
    [SerializeField] private Color foregroundDimColor = new Color(0.03f, 0.07f, 0.10f, 0.22f);
    [SerializeField] private string foregroundSortingLayerName = "Default";
    [SerializeField] private int foregroundSortingOrder = 90;

    [Header("Player Light")]
    [SerializeField] private bool enablePlayerLight = true;
    [SerializeField] private Transform playerLightTarget;
    [SerializeField] private float playerLightRadius = 5f;
    [SerializeField] private Color playerLightColor = new Color(1f, 0.95f, 0.7f, 0.25f);
    [SerializeField] private float playerLightFalloff = 0.7f;

    [Header("Collectible Glow")]
    [SerializeField] private bool enableCollectibleGlow = true;
    [SerializeField] private float collectibleGlowRadius = 2.2f;
    [SerializeField] private Color collectibleGlowColor = new Color(1f, 0.92f, 0.36f, 0.3f);
    [SerializeField] private float collectiblePulseSpeed = 1.5f;
    [SerializeField] private float collectiblePulseAmount = 0.15f;

    [Header("Checkpoint Glow")]
    [SerializeField] private bool enableCheckpointGlow = true;
    [SerializeField] private float checkpointGlowRadius = 2.8f;
    [SerializeField] private Color checkpointGlowColor = new Color(0.35f, 0.86f, 0.94f, 0.28f);
    [SerializeField] private float checkpointPulseSpeed = 1.2f;
    [SerializeField] private float checkpointPulseAmount = 0.12f;

    private Transform generatedRoot;
    private Transform ambientOverlay;
    private Transform foregroundOverlay;
    private Transform playerLight;
    private SpriteRenderer playerLightRenderer;
    private int collectibleGlowCount;
    private int checkpointGlowCount;
    private SpriteRenderer[] collectibleGlowPool = new SpriteRenderer[0];
    private SpriteRenderer[] checkpointGlowPool = new SpriteRenderer[0];

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        ClearGenerated();
    }

    private void LateUpdate()
    {
        UpdatePlayerLight();
        UpdateCollectibleGlows();
        UpdateCheckpointGlows();
    }

    private void Rebuild()
    {
        ClearGenerated();
        generatedRoot = CreateEmpty(GeneratedRootName, transform);
        BuildAmbientOverlay();
        BuildForegroundDimOverlay();
        BuildPlayerLight();
    }

    private void BuildAmbientOverlay()
    {
        ambientOverlay = CreateEmpty("AmbientOverlay", generatedRoot);
        ambientOverlay.localPosition = new Vector3(coverageCenter.x, coverageCenter.y, 0f);

        SpriteRenderer sr = ambientOverlay.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixelSprite();
        sr.color = ambientColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = ambientSortingOrder;
        ambientOverlay.localScale = new Vector3(Mathf.Max(1f, coverageSize.x), Mathf.Max(1f, coverageSize.y), 1f);

        ApplyGeneratedFlags(ambientOverlay.gameObject);
    }

    private void BuildForegroundDimOverlay()
    {
        foregroundOverlay = CreateEmpty("ForegroundDimOverlay", generatedRoot);
        foregroundOverlay.localPosition = new Vector3(coverageCenter.x, coverageCenter.y, 0f);

        SpriteRenderer sr = foregroundOverlay.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixelSprite();
        sr.color = foregroundDimColor;
        sr.sortingLayerName = foregroundSortingLayerName;
        sr.sortingOrder = foregroundSortingOrder;
        foregroundOverlay.localScale = new Vector3(Mathf.Max(1f, coverageSize.x), Mathf.Max(1f, coverageSize.y), 1f);

        ApplyGeneratedFlags(foregroundOverlay.gameObject);
    }

    private void BuildPlayerLight()
    {
        if (!enablePlayerLight)
        {
            return;
        }

        playerLight = CreateEmpty("PlayerLight", generatedRoot);

        int texSize = 64;
        Texture2D gradient = CreateRadialGradient(texSize, playerLightColor, playerLightFalloff);

        Sprite lightSprite = Sprite.Create(gradient, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), texSize / Mathf.Max(0.1f, playerLightRadius));
        lightSprite.hideFlags = HideFlags.HideAndDontSave;

        playerLightRenderer = playerLight.gameObject.AddComponent<SpriteRenderer>();
        playerLightRenderer.sprite = lightSprite;
        playerLightRenderer.sortingLayerName = foregroundSortingLayerName;
        playerLightRenderer.sortingOrder = foregroundSortingOrder + 1;
        playerLightRenderer.color = Color.white;

        ApplyGeneratedFlags(playerLight.gameObject);
    }

    private void UpdatePlayerLight()
    {
        if (playerLight == null) return;

        Transform target = playerLightTarget;
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            playerLight.position = target.position;
            playerLight.gameObject.SetActive(true);
        }
        else
        {
            playerLight.gameObject.SetActive(false);
        }
    }

    private void UpdateCollectibleGlows()
    {
        if (!enableCollectibleGlow || Application.isPlaying == false)
        {
            HideAll(collectibleGlowPool);
            return;
        }

        GameObject[] cherries = GameObject.FindGameObjectsWithTag("Cherry");
        EnsurePoolSize(ref collectibleGlowPool, ref collectibleGlowCount,
            Mathf.Min(cherries.Length, 30), "CollectibleGlow", collectibleGlowColor, collectibleGlowRadius);

        float pulse = 1f + Mathf.Sin(Time.time * collectiblePulseSpeed) * collectiblePulseAmount;

        for (int i = 0; i < collectibleGlowPool.Length; i++)
        {
            if (i < cherries.Length && cherries[i] != null)
            {
                collectibleGlowPool[i].transform.position = cherries[i].transform.position;
                Color c = collectibleGlowColor;
                c.a = collectibleGlowColor.a * pulse;
                collectibleGlowPool[i].color = c;
                collectibleGlowPool[i].enabled = true;
            }
            else
            {
                collectibleGlowPool[i].enabled = false;
            }
        }
    }

    private void UpdateCheckpointGlows()
    {
        if (!enableCheckpointGlow || Application.isPlaying == false)
        {
            HideAll(checkpointGlowPool);
            return;
        }

        SceneCheckpointMarker[] checkpoints = FindObjectsOfType<SceneCheckpointMarker>();
        EnsurePoolSize(ref checkpointGlowPool, ref checkpointGlowCount,
            Mathf.Min(checkpoints.Length, 15), "CheckpointGlow", checkpointGlowColor, checkpointGlowRadius);

        float pulse = 1f + Mathf.Sin(Time.time * checkpointPulseSpeed) * checkpointPulseAmount;

        for (int i = 0; i < checkpointGlowPool.Length; i++)
        {
            if (i < checkpoints.Length && checkpoints[i] != null)
            {
                checkpointGlowPool[i].transform.position = checkpoints[i].transform.position;
                Color c = checkpointGlowColor;
                c.a = checkpointGlowColor.a * pulse;
                checkpointGlowPool[i].color = c;
                checkpointGlowPool[i].enabled = true;
            }
            else
            {
                checkpointGlowPool[i].enabled = false;
            }
        }
    }

    private void EnsurePoolSize(ref SpriteRenderer[] pool, ref int lastCount,
        int required, string name, Color baseColor, float radius)
    {
        if (pool.Length >= required) return;

        SpriteRenderer[] newPool = new SpriteRenderer[required];
        for (int i = 0; i < pool.Length; i++)
        {
            newPool[i] = pool[i];
        }

        int texSize = 32;
        Texture2D gradient = CreateRadialGradient(texSize, baseColor, 0.5f);
        Sprite sprite = Sprite.Create(gradient, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), texSize / Mathf.Max(0.1f, radius));
        sprite.hideFlags = HideFlags.HideAndDontSave;

        for (int i = pool.Length; i < required; i++)
        {
            Transform child = CreateEmpty(name + "_" + i, generatedRoot);
            SpriteRenderer sr = child.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = foregroundSortingLayerName;
            sr.sortingOrder = foregroundSortingOrder + 2;
            sr.enabled = false;
            ApplyGeneratedFlags(child.gameObject);
            newPool[i] = sr;
        }

        pool = newPool;
    }

    private static void HideAll(SpriteRenderer[] pool)
    {
        if (pool == null) return;
        foreach (var sr in pool)
        {
            if (sr != null) sr.enabled = false;
        }
    }

    private static Texture2D CreateRadialGradient(int size, Color centerColor, float falloff)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = centerColor.a * Mathf.Pow(Mathf.Clamp01(1f - dist), falloff);
                tex.SetPixel(x, y, new Color(centerColor.r, centerColor.g, centerColor.b, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null) return pixelSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelSprite;
    }

    private Transform CreateEmpty(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        ApplyGeneratedFlags(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void ApplyGeneratedFlags(GameObject go)
    {
        if (!Application.isPlaying)
        {
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
    }
}
