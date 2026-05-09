using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class TutorialSceneController : MonoBehaviour
{
    private const int PageCount = 4;

    [SerializeField] private Font uiFont;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1280f, 720f);
    [SerializeField] private Vector2 pageImageSize = new Vector2(520f, 260f);
    [SerializeField] private int pageTitleFontSize = 26;
    [SerializeField] private int pageBodyFontSize = 20;
    [SerializeField] private float dotSize = 14f;
    [SerializeField] private string levelSelectSceneName = "SelectLevel";
    [SerializeField] private TutorialPageArt[] pageArt = CreateDefaultPageArt();

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.13f, 0.25f, 0.36f, 1f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.1f, 0.08f, 0.92f);
    [SerializeField] private Color titleColor = new Color(1f, 0.86f, 0.42f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.98f, 0.86f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.22f, 0.18f, 0.14f, 1f);
    [SerializeField] private Color buttonHighlightedColor = new Color(0.32f, 0.26f, 0.19f, 1f);
    [SerializeField] private Color inactiveDotColor = new Color(0.34f, 0.29f, 0.22f, 1f);
    [SerializeField] private Color activeDotColor = new Color(1f, 0.86f, 0.42f, 1f);

    private readonly TutorialPage[] pages =
    {
        new TutorialPage("MOVE", "A/D or Left/Right arrows move your hero across platforms.", new Color(0.56f, 0.78f, 0.38f, 1f)),
        new TutorialPage("JUMP", "Press Space to jump. Use timing to clear traps and gaps.", new Color(0.94f, 0.8f, 0.39f, 1f)),
        new TutorialPage("SHOOT", "Press F, Left Ctrl, or Mouse Left to fire a projectile.", new Color(0.92f, 0.46f, 0.38f, 1f)),
        new TutorialPage("COLLECT", "Grab cherries to raise your score and chase the ranking.", new Color(1f, 0.95f, 0.62f, 1f))
    };

    private Font resolvedFont;
    private Image vignetteBackgroundImage;
    private Image characterImage;
    private Image supportImage;
    private RectTransform characterRect;
    private RectTransform supportRect;
    private Text titleText;
    private Text bodyText;
    private Image[] dotImages;
    private int currentPage;

    [Serializable]
    private class TutorialPageArt
    {
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private Sprite supportSprite;
        [SerializeField] private float characterScale = 4f;
        [SerializeField] private float supportScale = 3f;
        [SerializeField] private Vector2 characterPosition = new Vector2(-112f, -38f);
        [SerializeField] private Vector2 supportPosition = new Vector2(122f, -34f);
        [SerializeField] private bool flipCharacter;

        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite CharacterSprite => characterSprite;
        public Sprite SupportSprite => supportSprite;
        public float CharacterScale => Mathf.Max(0.5f, characterScale);
        public float SupportScale => Mathf.Max(0.5f, supportScale);
        public Vector2 CharacterPosition => characterPosition;
        public Vector2 SupportPosition => supportPosition;
        public bool FlipCharacter => flipCharacter;

        public bool HasCompleteSprites()
        {
            return backgroundSprite != null && characterSprite != null && supportSprite != null;
        }

        public void ApplyLayout(float newCharacterScale, float newSupportScale, Vector2 newCharacterPosition, Vector2 newSupportPosition, bool newFlipCharacter)
        {
            characterScale = newCharacterScale;
            supportScale = newSupportScale;
            characterPosition = newCharacterPosition;
            supportPosition = newSupportPosition;
            flipCharacter = newFlipCharacter;
        }

#if UNITY_EDITOR
        public bool FillMissingSprites(Sprite newBackgroundSprite, Sprite newCharacterSprite, Sprite newSupportSprite)
        {
            bool changed = false;
            changed |= AssignIfMissing(ref backgroundSprite, newBackgroundSprite);
            changed |= AssignIfMissing(ref characterSprite, newCharacterSprite);
            changed |= AssignIfMissing(ref supportSprite, newSupportSprite);
            return changed;
        }

        private static bool AssignIfMissing(ref Sprite target, Sprite sprite)
        {
            if (target != null || sprite == null)
            {
                return false;
            }

            target = sprite;
            return true;
        }
#endif
    }

    private struct TutorialPage
    {
        public readonly string Title;
        public readonly string Body;
        public readonly Color Accent;

        public TutorialPage(string title, string body, Color accent)
        {
            Title = title;
            Body = body;
            Accent = accent;
        }
    }

