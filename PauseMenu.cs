using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour
{
    private const string LevelSelectSceneName = "SelectLevel";
    private const string TutorialSceneName = "Tutorial";

    public static bool IsPaused { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Font uiFont;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1280f, 720f);
    [SerializeField] private int sortingOrder = 100;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(420f, 380f);
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color panelColor = new Color(0.14f, 0.12f, 0.1f, 0.92f);
    [SerializeField] private Color innerPanelColor = new Color(0.12f, 0.1f, 0.08f, 0.86f);
    [SerializeField] private Color goldBandColor = new Color(1f, 0.86f, 0.42f, 0.42f);
    [SerializeField] private Color greenBandColor = new Color(0.56f, 0.78f, 0.38f, 0.85f);

    [Header("Title")]
    [SerializeField] private int titleFontSize = 28;
    [SerializeField] private Color titleColor = new Color(1f, 0.86f, 0.42f, 1f);

    [Header("Buttons")]
    [SerializeField] private Vector2 buttonSize = new Vector2(200f, 44f);
    [SerializeField] private float buttonSpacing = 54f;
    [SerializeField] private int buttonFontSize = 20;
    [SerializeField] private Color buttonColor = new Color(0.22f, 0.18f, 0.14f, 1f);
    [SerializeField] private Color buttonHighlightedColor = new Color(0.32f, 0.26f, 0.19f, 1f);
    [SerializeField] private Color buttonPressedColor = new Color(0.16f, 0.13f, 0.1f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.94f, 0.88f, 0.72f, 1f);

    [Header("Volume")]
    [SerializeField] private string volumePreferenceKey = "MasterVolume";
    [SerializeField] private Vector2 sliderSize = new Vector2(240f, 24f);
    [SerializeField] private Color sliderBackgroundColor = new Color(0.18f, 0.15f, 0.12f, 1f);
    [SerializeField] private Color sliderFillColor = new Color(1f, 0.86f, 0.42f, 1f);
    [SerializeField] private Color labelColor = new Color(0.94f, 0.98f, 0.86f, 1f);

    private readonly List<Button> menuButtons = new List<Button>();
    private Canvas menuCanvas;
    private GameObject canvasObject;
    private GameObject firstSelectedButton;
    private GameObject selectedBeforePause;
    private Slider volumeSlider;
    private Font resolvedFont;
    private bool ownsPauseState;

    private void Start()
    {
        resolvedFont = ResolveFont();
        EnsureEventSystem();
        BuildMenuUi();
        ApplyStoredVolume();
        SetMenuVisible(false);
        Resume(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        if (IsPaused)
        {
            UpdateKeyboardSelection();
        }
    }

    private void OnDisable()
    {
        RestoreIfOwningPause();
    }

    private void OnDestroy()
    {
        RestoreIfOwningPause();
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        selectedBeforePause = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        IsPaused = true;
        ownsPauseState = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;
        SetMenuVisible(true);
        SelectFirstButton();
    }

    public void Resume()
    {
        Resume(true);
    }

    public void Restart()
    {
        Resume(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenLevelSelect()
    {
        Resume(false);
        SceneManager.LoadScene(LevelSelectSceneName);
    }

    public void OpenTutorial()
    {
        Resume(false);
        SceneManager.LoadScene(TutorialSceneName);
    }

    public void QuitGame()
    {
        Resume(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Resume(bool restoreSelection)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        ownsPauseState = false;
        SetMenuVisible(false);

        if (restoreSelection && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedBeforePause);
        }
    }

    private void RestoreIfOwningPause()
    {
        if (!ownsPauseState)
        {
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        ownsPauseState = false;
    }

    private void BuildMenuUi()
    {
        if (canvasObject != null)
        {
            return;
        }

        canvasObject = new GameObject("PauseMenuCanvas");
        canvasObject.layer = 5;

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        Stretch(canvasObject.GetComponent<RectTransform>());

        GameObject overlay = CreateUiObject("PauseOverlay", canvasObject.transform);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;
        Stretch(overlay.GetComponent<RectTransform>());

        GameObject panel = CreateUiObject("PausePanel", overlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        GameObject innerPanel = CreateUiObject("PauseInnerPanel", panel.transform);
        RectTransform innerRect = innerPanel.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.sizeDelta = panelSize - new Vector2(34f, 34f);
        innerRect.anchoredPosition = Vector2.zero;
        Image innerImage = innerPanel.AddComponent<Image>();
        innerImage.color = innerPanelColor;

        CreateBand("PauseHeaderBand", innerPanel.transform, new Vector2(panelSize.x - 70f, 6f), new Vector2(0f, 158f), goldBandColor);
        CreateBand("PauseFooterBand", innerPanel.transform, new Vector2(panelSize.x - 88f, 5f), new Vector2(0f, -158f), greenBandColor);

        Text title = CreateText("PauseTitle", innerPanel.transform, titleFontSize, FontStyle.Bold, titleColor);
        title.text = "PAUSED";
        title.rectTransform.sizeDelta = new Vector2(330f, 42f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 126f);
        AddOutline(title, new Vector2(1.5f, -1.5f));

        string[] labels = { "Resume", "Restart", "Level Select", "Tutorial", "Quit" };
        UnityEngine.Events.UnityAction[] actions = { Resume, Restart, OpenLevelSelect, OpenTutorial, QuitGame };
        float firstY = 73f;

        for (int i = 0; i < labels.Length; i++)
        {
            Button button = CreateButton(labels[i] + " Button", innerPanel.transform, labels[i], new Vector2(0f, firstY - buttonSpacing * i));
            button.onClick.AddListener(actions[i]);
            menuButtons.Add(button);

            if (i == 0)
            {
                firstSelectedButton = button.gameObject;
            }
        }

        BuildVolumeControl(innerPanel.transform);
        ConfigureButtonNavigation();
    }

    private void BuildVolumeControl(Transform parent)
    {
        GameObject row = CreateUiObject("VolumeRow", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(330f, 34f);
        rowRect.anchoredPosition = new Vector2(0f, -148f);

        Text label = CreateText("VolumeLabel", row.transform, 18, FontStyle.Bold, labelColor);
        label.text = "Volume";
        label.alignment = TextAnchor.MiddleLeft;
        label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(82f, 28f);
        label.rectTransform.anchoredPosition = new Vector2(41f, 0f);
        AddOutline(label, new Vector2(1.2f, -1.2f));

        GameObject sliderObject = CreateUiObject("VolumeSlider", row.transform);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.sizeDelta = sliderSize;
        sliderRect.anchoredPosition = new Vector2(-sliderSize.x * 0.5f, 0f);

        volumeSlider = sliderObject.AddComponent<Slider>();
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        Stretch(backgroundRect);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = sliderBackgroundColor;

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 5f);
        fillAreaRect.offsetMax = new Vector2(-4f, -5f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = sliderFillColor;

        GameObject handle = CreateUiObject("Handle", sliderObject.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 26f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = buttonTextColor;

        volumeSlider.fillRect = fillRect;
        volumeSlider.handleRect = handleRect;
        volumeSlider.targetGraphic = handleImage;
        volumeSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = buttonSize;
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHighlightedColor;
        colors.selectedColor = buttonHighlightedColor;
        colors.pressedColor = buttonPressedColor;
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, buttonFontSize, FontStyle.Bold, buttonTextColor);
        text.text = label;
        text.rectTransform.sizeDelta = buttonSize;
        text.rectTransform.anchoredPosition = Vector2.zero;
        AddOutline(text, new Vector2(1.2f, -1.2f));
        return button;
    }

    private void ConfigureButtonNavigation()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            Navigation navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = menuButtons[(i - 1 + menuButtons.Count) % menuButtons.Count],
                selectOnDown = menuButtons[(i + 1) % menuButtons.Count]
            };

            menuButtons[i].navigation = navigation;
        }
    }

    private void UpdateKeyboardSelection()
    {
        if (EventSystem.current == null || menuButtons.Count == 0)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        int index = menuButtons.FindIndex(button => button != null && button.gameObject == selected);

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            int nextIndex = index <= 0 ? menuButtons.Count - 1 : index - 1;
            EventSystem.current.SetSelectedGameObject(menuButtons[nextIndex].gameObject);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            int nextIndex = index < 0 || index >= menuButtons.Count - 1 ? 0 : index + 1;
            EventSystem.current.SetSelectedGameObject(menuButtons[nextIndex].gameObject);
        }
        else if (selected == null)
        {
            SelectFirstButton();
        }
    }

    private void SelectFirstButton()
    {
        if (EventSystem.current != null && firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(visible);
        }
    }

    private void ApplyStoredVolume()
    {
        float volume = PlayerPrefs.GetFloat(volumePreferenceKey, 1f);
        AudioListener.volume = Mathf.Clamp01(volume);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        }
    }

    private void SetMasterVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = clampedVolume;
        PlayerPrefs.SetFloat(volumePreferenceKey, clampedVolume);
        PlayerPrefs.Save();
    }

    private Font ResolveFont()
    {
        return uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.layer = 5;
        uiObject.AddComponent<RectTransform>();
        return uiObject;
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
        image.raycastTarget = false;
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
