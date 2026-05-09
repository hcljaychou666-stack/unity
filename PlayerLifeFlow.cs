using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLifeFlow : MonoBehaviour
{
    public const string LifeBroadcastSceneName = "LifeBroadcast";
    private const string FallbackGameplaySceneName = "Level1";
    private const string GameOverSceneName = "End";

    private static PlayerLifeFlow instance;

    private int maxLives = 3;
    private int currentLives = 3;
    private int previousLives = 3;
    private string returnSceneName = FallbackGameplaySceneName;
    private string activeGameplaySceneName = string.Empty;
    private bool expectingReturnToGameplay;
    private bool resetLivesOnReturn;
    private bool transitionInProgress;
    private int sceneLoadSerial;
    private int lastGameplayRegistrationSerial = -1;
    private readonly Dictionary<string, CheckpointData> sceneCheckpoints = new();

    private sealed class CheckpointData
    {
        public string Id;
        public Vector3 Position;
    }

    public static PlayerLifeFlow Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public int CurrentLives => currentLives;
    public int PreviousLives => previousLives;
    public int MaxLives => maxLives;
    public bool IsGameOver => currentLives <= 0;
    public string ReturnSceneName => string.IsNullOrWhiteSpace(returnSceneName) ? FallbackGameplaySceneName : returnSceneName;
    public string NextSceneAfterBroadcast => IsGameOver ? GameOverSceneName : ReturnSceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        PlayerLifeFlow existing = FindObjectOfType<PlayerLifeFlow>();

        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject root = new GameObject(nameof(PlayerLifeFlow));
        instance = root.AddComponent<PlayerLifeFlow>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneLoadSerial++;
    }

    public void RegisterGameplayScene(string sceneName, int configuredLives)
    {
        int sanitizedLives = Mathf.Max(1, configuredLives);
        bool returningFromLifeScene = expectingReturnToGameplay && sceneName == returnSceneName;
        bool duplicateSceneRegistration = lastGameplayRegistrationSerial == sceneLoadSerial
            && sceneName == activeGameplaySceneName;

        maxLives = sanitizedLives;

        if (!duplicateSceneRegistration)
        {
            if (!returningFromLifeScene)
            {
                ClearCheckpoint(sceneName);
                currentLives = maxLives;
                previousLives = maxLives;
            }
            else if (resetLivesOnReturn || currentLives <= 0)
            {
                currentLives = maxLives;
            }
        }
        else
        {
            currentLives = Mathf.Clamp(currentLives, 0, maxLives);
            previousLives = Mathf.Clamp(previousLives, 0, maxLives);
        }

        activeGameplaySceneName = sceneName;
        lastGameplayRegistrationSerial = sceneLoadSerial;
        returnSceneName = sceneName;
        expectingReturnToGameplay = false;
        resetLivesOnReturn = false;
        transitionInProgress = false;
    }

    public bool TryBeginDeathTransition(string sceneName, int configuredLives)
    {
        if (transitionInProgress)
        {
            return true;
        }

        int sanitizedLives = Mathf.Max(1, configuredLives);
        maxLives = sanitizedLives;

        if (currentLives <= 0
            || currentLives > maxLives
            || string.IsNullOrWhiteSpace(returnSceneName)
            || (!expectingReturnToGameplay && returnSceneName != sceneName))
        {
            currentLives = maxLives;
        }

        previousLives = Mathf.Clamp(currentLives, 0, maxLives);
        currentLives = Mathf.Max(0, currentLives - 1);
        returnSceneName = sceneName;
        expectingReturnToGameplay = true;
        resetLivesOnReturn = currentLives <= 0;
        transitionInProgress = true;

        if (!CanLoadLifeBroadcastScene())
        {
            transitionInProgress = false;
            return false;
        }

        SceneManager.LoadScene(LifeBroadcastSceneName);
        return true;
    }

    public void CompleteBroadcast()
    {
        transitionInProgress = false;

        if (IsGameOver)
        {
            ClearAllCheckpoints();
            expectingReturnToGameplay = false;
            resetLivesOnReturn = false;
            activeGameplaySceneName = string.Empty;
            lastGameplayRegistrationSerial = -1;
            currentLives = maxLives;
            previousLives = maxLives;
            SceneManager.LoadScene(GameOverSceneName);
            return;
        }

        SceneManager.LoadScene(ReturnSceneName);
    }

    public void SetCheckpoint(string sceneName, string checkpointId, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(checkpointId))
        {
            return;
        }

        sceneCheckpoints[sceneName] = new CheckpointData
        {
            Id = checkpointId,
            Position = position
        };
    }

    public bool TryGetCheckpointPosition(string sceneName, out Vector3 position)
    {
        if (!string.IsNullOrWhiteSpace(sceneName)
            && sceneCheckpoints.TryGetValue(sceneName, out CheckpointData checkpoint))
        {
            position = checkpoint.Position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    public bool IsCheckpointActive(string sceneName, string checkpointId)
    {
        return !string.IsNullOrWhiteSpace(sceneName)
            && !string.IsNullOrWhiteSpace(checkpointId)
            && sceneCheckpoints.TryGetValue(sceneName, out CheckpointData checkpoint)
            && checkpoint.Id == checkpointId;
    }

    public void ClearCheckpoint(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        sceneCheckpoints.Remove(sceneName);
    }

    private void ClearAllCheckpoints()
    {
        sceneCheckpoints.Clear();
    }

    private static bool CanLoadLifeBroadcastScene()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == LifeBroadcastSceneName)
            {
                return true;
            }
        }

        Debug.LogError($"Scene '{LifeBroadcastSceneName}' is not in Build Settings.");
        return false;
    }
}
