using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LevelVisualPolish : MonoBehaviour
{
    private const string GeneratedRootName = "__LevelVisualPolishGenerated";
    private const string LegacyGeneratedRootName = "__Level1VisualPolishGenerated";

    private static Sprite pixelSprite;

    [Header("Coverage")]
    [SerializeField] private float levelCenterX = 16.5f;
    [SerializeField] private float levelWidth = 112f;
    [SerializeField] private float backdropCenterY = 1f;
    [SerializeField] private float backdropHeight = 12f;
    [SerializeField] private string sortingLayerName = "Background";
    [SerializeField] private int backdropSortingOrder = 4;

    [Header("Palette")]
    [SerializeField] private Color cameraColor = new Color(0.13f, 0.25f, 0.36f, 1f);
    [SerializeField] private Color skyTop = new Color(0.18f, 0.38f, 0.54f, 0.92f);
    [SerializeField] private Color skyMid = new Color(0.27f, 0.53f, 0.60f, 0.74f);
    [SerializeField] private Color skyLow = new Color(0.74f, 0.62f, 0.42f, 0.24f);
    [SerializeField] private Color ridgeFar = new Color(0.09f, 0.28f, 0.31f, 0.38f);
    [SerializeField] private Color ridgeNear = new Color(0.11f, 0.37f, 0.31f, 0.44f);
    [SerializeField] private Color cloudColor = new Color(0.95f, 0.98f, 1f, 0.42f);
    [SerializeField] private Color grassAccent = new Color(0.34f, 0.76f, 0.40f, 0.34f);
    [SerializeField] private Color warmAccent = new Color(1f, 0.76f, 0.38f, 0.20f);
    [SerializeField] private Color shadowAccent = new Color(0.04f, 0.12f, 0.14f, 0.24f);
    [SerializeField] private Color sparkleAccent = new Color(1f, 0.95f, 0.62f, 0.34f);
    [SerializeField] private Color platformGuideAccent = new Color(0.35f, 0.86f, 0.94f, 0.20f);
    [SerializeField] private Color routeAccent = new Color(0.50f, 0.92f, 0.82f, 0.18f);
    [SerializeField] private Color hazardAccent = new Color(1f, 0.24f, 0.16f, 0.24f);
    [SerializeField] private Color collectibleAccent = new Color(1f, 0.92f, 0.36f, 0.26f);

    [Header("Readability")]
    [SerializeField] private bool showRouteGuides;
    [SerializeField] private bool showHazardGuides;
    [SerializeField] private bool showCollectibleGlow;

    [Header("Parallax")]
    [SerializeField] private bool enableParallax = true;
    [SerializeField] private float ridgeFollowFactor = 0.82f;
    [SerializeField] private float cloudFollowFactor = 0.68f;
    [SerializeField] private float foregroundFollowFactor = 0.96f;
    [SerializeField] private float cloudDriftSpeed = 0.12f;
    [SerializeField] private float foregroundSwayAmount = 0.035f;
    [SerializeField] private float foregroundSwaySpeed = 1.25f;

    private Transform ridgeLayer;
    private Transform cloudLayer;
    private Transform foregroundLayer;
    private Vector3 ridgeBaseLocalPosition;
    private Vector3 cloudBaseLocalPosition;
    private Vector3 foregroundBaseLocalPosition;
    private Vector3 parallaxCameraOrigin;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        ClearGenerated();
    }

    private void Update()
    {
        if (!Application.isPlaying || !enableParallax)
        {
            return;
        }

        ApplyParallax();
    }

    [ContextMenu("Rebuild Level Visual Polish")]
    private void Rebuild()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyCameraColor();
        ClearGenerated();

        Transform root = CreateGeneratedRoot();
        Transform backdropLayer = CreateEmpty(root, "Parallax_Backdrop");
        ridgeLayer = CreateEmpty(root, "Parallax_FarRidges");
        cloudLayer = CreateEmpty(root, "Parallax_Clouds");
        foregroundLayer = CreateEmpty(root, "Parallax_ForegroundAccents");

        BuildSky(backdropLayer);
        BuildRidges(ridgeLayer);
        BuildClouds(cloudLayer);
        BuildAccents(foregroundLayer);
        BuildForegroundDepth(foregroundLayer);
        BuildLightMotifs(cloudLayer);
        BuildOptionalRouteGuides(foregroundLayer);
        BuildOptionalHazardGuides(foregroundLayer);
        BuildOptionalCollectibleGlow(cloudLayer);
        CaptureParallaxBases();
    }

    private void ApplyCameraColor()
    {
        UnityEngine.Camera sceneCamera = UnityEngine.Camera.main;
        if (sceneCamera == null)
        {
            return;
        }

        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = cameraColor;
    }

    private void CaptureParallaxBases()
    {
        ridgeBaseLocalPosition = ridgeLayer != null ? ridgeLayer.localPosition : Vector3.zero;
        cloudBaseLocalPosition = cloudLayer != null ? cloudLayer.localPosition : Vector3.zero;
        foregroundBaseLocalPosition = foregroundLayer != null ? foregroundLayer.localPosition : Vector3.zero;
        parallaxCameraOrigin = GetCameraPosition();
    }

    private void ApplyParallax()
    {
        Vector3 cameraDelta = GetCameraPosition() - parallaxCameraOrigin;

        ApplyLayerParallax(ridgeLayer, ridgeBaseLocalPosition, cameraDelta, ridgeFollowFactor, 0f, 0f);
        ApplyLayerParallax(cloudLayer, cloudBaseLocalPosition, cameraDelta, cloudFollowFactor, Time.time * cloudDriftSpeed, 0f);
        ApplyLayerParallax(
            foregroundLayer,
            foregroundBaseLocalPosition,
            cameraDelta,
            foregroundFollowFactor,
            0f,
            Mathf.Sin(Time.time * foregroundSwaySpeed) * foregroundSwayAmount);
    }

    private static void ApplyLayerParallax(Transform layer, Vector3 basePosition, Vector3 cameraDelta, float followFactor, float extraX, float extraY)
    {
        if (layer == null)
        {
            return;
        }

        float clampedFollow = Mathf.Clamp01(followFactor);
        layer.localPosition = basePosition + new Vector3(cameraDelta.x * clampedFollow + extraX, cameraDelta.y * clampedFollow * 0.35f + extraY, 0f);
    }

    private static Vector3 GetCameraPosition()
    {
        UnityEngine.Camera sceneCamera = UnityEngine.Camera.main;
        return sceneCamera != null ? sceneCamera.transform.position : Vector3.zero;
    }

    private Transform CreateGeneratedRoot()
    {
        GameObject root = new GameObject(GeneratedRootName);
        ApplyGeneratedFlags(root);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    private void BuildSky(Transform root)
    {
        Vector2 center = GetBackdropCenter();
        float height = GetBackdropHeight();

        AddRect(root, "Sky_Top_Band", new Vector2(center.x, center.y + height * 0.28f), new Vector2(levelWidth, height * 0.52f), skyTop, backdropSortingOrder);
        AddRect(root, "Sky_Mid_Band", new Vector2(center.x, center.y - height * 0.08f), new Vector2(levelWidth, height * 0.36f), skyMid, backdropSortingOrder + 1);
        AddRect(root, "Warm_Horizon_Band", new Vector2(center.x, center.y - height * 0.36f), new Vector2(levelWidth, height * 0.18f), skyLow, backdropSortingOrder + 2);
    }

    private void BuildRidges(Transform root)
    {
        Vector2 center = GetBackdropCenter();
        float height = GetBackdropHeight();
        float farY = center.y - height * 0.38f;
        float nearY = center.y - height * 0.48f;

        AddRect(root, "Far_Ridge_Left", new Vector2(levelCenterX - 32f, farY), new Vector2(32f, 3.1f), ridgeFar, backdropSortingOrder + 4, 8f);
        AddRect(root, "Far_Ridge_Mid", new Vector2(levelCenterX, farY - 0.2f), new Vector2(38f, 3.5f), ridgeFar, backdropSortingOrder + 4, -6f);
        AddRect(root, "Far_Ridge_Right", new Vector2(levelCenterX + 34f, farY), new Vector2(34f, 3.1f), ridgeFar, backdropSortingOrder + 4, 7f);

        AddRect(root, "Near_Ridge_Left", new Vector2(levelCenterX - 22f, nearY), new Vector2(28f, 2.7f), ridgeNear, backdropSortingOrder + 5, -4f);
        AddRect(root, "Near_Ridge_Mid", new Vector2(levelCenterX + 7f, nearY + 0.15f), new Vector2(34f, 2.8f), ridgeNear, backdropSortingOrder + 5, 5f);
        AddRect(root, "Near_Ridge_Right", new Vector2(levelCenterX + 38f, nearY), new Vector2(26f, 2.5f), ridgeNear, backdropSortingOrder + 5, -5f);
    }

    private void BuildClouds(Transform root)
    {
        Vector2 center = GetBackdropCenter();
        float cloudY = center.y + GetBackdropHeight() * 0.32f;
        int order = backdropSortingOrder + 7;

        AddCloud(root, "Cloud_A", levelCenterX - 36f, cloudY + 0.4f, 1.0f, 0.32f, order);
        AddCloud(root, "Cloud_B", levelCenterX - 18f, cloudY - 0.45f, 0.78f, 0.25f, order);
        AddCloud(root, "Cloud_C", levelCenterX - 2f, cloudY + 0.2f, 1.15f, 0.34f, order);
        AddCloud(root, "Cloud_D", levelCenterX + 16f, cloudY - 0.35f, 0.86f, 0.28f, order);
        AddCloud(root, "Cloud_E", levelCenterX + 34f, cloudY + 0.3f, 1.05f, 0.30f, order);
        AddCloud(root, "Cloud_F", levelCenterX + 49f, cloudY - 0.55f, 0.82f, 0.24f, order);
    }

    private void BuildAccents(Transform root)
    {
        AddRect(root, "Soft_Horizon_Glow", new Vector2(levelCenterX, -0.2f), new Vector2(levelWidth, 0.24f), warmAccent, backdropSortingOrder + 8);

        AddRect(root, "Ground_Accent_Start", new Vector2(-11f, -3.72f), new Vector2(16f, 0.08f), grassAccent, backdropSortingOrder + 11);
        AddRect(root, "Ground_Accent_Mid_A", new Vector2(7f, -3.55f), new Vector2(15f, 0.08f), grassAccent, backdropSortingOrder + 11);
        AddRect(root, "Ground_Accent_Mid_B", new Vector2(25f, -3.35f), new Vector2(14f, 0.08f), grassAccent, backdropSortingOrder + 11);
        AddRect(root, "Ground_Accent_End", new Vector2(46f, -3.45f), new Vector2(20f, 0.08f), grassAccent, backdropSortingOrder + 11);
    }

    private void BuildForegroundDepth(Transform root)
    {
        AddRect(root, "Low_Shadow_Start", new Vector2(levelCenterX - 33f, -4.12f), new Vector2(24f, 0.18f), shadowAccent, backdropSortingOrder + 10);
        AddRect(root, "Low_Shadow_Mid", new Vector2(levelCenterX + 2f, -4.03f), new Vector2(30f, 0.16f), shadowAccent, backdropSortingOrder + 10);
        AddRect(root, "Low_Shadow_End", new Vector2(levelCenterX + 37f, -4.08f), new Vector2(28f, 0.18f), shadowAccent, backdropSortingOrder + 10);

        AddRect(root, "Moving_Platform_Guide_Left", new Vector2(levelCenterX - 21f, -0.72f), new Vector2(4.8f, 0.05f), platformGuideAccent, backdropSortingOrder + 9);
        AddRect(root, "Moving_Platform_Guide_Right", new Vector2(levelCenterX + 23f, 0.82f), new Vector2(5.2f, 0.05f), platformGuideAccent, backdropSortingOrder + 9);
    }

    private void BuildLightMotifs(Transform root)
    {
        Vector2 center = GetBackdropCenter();
        float sparkleY = center.y + GetBackdropHeight() * 0.3f;
        int order = backdropSortingOrder + 12;

        AddSparkle(root, "Sky_Sparkle_A", levelCenterX - 38f, sparkleY + 0.65f, 0.22f, order);
        AddSparkle(root, "Sky_Sparkle_B", levelCenterX - 9f, sparkleY - 0.55f, 0.18f, order);
        AddSparkle(root, "Sky_Sparkle_C", levelCenterX + 18f, sparkleY + 0.35f, 0.20f, order);
        AddSparkle(root, "Sky_Sparkle_D", levelCenterX + 43f, sparkleY - 0.25f, 0.16f, order);

        AddRect(root, "Warm_Path_Glint_A", new Vector2(levelCenterX - 18f, -2.85f), new Vector2(0.55f, 0.05f), sparkleAccent, backdropSortingOrder + 13, -8f);
        AddRect(root, "Warm_Path_Glint_B", new Vector2(levelCenterX + 10f, -2.62f), new Vector2(0.42f, 0.05f), sparkleAccent, backdropSortingOrder + 13, 7f);
        AddRect(root, "Warm_Path_Glint_C", new Vector2(levelCenterX + 33f, -2.72f), new Vector2(0.50f, 0.05f), sparkleAccent, backdropSortingOrder + 13, -6f);
    }

    private void BuildOptionalRouteGuides(Transform root)
    {
        if (!showRouteGuides)
        {
            return;
        }

        int order = backdropSortingOrder + 14;
        float left = levelCenterX - levelWidth * 0.42f;

        AddRect(root, "Route_Guide_Start_Run", new Vector2(left + 9f, -2.88f), new Vector2(13f, 0.06f), routeAccent, order);
        AddRect(root, "Route_Guide_First_Jump", new Vector2(left + 23f, -1.16f), new Vector2(7.5f, 0.055f), routeAccent, order, 8f);
        AddRect(root, "Route_Guide_Mid_Platform", new Vector2(levelCenterX + 2f, 1.05f), new Vector2(12f, 0.06f), routeAccent, order);
        AddRect(root, "Route_Guide_Exit", new Vector2(levelCenterX + 22f, 0.25f), new Vector2(15f, 0.06f), routeAccent, order, -5f);
    }

    private void BuildOptionalHazardGuides(Transform root)
    {
        if (!showHazardGuides)
        {
            return;
        }

        int order = backdropSortingOrder + 15;
        float left = levelCenterX - levelWidth * 0.34f;

        AddRect(root, "Hazard_Low_Saw_Glow", new Vector2(left + 10f, -0.92f), new Vector2(8.5f, 0.16f), hazardAccent, order);
        AddWarningStripes(root, "Hazard_Low_Saw_Stripes", left + 10f, -0.74f, 7, 0.62f, order + 1);

        AddRect(root, "Hazard_Mid_Saw_Glow", new Vector2(levelCenterX + 5f, 0.62f), new Vector2(9f, 0.16f), hazardAccent, order);
        AddWarningStripes(root, "Hazard_Mid_Saw_Stripes", levelCenterX + 5f, 0.8f, 7, 0.64f, order + 1);

        AddRect(root, "Hazard_End_Glow", new Vector2(levelCenterX + 21f, -0.52f), new Vector2(8f, 0.14f), hazardAccent, order);
        AddWarningStripes(root, "Hazard_End_Stripes", levelCenterX + 21f, -0.35f, 6, 0.62f, order + 1);
    }

    private void BuildOptionalCollectibleGlow(Transform root)
    {
        if (!showCollectibleGlow)
        {
            return;
        }

        int order = backdropSortingOrder + 16;

        AddSparkle(root, "Collectible_Glint_Start", levelCenterX - 25f, 4.6f, 0.22f, order);
        AddSparkle(root, "Collectible_Glint_Mid_A", levelCenterX - 6f, 4.2f, 0.18f, order);
        AddSparkle(root, "Collectible_Glint_Mid_B", levelCenterX + 12f, 5.0f, 0.20f, order);
        AddSparkle(root, "Collectible_Glint_End", levelCenterX + 25f, 3.35f, 0.18f, order);

        AddRect(root, "Collectible_Warm_Halo_Start", new Vector2(levelCenterX - 25f, 4.48f), new Vector2(1.2f, 0.08f), collectibleAccent, order);
        AddRect(root, "Collectible_Warm_Halo_Mid", new Vector2(levelCenterX + 6f, 4.32f), new Vector2(1.4f, 0.08f), collectibleAccent, order);
        AddRect(root, "Collectible_Warm_Halo_End", new Vector2(levelCenterX + 25f, 3.23f), new Vector2(1.2f, 0.08f), collectibleAccent, order);
    }

    private void AddWarningStripes(Transform parent, string name, float centerX, float y, int count, float spacing, int sortingOrder)
    {
        Transform stripes = CreateEmpty(parent, name);
        float startX = centerX - (count - 1) * spacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * spacing;
            AddRect(stripes, "Stripe_" + i, new Vector2(x, y), new Vector2(0.34f, 0.055f), hazardAccent, sortingOrder, i % 2 == 0 ? 24f : -24f);
        }
    }

    private void AddSparkle(Transform parent, string name, float x, float y, float size, int sortingOrder)
    {
        Transform sparkle = CreateEmpty(parent, name);
        AddRect(sparkle, "Horizontal", new Vector2(x, y), new Vector2(size, size * 0.16f), sparkleAccent, sortingOrder);
        AddRect(sparkle, "Vertical", new Vector2(x, y), new Vector2(size * 0.16f, size), sparkleAccent, sortingOrder);
    }

    private void AddCloud(Transform parent, string name, float x, float y, float scale, float alpha, int sortingOrder)
    {
        Color color = cloudColor;
        color.a = alpha;

        Transform cloud = CreateEmpty(parent, name);
        AddRect(cloud, "Core", new Vector2(x, y), new Vector2(2.0f * scale, 0.45f * scale), color, sortingOrder);
        AddRect(cloud, "Left_Puff", new Vector2(x - 0.75f * scale, y - 0.12f * scale), new Vector2(0.95f * scale, 0.34f * scale), color, sortingOrder);
        AddRect(cloud, "Top_Puff", new Vector2(x - 0.1f * scale, y + 0.24f * scale), new Vector2(1.15f * scale, 0.36f * scale), color, sortingOrder);
        AddRect(cloud, "Right_Puff", new Vector2(x + 0.82f * scale, y - 0.08f * scale), new Vector2(1.05f * scale, 0.33f * scale), color, sortingOrder);
    }

    private Transform CreateEmpty(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        ApplyGeneratedFlags(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private void AddRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder, float rotationZ = 0f)
    {
        GameObject go = new GameObject(name);
        ApplyGeneratedFlags(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPixelSprite();
        renderer.color = color;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder;
        renderer.maskInteraction = SpriteMaskInteraction.None;
    }

    private Vector2 GetBackdropCenter()
    {
        return new Vector2(levelCenterX, backdropCenterY);
    }

    private float GetBackdropHeight()
    {
        return Mathf.Max(8f, backdropHeight);
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
        {
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "LevelPolishPixel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.name = "LevelPolishPixelSprite";
        pixelSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelSprite;
    }

    private void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName || child.name == LegacyGeneratedRootName)
            {
                DestroyGenerated(child.gameObject);
            }
        }
    }

    private static void DestroyGenerated(GameObject go)
    {
        if (Application.isPlaying)
        {
            Destroy(go);
        }
        else
        {
#if UNITY_EDITOR
            ClearSelectionIfGeneratedObjectIsSelected(go.transform);
#endif
            DestroyImmediate(go);
        }
    }

#if UNITY_EDITOR
    private static void ClearSelectionIfGeneratedObjectIsSelected(Transform generatedRoot)
    {
        if (generatedRoot == null)
        {
            return;
        }

        foreach (Transform selected in Selection.transforms)
        {
            if (selected == generatedRoot || selected.IsChildOf(generatedRoot))
            {
                Selection.activeObject = null;
                return;
            }
        }
    }
#endif

    private static void ApplyGeneratedFlags(GameObject go)
    {
        if (!Application.isPlaying)
        {
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
    }
}
