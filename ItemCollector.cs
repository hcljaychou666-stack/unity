using UnityEngine;
using UnityEngine.UI;
using static AllControl;

public class ItemCollector : MonoBehaviour
{
    private const string CounterObjectName = "CherryCounterText";
    private const string CounterCanvasName = "GameplayHUD";

    private int cherries;

    [SerializeField] private Text cherriesText;
    [SerializeField] private AudioSource collectSoundEffect;

    [Header("Collect Effects")]
    [SerializeField] private bool enableCollectEffects = true;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem cherryCollectParticles;

    private void Start()
    {
        GameManager.Instance.BeginRunIfNeeded();
        cherries = GameManager.Instance.score;
        EnsureCounterText();
        RefreshCherriesText();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Cherry"))
        {
            return;
        }

        if (collectSoundEffect != null)
        {
            collectSoundEffect.Play();
        }

        PlayCollectEffect(collision.transform);
        Destroy(collision.gameObject);
        GameManager.Instance.AddScore(1);
        cherries = GameManager.Instance.score;
        RefreshCherriesText();
    }

    private void RefreshCherriesText()
    {
        if (cherriesText != null)
        {
            cherriesText.text = "Cherries: " + cherries;
        }
    }

    private void EnsureCounterText()
    {
        if (cherriesText == null)
        {
            cherriesText = FindExistingCounterText();
        }

        if (cherriesText == null)
        {
            cherriesText = CreateCounterText();
        }

        if (cherriesText != null)
        {
            ConfigureCounterText(cherriesText);
        }
    }

    private static Text FindExistingCounterText()
    {
        GameObject namedCounter = GameObject.Find(CounterObjectName);
        if (namedCounter != null && namedCounter.TryGetComponent(out Text namedText))
        {
            return namedText;
        }

        Text[] texts = FindObjectsOfType<Text>();
        foreach (Text text in texts)
        {
            if (text != null && text.name.Contains("Text"))
            {
                return text;
            }
        }

        return null;
    }

    private static Text CreateCounterText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(CounterCanvasName);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null && canvasRect.localScale == Vector3.zero)
        {
            canvasRect.localScale = Vector3.one;
        }

        GameObject counterObject = new GameObject(CounterObjectName);
        counterObject.transform.SetParent(canvas.transform, false);
        return counterObject.AddComponent<Text>();
    }

    private static void ConfigureCounterText(Text text)
    {
        text.gameObject.name = CounterObjectName;
        text.raycastTarget = false;
        text.color = new Color(1f, 0.95f, 0.62f, 1f);
        text.fontSize = 24;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(240f, 42f);
            rect.localScale = Vector3.one;
        }

        Canvas parentCanvas = text.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (canvasRect != null && canvasRect.localScale == Vector3.zero)
            {
                canvasRect.localScale = Vector3.one;
            }

            CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void PlayCollectEffect(Transform collectTarget)
    {
        if (!enableCollectEffects || cherryCollectParticles == null)
        {
            return;
        }

        PixelEffectSpawner spawner = effectSpawner != null ? effectSpawner : GetComponent<PixelEffectSpawner>();
        if (spawner != null)
        {
            spawner.PlayAtTransform(cherryCollectParticles, collectTarget);
        }
    }
}
