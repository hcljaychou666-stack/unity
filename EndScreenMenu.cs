using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreenMenu : MonoBehaviour
{
    private const string LevelSelectSceneName = "SelectLevel";
    private const string TutorialSceneName = "Tutorial";
    private const string SummaryPanelName = "RunSummaryPanel";
    private const string TutorialButtonName = "TutorialButton";

    [SerializeField] private Font uiFont;

    private void Start()
    {
        FixGameOverTitle();
        BuildRunSummaryPanel();
        EnsureTutorialButton();
    }

    public void ReloadGame()
    {
        Time.timeScale = 1f;
        AllControl.GameManager.Instance.BeginNewRun();
        SceneManager.LoadScene(LevelSelectSceneName);
    }

    public void OpenTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(TutorialSceneName);
    }

    private void BuildRunSummaryPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        ConfigureCanvas(canvas);

        Font font = ResolveFont();
        AllControl.GameManager.RunSummary summary = AllControl.GameManager.Instance.SubmitRunToLeaderboard();
        int[] leaderboardScores = AllControl.GameManager.Instance.GetLeaderboard();

        GameObject panel = CreateUiObject(SummaryPanelName, canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 300f);
        panelRect.anchoredPosition = new Vector2(32f, 32f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.1f, 0.08f, 0.86f);
        panelImage.raycastTarget = false;

        Text title = CreateText("RunSummaryTitle", panel.transform, font, 24, FontStyle.Bold, new Color(1f, 0.86f, 0.42f, 1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 114f);
        title.rectTransform.sizeDelta = new Vector2(360f, 34f);
        title.text = "RUN SUMMARY";

        Text stats = CreateText("RunStats", panel.transform, font, 19, FontStyle.Bold, new Color(0.94f, 0.98f, 0.86f, 1f));
        stats.alignment = TextAnchor.UpperLeft;
        stats.lineSpacing = 1.05f;
        stats.rectTransform.anchoredPosition = new Vector2(0f, 54f);
        stats.rectTransform.sizeDelta = new Vector2(330f, 94f);
        stats.text = BuildSummaryText(summary);

        CreateBand("SummaryDivider", panel.transform, new Vector2(360f, 2f), new Vector2(0f, -24f), new Color(1f, 0.86f, 0.42f, 0.42f));

        Text rankingTitle = CreateText("CollectionRankingTitle", panel.transform, font, 19, FontStyle.Bold, new Color(1f, 0.86f, 0.42f, 1f));
        rankingTitle.rectTransform.anchoredPosition = new Vector2(0f, -48f);
        rankingTitle.rectTransform.sizeDelta = new Vector2(360f, 28f);
        rankingTitle.text = "COLLECTION RANKING";

        Text ranking = CreateText("RankingList", panel.transform, font, 17, FontStyle.Normal, new Color(0.9f, 0.88f, 0.76f, 1f));
        ranking.alignment = TextAnchor.UpperCenter;
        ranking.lineSpacing = 1.02f;
        ranking.rectTransform.anchoredPosition = new Vector2(0f, -104f);
        ranking.rectTransform.sizeDelta = new Vector2(360f, 82f);
        ranking.text = BuildLeaderboardText(leaderboardScores);
    }

    private void EnsureTutorialButton()
    {
        if (GameObject.Find(TutorialButtonName) != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        ConfigureCanvas(canvas);

        Button tutorialButton = CreateMenuButton(TutorialButtonName, canvas.transform, "Tutorial", new Vector2(0f, -232f));
        tutorialButton.onClick.AddListener(OpenTutorial);
    }

    private static string BuildSummaryText(AllControl.GameManager.RunSummary summary)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Best cherries: {summary.BestCherries}");
        builder.AppendLine($"Deaths: {summary.Deaths}");
        builder.AppendLine($"Time: {FormatTime(summary.TimeSeconds)}");
        builder.AppendLine($"Rank: {FormatRank(summary.Rank)}");
        return builder.ToString();
    }

    private static string BuildLeaderboardText(int[] leaderboardScores)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < leaderboardScores.Length; i++)
        {
            int score = leaderboardScores[i];
            string scoreText = score > 0 ? $"{score} cherries" : "--";
            builder.AppendLine($"{i + 1}. {scoreText}");
        }

        return builder.ToString();
    }

    private void FixGameOverTitle()
    {
        Text[] texts = FindObjectsOfType<Text>();
        foreach (Text text in texts)
        {
            if (text != null && text.text == "GAEM OVER")
            {
                text.text = "GAME OVER";
            }
        }
    }

    private Font ResolveFont()
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("EndScreenCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null && rect.localScale == Vector3.zero)
        {
            rect.localScale = Vector3.one;
        }
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

    private Button CreateMenuButton(string objectName, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160f, 30f);
        rect.localScale = new Vector3(1.5126f, 1.5126f, 1f);
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.18f, 0.14f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.22f, 0.18f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.26f, 0.19f, 1f);
        colors.selectedColor = new Color(0.32f, 0.26f, 0.19f, 1f);
        colors.pressedColor = new Color(0.16f, 0.13f, 0.1f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, ResolveFont(), 16, FontStyle.Bold, new Color(0.94f, 0.88f, 0.72f, 1f));
        text.text = label;
        text.rectTransform.sizeDelta = new Vector2(160f, 30f);
        text.rectTransform.anchoredPosition = Vector2.zero;
        AddOutline(text, new Vector2(1.2f, -1.2f));
        return button;
    }

    private static void AddOutline(Text text, Vector2 distance)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = distance;
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private static string FormatRank(int rank)
    {
        return rank > 0 ? $"#{rank}" : "Unranked";
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.layer = 5;
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }
}
