using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LifeBroadcastSceneController : MonoBehaviour
{
    [SerializeField] private Font uiFont;

    private const float IntroDuration = 0.45f;
    private const float LifeLossDuration = 0.6f;
    private const float OutroDuration = 0.25f;
    private const float HoldAfterIntro = 0.15f;
    private const float HoldBeforeReturn = 0.55f;

    private Text titleText;
    private Text livesText;
    private Text statusText;
    private Text sceneText;
    private Image lifeFillImage;
    private RectTransform lifeFillRect;
    private RectTransform panelRect;
    private RectTransform livesRect;
    private RectTransform pipsRoot;
    private CanvasGroup canvasGroup;
    private CanvasGroup panelGroup;
    private Image[] lifePips;
    private float lifeFillWidth = 720f;

    private void Start()
    {
        BuildRuntimeUi();
        StartCoroutine(PlayBroadcast());
    }

    private void BuildRuntimeUi()
    {
        Font font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("LifeBroadcastCanvas");
        canvasObject.layer = 5;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject background = CreateUiObject("Background", canvasObject.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.17f, 0.24f, 0.21f, 1f);
        Stretch(background.GetComponent<RectTransform>());

        CreateStripe("TopStripe", canvasObject.transform, new Color(0.93f, 0.78f, 0.4f, 0.18f), 90f, true);
        CreateStripe("BottomStripe", canvasObject.transform, new Color(0.93f, 0.78f, 0.4f, 0.12f), 110f, false);

        GameObject panelShadow = CreateUiObject("PanelShadow", canvasObject.transform);
        RectTransform panelShadowRect = panelShadow.GetComponent<RectTransform>();
        panelShadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelShadowRect.sizeDelta = new Vector2(1010f, 550f);
        panelShadowRect.anchoredPosition = new Vector2(18f, -18f);
        Image panelShadowImage = panelShadow.AddComponent<Image>();
        panelShadowImage.color = new Color(0.08f, 0.06f, 0.04f, 0.55f);

        GameObject panel = CreateUiObject("Panel", canvasObject.transform);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 520f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.localScale = Vector3.one * 0.92f;

        panelGroup = panel.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.36f, 0.24f, 0.14f, 0.98f);

        GameObject innerPanel = CreateUiObject("InnerPanel", panel.transform);
        RectTransform innerPanelRect = innerPanel.GetComponent<RectTransform>();
        innerPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerPanelRect.sizeDelta = new Vector2(900f, 440f);
        innerPanelRect.anchoredPosition = new Vector2(0f, -8f);
        Image innerPanelImage = innerPanel.AddComponent<Image>();
        innerPanelImage.color = new Color(0.25f, 0.17f, 0.11f, 0.96f);

        CreateBand("HeaderBand", innerPanel.transform, new Vector2(820f, 14f), new Vector2(0f, 178f), new Color(0.94f, 0.8f, 0.39f, 1f));
        CreateBand("FooterBand", innerPanel.transform, new Vector2(820f, 10f), new Vector2(0f, -208f), new Color(0.56f, 0.78f, 0.38f, 0.85f));

        sceneText = CreateText("SceneLabel", innerPanel.transform, font, 18, FontStyle.Normal, new Color(0.94f, 0.88f, 0.72f, 1f));
        sceneText.rectTransform.anchoredPosition = new Vector2(0f, 142f);
        sceneText.rectTransform.sizeDelta = new Vector2(760f, 40f);
        sceneText.text = "DEATH REPORT";

        titleText = CreateText("Title", innerPanel.transform, font, 30, FontStyle.Bold, Color.white);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 92f);
        titleText.rectTransform.sizeDelta = new Vector2(820f, 66f);
        titleText.text = "REMAINING LIVES";

        GameObject pipsObject = CreateUiObject("LifePips", innerPanel.transform);
        pipsRoot = pipsObject.GetComponent<RectTransform>();
        pipsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        pipsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        pipsRoot.sizeDelta = new Vector2(520f, 56f);
        pipsRoot.anchoredPosition = new Vector2(0f, 22f);

        livesText = CreateText("LivesValue", innerPanel.transform, font, 60, FontStyle.Bold, new Color(1f, 0.95f, 0.82f, 1f));
        livesRect = livesText.rectTransform;
        livesRect.anchoredPosition = new Vector2(0f, -40f);
        livesRect.sizeDelta = new Vector2(820f, 96f);

        GameObject meterBackground = CreateUiObject("LifeMeterBackground", innerPanel.transform);
        RectTransform meterBackgroundRect = meterBackground.GetComponent<RectTransform>();
        meterBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        meterBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        meterBackgroundRect.sizeDelta = new Vector2(760f, 42f);
        meterBackgroundRect.anchoredPosition = new Vector2(0f, -118f);
        Image meterBackgroundImage = meterBackground.AddComponent<Image>();
        meterBackgroundImage.color = new Color(0.15f, 0.11f, 0.08f, 1f);

        GameObject meterFill = CreateUiObject("LifeMeterFill", meterBackground.transform);
        lifeFillRect = meterFill.GetComponent<RectTransform>();
        lifeFillRect.anchorMin = new Vector2(0f, 0f);
        lifeFillRect.anchorMax = new Vector2(0f, 1f);
        lifeFillRect.pivot = new Vector2(0f, 0.5f);
        lifeFillRect.anchoredPosition = Vector2.zero;
        lifeFillRect.sizeDelta = new Vector2(720f, 0f);
        lifeFillWidth = 720f;
        lifeFillImage = meterFill.AddComponent<Image>();
        lifeFillImage.color = new Color(0.55f, 0.8f, 0.33f, 1f);

        statusText = CreateText("Status", innerPanel.transform, font, 18, FontStyle.Normal, new Color(0.92f, 0.9f, 0.8f, 1f));
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.rectTransform.anchoredPosition = new Vector2(0f, -182f);
        statusText.rectTransform.sizeDelta = new Vector2(820f, 48f);
    }

    private IEnumerator PlayBroadcast()
    {
        PlayerLifeFlow flow = PlayerLifeFlow.Instance;
        int maxLives = Mathf.Max(1, flow.MaxLives);
        int displayedLives = Mathf.Clamp(flow.PreviousLives, 0, maxLives);
        int targetLives = Mathf.Clamp(flow.CurrentLives, 0, maxLives);
        float initialRatio = maxLives <= 0 ? 0f : (float)displayedLives / maxLives;

        BuildLifePips(maxLives);
        UpdateLifeView(displayedLives, maxLives, initialRatio);
        statusText.text = "Cherries reset. Synchronizing player status...";

        yield return AnimateIntro();
        yield return new WaitForSecondsRealtime(HoldAfterIntro);

        while (displayedLives > targetLives)
        {
            statusText.text = "Life lost. Cherries reset.";
            yield return AnimateLifeChange(displayedLives, displayedLives - 1, maxLives, LifeLossDuration);
            displayedLives--;
        }

        if (flow.IsGameOver)
        {
            statusText.text = "Cherries reset. No lives left. Game over...";
        }
        else
        {
            statusText.text = $"Cherries reset. Lives left: {targetLives}. Returning to {flow.ReturnSceneName}...";
        }

        yield return PulseRect(livesRect, 1.08f, 0.22f);
        yield return new WaitForSecondsRealtime(HoldBeforeReturn);
        yield return AnimateOutro();
        flow.CompleteBroadcast();
    }

    private void BuildLifePips(int maxLives)
    {
        for (int i = pipsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(pipsRoot.GetChild(i).gameObject);
        }

        lifePips = new Image[maxLives];
        float spacing = 86f;
        float totalWidth = Mathf.Max(0f, (maxLives - 1) * spacing);

        for (int i = 0; i < maxLives; i++)
        {
            float x = -totalWidth * 0.5f + i * spacing;

            GameObject pipShadow = CreateUiObject($"LifePipShadow_{i}", pipsRoot);
            RectTransform pipShadowRect = pipShadow.GetComponent<RectTransform>();
            pipShadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            pipShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            pipShadowRect.sizeDelta = new Vector2(54f, 54f);
            pipShadowRect.anchoredPosition = new Vector2(x + 4f, -4f);
            Image pipShadowImage = pipShadow.AddComponent<Image>();
            pipShadowImage.color = new Color(0.08f, 0.06f, 0.04f, 0.65f);

            GameObject pip = CreateUiObject($"LifePip_{i}", pipsRoot);
            RectTransform pipRect = pip.GetComponent<RectTransform>();
            pipRect.anchorMin = new Vector2(0.5f, 0.5f);
            pipRect.anchorMax = new Vector2(0.5f, 0.5f);
            pipRect.sizeDelta = new Vector2(54f, 54f);
            pipRect.anchoredPosition = new Vector2(x, 0f);
            Image pipImage = pip.AddComponent<Image>();
            pipImage.color = new Color(0.55f, 0.8f, 0.33f, 1f);
            lifePips[i] = pipImage;
        }
    }

    private IEnumerator AnimateIntro()
    {
        float elapsed = 0f;

        while (elapsed < IntroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / IntroDuration);
            float eased = EaseOutCubic(t);

            canvasGroup.alpha = eased;
            panelGroup.alpha = eased;
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
            titleText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
        titleText.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator AnimateLifeChange(int fromLives, int toLives, int maxLives, float duration)
    {
        float elapsed = 0f;
        float fromRatio = maxLives <= 0 ? 0f : (float)fromLives / maxLives;
        float toRatio = maxLives <= 0 ? 0f : (float)toLives / maxLives;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInOutCubic(t);
            float ratio = Mathf.Lerp(fromRatio, toRatio, eased);
            float pulse = 1f + Mathf.Sin(eased * Mathf.PI) * 0.08f;

            UpdateLifeView(toLives, maxLives, ratio);
            livesRect.localScale = new Vector3(pulse, pulse, 1f);
            yield return null;
        }

        livesRect.localScale = Vector3.one;
        UpdateLifeView(toLives, maxLives, toRatio);
    }

    private IEnumerator PulseRect(RectTransform target, float peakScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(1f, peakScale, pulse);
            target.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private IEnumerator AnimateOutro()
    {
        float elapsed = 0f;

        while (elapsed < OutroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / OutroDuration);
            float eased = 1f - EaseInOutCubic(t);

            panelGroup.alpha = eased;
            canvasGroup.alpha = eased;
            panelRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.04f, 1f - eased);
            yield return null;
        }

        panelGroup.alpha = 0f;
        canvasGroup.alpha = 0f;
    }

    private void UpdateLifeView(int currentLives, int maxLives, float ratio)
    {
        livesText.text = $"{currentLives} / {maxLives}";

        float width = Mathf.Lerp(0f, lifeFillWidth, Mathf.Clamp01(ratio));
        lifeFillRect.sizeDelta = new Vector2(width, lifeFillRect.sizeDelta.y);

        if (currentLives <= 0)
        {
            lifeFillImage.color = new Color(0.78f, 0.2f, 0.24f, 1f);
        }
        else if (ratio <= 0.34f)
        {
            lifeFillImage.color = new Color(0.95f, 0.62f, 0.22f, 1f);
        }
        else
        {
            lifeFillImage.color = new Color(0.55f, 0.8f, 0.33f, 1f);
        }

        UpdateLifePips(maxLives, ratio);
    }

    private void UpdateLifePips(int maxLives, float ratio)
    {
        if (lifePips == null)
        {
            return;
        }

        int activePips = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(ratio) * maxLives - 0.0001f), 0, maxLives);

        for (int i = 0; i < lifePips.Length; i++)
        {
            lifePips[i].color = i < activePips
                ? new Color(0.94f, 0.82f, 0.42f, 1f)
                : new Color(0.24f, 0.18f, 0.14f, 1f);
        }
    }

    private static void CreateStripe(string objectName, Transform parent, Color color, float height, bool top)
    {
        GameObject stripe = CreateUiObject(objectName, parent);
        RectTransform rect = stripe.GetComponent<RectTransform>();
        rect.anchorMin = top ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchorMax = top ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
        Image image = stripe.AddComponent<Image>();
        image.color = color;
    }

    private static void CreateBand(string objectName, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        GameObject band = CreateUiObject(objectName, parent);
        RectTransform rect = band.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = band.AddComponent<Image>();
        image.color = color;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.layer = 5;
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}