#if UNITY_EDITOR
    private struct SpritePath
    {
        public readonly string Path;
        public readonly int Index;

        public SpritePath(string path, int index = 0)
        {
            Path = path;
            Index = index;
        }
    }

    private static readonly SpritePath[] DefaultBackgroundSprites =
    {
        new SpritePath("Assets/Pixel Adventure 1/Assets/Background/Blue.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Background/Blue.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Background/Purple.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Background/Yellow.png")
    };

    private static readonly SpritePath[] DefaultCharacterSprites =
    {
        new SpritePath("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Fall (32x32).png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Jump (32x32).png", 0),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Main Characters/Pink Man/Jump (32x32).png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Jump (32x32).png", 0)
    };

    private static readonly SpritePath[] DefaultSupportSprites =
    {
        new SpritePath("Assets/Pixel Adventure 1/Assets/Traps/Platforms/Brown Off.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Traps/Spikes/Idle.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Traps/Falling Platforms/Off.png"),
        new SpritePath("Assets/Pixel Adventure 1/Assets/Items/Checkpoints/Checkpoint/Checkpoint (No Flag).png")
    };
#endif

    private void Reset()
    {
#if UNITY_EDITOR
        AutoFillExistingArt();
#endif
    }

    private void OnValidate()
    {
        EnsurePageArtArray();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        EnsurePageArtArray();

        resolvedFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUi();
        ShowPage(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            ShowPage(currentPage + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            ShowPage(currentPage - 1);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            BackToLevelSelect();
        }
    }

    public void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    public void BackToLevelSelect()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("TutorialCanvas");
        canvasObject.layer = 5;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        Stretch(canvasObject.GetComponent<RectTransform>());

        GameObject background = CreateUiObject("Background", canvasObject.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        Stretch(background.GetComponent<RectTransform>());

        CreateStripe("TopStripe", canvasObject.transform, new Color(0.93f, 0.78f, 0.4f, 0.18f), 82f, true);
        CreateStripe("BottomStripe", canvasObject.transform, new Color(0.56f, 0.78f, 0.38f, 0.16f), 96f, false);

        GameObject panel = CreateUiObject("TutorialPanel", canvasObject.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 560f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText("PageTitle", panel.transform, pageTitleFontSize, FontStyle.Bold, titleColor);
        titleText.rectTransform.sizeDelta = new Vector2(640f, 44f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 230f);
        AddOutline(titleText, new Vector2(1.5f, -1.5f));

        BuildVignette(panel.transform);

        bodyText = CreateText("PageBody", panel.transform, pageBodyFontSize, FontStyle.Bold, bodyColor);
        bodyText.rectTransform.sizeDelta = new Vector2(620f, 72f);
        bodyText.rectTransform.anchoredPosition = new Vector2(0f, -130f);
        bodyText.lineSpacing = 1.08f;
        AddOutline(bodyText, new Vector2(1.2f, -1.2f));

        Button previous = CreateButton("PreviousButton", panel.transform, "<", new Vector2(-334f, 62f), new Vector2(52f, 78f));
        previous.onClick.AddListener(PreviousPage);

        Button next = CreateButton("NextButton", panel.transform, ">", new Vector2(334f, 62f), new Vector2(52f, 78f));
        next.onClick.AddListener(NextPage);

        Button back = CreateButton("BackButton", panel.transform, "Back", new Vector2(278f, -236f), new Vector2(130f, 38f));
        back.onClick.AddListener(BackToLevelSelect);

        BuildDots(panel.transform);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(next.gameObject);
        }
    }

    private void BuildVignette(Transform parent)
    {
        GameObject frame = CreateUiObject("PixelArtVignette", parent);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = pageImageSize;
        frameRect.anchoredPosition = new Vector2(0f, 62f);
        frame.AddComponent<RectMask2D>();

        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.06f, 0.07f, 0.08f, 1f);

        GameObject background = CreateUiObject("ExistingBackgroundSprite", frame.transform);
        vignetteBackgroundImage = background.AddComponent<Image>();
        vignetteBackgroundImage.preserveAspect = false;
        Stretch(background.GetComponent<RectTransform>());

        CreateVignetteBand("FarGroundShadow", frame.transform, new Vector2(0f, -88f), new Vector2(pageImageSize.x, 48f), new Color(0.04f, 0.09f, 0.08f, 0.52f));
        CreateVignetteBand("GroundTop", frame.transform, new Vector2(0f, -72f), new Vector2(pageImageSize.x, 18f), new Color(0.42f, 0.35f, 0.18f, 0.92f));
        CreateVignetteBand("GroundFace", frame.transform, new Vector2(0f, -102f), new Vector2(pageImageSize.x, 46f), new Color(0.18f, 0.12f, 0.08f, 0.94f));
        CreateVignetteBand("WarmPixelHighlight", frame.transform, new Vector2(-118f, -62f), new Vector2(156f, 5f), new Color(1f, 0.86f, 0.42f, 0.42f));
        CreateVignetteBand("GrassPixelHighlight", frame.transform, new Vector2(126f, -60f), new Vector2(138f, 5f), new Color(0.56f, 0.78f, 0.38f, 0.42f));

        characterImage = CreateSpriteImage("CharacterSprite", frame.transform, out characterRect);
        supportImage = CreateSpriteImage("SupportSprite", frame.transform, out supportRect);
    }

    private void BuildDots(Transform parent)
    {
        GameObject dotRoot = CreateUiObject("PageDots", parent);
        RectTransform rootRect = dotRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(240f, 28f);
        rootRect.anchoredPosition = new Vector2(0f, -212f);

        dotImages = new Image[pages.Length];
        float spacing = 28f;
        float startX = -spacing * (pages.Length - 1) * 0.5f;

        for (int i = 0; i < pages.Length; i++)
        {
            GameObject dot = CreateUiObject("PageDot_" + (i + 1), dotRoot.transform);
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
            dotRect.anchoredPosition = new Vector2(startX + spacing * i, 0f);

            Image dotImage = dot.AddComponent<Image>();
            dotImages[i] = dotImage;
        }
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHighlightedColor;
        colors.selectedColor = buttonHighlightedColor;
        colors.pressedColor = new Color(0.16f, 0.13f, 0.1f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, label.Length <= 1 ? 28 : 18, FontStyle.Bold, bodyColor);
        text.text = label;
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = Vector2.zero;
        AddOutline(text, new Vector2(1.2f, -1.2f));
        return button;
    }

    private void ShowPage(int page)
    {
        currentPage = (page + pages.Length) % pages.Length;
        TutorialPage data = pages[currentPage];

        titleText.text = data.Title;
        titleText.color = data.Accent;
        bodyText.text = data.Body;
        ApplyPageArt(GetPageArt(currentPage));

        for (int i = 0; i < dotImages.Length; i++)
        {
            dotImages[i].color = i == currentPage ? activeDotColor : inactiveDotColor;
            dotImages[i].rectTransform.sizeDelta = i == currentPage ? new Vector2(dotSize * 1.5f, dotSize) : new Vector2(dotSize, dotSize);
        }
    }

    private TutorialPageArt GetPageArt(int index)
    {
        EnsurePageArtArray();
        return pageArt[Mathf.Clamp(index, 0, pageArt.Length - 1)];
    }

    private void ApplyPageArt(TutorialPageArt art)
    {
        ApplyImage(vignetteBackgroundImage, art.BackgroundSprite);
        ApplySpriteImage(characterImage, characterRect, art.CharacterSprite, art.CharacterScale, art.CharacterPosition, art.FlipCharacter);
        ApplySpriteImage(supportImage, supportRect, art.SupportSprite, art.SupportScale, art.SupportPosition, false);
    }

    private static void ApplyImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.color = Color.white;
    }

    private static void ApplySpriteImage(Image image, RectTransform rect, Sprite sprite, float scale, Vector2 position, bool flipX)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
        image.color = Color.white;
        rect.anchoredPosition = position;
        rect.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);

        if (sprite == null)
        {
            rect.sizeDelta = Vector2.zero;
            return;
        }

        Rect spriteRect = sprite.rect;
        rect.sizeDelta = new Vector2(spriteRect.width * scale, spriteRect.height * scale);
    }

    private static Image CreateSpriteImage(string objectName, Transform parent, out RectTransform rect)
    {
        GameObject imageObject = CreateUiObject(objectName, parent);
        rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateVignetteBand(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject band = CreateUiObject(objectName, parent);
        RectTransform rect = band.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = band.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = resolvedFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void EnsurePageArtArray()
    {
        if (pageArt == null || pageArt.Length != PageCount)
        {
            pageArt = CreateDefaultPageArt();
        }

        for (int i = 0; i < pageArt.Length; i++)
        {
            if (pageArt[i] == null)
            {
                pageArt[i] = CreateDefaultPageArt(i);
            }
        }
    }

    private static TutorialPageArt[] CreateDefaultPageArt()
    {
        TutorialPageArt[] art = new TutorialPageArt[PageCount];
        for (int i = 0; i < art.Length; i++)
        {
            art[i] = CreateDefaultPageArt(i);
        }

        return art;
    }

    private static TutorialPageArt CreateDefaultPageArt(int index)
    {
        TutorialPageArt art = new TutorialPageArt();

        switch (index)
        {
            case 0:
                art.ApplyLayout(3.9f, 3.25f, new Vector2(-130f, -38f), new Vector2(126f, -46f), false);
                break;
            case 1:
                art.ApplyLayout(4.2f, 3.0f, new Vector2(-120f, -8f), new Vector2(118f, -42f), false);
                break;
            case 2:
                art.ApplyLayout(4.0f, 3.4f, new Vector2(-142f, -36f), new Vector2(92f, -30f), false);
                break;
            case 3:
                art.ApplyLayout(4.0f, 3.0f, new Vector2(-122f, -38f), new Vector2(118f, -24f), false);
                break;
        }

        return art;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill Existing Tutorial Art")]
    private void AutoFillExistingArt()
    {
        EnsurePageArtArray();
        bool changed = false;

        if (uiFont == null)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/PressStart2P-Regular.ttf");
            if (font != null)
            {
                uiFont = font;
                changed = true;
            }
        }

        for (int i = 0; i < pageArt.Length; i++)
        {
            Sprite backgroundSprite = LoadSprite(DefaultBackgroundSprites[i]);
            Sprite characterSprite = LoadSprite(DefaultCharacterSprites[i]);
            Sprite supportSprite = LoadSprite(DefaultSupportSprites[i]);
            changed |= pageArt[i].FillMissingSprites(backgroundSprite, characterSprite, supportSprite);
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
    }

    private static Sprite LoadSprite(SpritePath spritePath)
    {
        if (string.IsNullOrEmpty(spritePath.Path))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath.Path);
        if (sprite != null)
        {
            return sprite;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath.Path);
        int spriteIndex = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite candidate = assets[i] as Sprite;
            if (candidate == null)
            {
                continue;
            }

            if (spriteIndex == spritePath.Index)
            {
                return candidate;
            }

            spriteIndex++;
        }

        return null;
    }
#endif

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

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.layer = 5;
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void AddOutline(Text text, Vector2 distance)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = distance;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
