using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    private const string StartSceneName = "Start";
    private const string LevelSelectSceneName = "SelectLevel";
    private const string Level1SceneName = "Level1";
    private const string Level2SceneName = "Level2";
    private const string Level3SceneName = "Level3";
    private const string TutorialSceneName = "Tutorial";
    private const string Level1ButtonName = "Level 1 Button";
    private const string Level2ButtonName = "Level 2 Button";
    private const string Level3ButtonName = "Level 3 Button";
    private const string TutorialButtonName = "Tutorial Button";
    private const string BackButtonName = "Back Button";

    private void Start()
    {
        EnsureLevelSelectLayout();
    }

    public void StartGame()
    {
        LoadScene(LevelSelectSceneName);
    }

    public void OpenLevel1()
    {
        LoadScene(Level1SceneName);
    }

    public void OpenLevel2()
    {
        LoadScene(Level2SceneName);
    }

    public void OpenLevel3()
    {
        LoadScene(Level3SceneName);
    }

    public void OpenTutorial()
    {
        LoadScene(TutorialSceneName);
    }

    public void BackToStart()
    {
        LoadScene(StartSceneName);
    }

    private static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void EnsureLevelSelectLayout()
    {
        if (SceneManager.GetActiveScene().name != LevelSelectSceneName)
        {
            return;
        }

        GameObject level2Button = GameObject.Find(Level2ButtonName);
        if (level2Button == null)
        {
            return;
        }

        GameObject level3Button = GameObject.Find(Level3ButtonName);
        if (level3Button == null)
        {
            level3Button = Instantiate(level2Button, level2Button.transform.parent);
            level3Button.name = Level3ButtonName;
        }

        ConfigureLevelButton(level3Button, "Level 3", OpenLevel3);

        GameObject tutorialButton = GameObject.Find(TutorialButtonName);
        if (tutorialButton == null)
        {
            tutorialButton = Instantiate(level2Button, level2Button.transform.parent);
            tutorialButton.name = TutorialButtonName;
        }

        ConfigureLevelButton(tutorialButton, "Tutorial", OpenTutorial);
        SetButtonY(Level1ButtonName, -92f);
        SetButtonY(Level2ButtonName, -146f);
        SetButtonY(Level3ButtonName, -200f);
        SetButtonY(TutorialButtonName, -254f);
        SetButtonY(BackButtonName, -308f);
    }

    private static void ConfigureLevelButton(GameObject buttonObject, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        Text text = buttonObject.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static void SetButtonY(string buttonName, float y)
    {
        GameObject buttonObject = GameObject.Find(buttonName);
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }
}
