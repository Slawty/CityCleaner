using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartController : MonoBehaviour
{
    [SerializeField] string managerSceneName = "Manager Scene";
    [SerializeField] string levelSceneName = "Level_01";
    [SerializeField] SettingsMenuButton startButton;
    [SerializeField] SettingsMenuButton exitButton;
    [SerializeField] CanvasGroup backgroundGroup;
    [SerializeField] TMP_Text loadingText;
    [SerializeField] float levelLoadDelaySeconds = 0.25f;
    [SerializeField] float backgroundFadeOutDuration = 0.6f;

    bool managerSceneReady;
    bool isLoadingLevel;
    CancellationTokenSource loadCts;

    void Awake()
    {
        GameSettings.Load();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (startButton == null)
            throw new InvalidOperationException($"{nameof(GameStartController)} on {name}: {nameof(startButton)} is not assigned.");

        if (backgroundGroup == null)
            throw new InvalidOperationException($"{nameof(GameStartController)} on {name}: {nameof(backgroundGroup)} is not assigned.");

        startButton.SetLabel("Start");
        startButton.SetClickHandler(StartGame);
        startButton.Button.interactable = false;

        if (exitButton == null)
            throw new InvalidOperationException($"{nameof(GameStartController)} on {name}: {nameof(exitButton)} is not assigned.");

        exitButton.SetClickHandler(GameRestart.QuitApplication);

        if (loadingText != null)
            loadingText.gameObject.SetActive(false);
    }

    void Start()
    {
        LoadManagerSceneAsync(destroyCancellationToken).Forget();
    }

    void OnDestroy()
    {
        if (loadCts == null)
            return;

        loadCts.Cancel();
        loadCts.Dispose();
        loadCts = null;
    }

    async UniTaskVoid LoadManagerSceneAsync(CancellationToken cancellationToken)
    {
        await LoadSceneAdditiveAsync(managerSceneName, cancellationToken);

        GameSettings.ApplyAll();

        if (Managers.IsInitialized)
            Managers.UI.SetHudVisible(false);

        managerSceneReady = true;
        startButton.Button.interactable = true;
    }

    void StartGame()
    {
        if (!managerSceneReady || isLoadingLevel)
            return;

        isLoadingLevel = true;
        ShowLoadingState();

        loadCts = new CancellationTokenSource();
        StartLevelAsync(loadCts.Token).Forget();
    }

    void ShowLoadingState()
    {
        startButton.Button.interactable = false;
        exitButton.Button.interactable = false;
        startButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        if (loadingText != null)
            loadingText.gameObject.SetActive(true);

        if (backgroundGroup != null)
        {
            backgroundGroup.interactable = false;
            backgroundGroup.blocksRaycasts = false;
        }

        Canvas.ForceUpdateCanvases();
    }

    async UniTaskVoid StartLevelAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (levelLoadDelaySeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(levelLoadDelaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);

            await LoadSceneAdditiveAsync(levelSceneName, cancellationToken);

            Scene levelScene = SceneManager.GetSceneByName(levelSceneName);
            if (!levelScene.IsValid())
                throw new InvalidOperationException($"{nameof(GameStartController)} on {name}: loaded scene '{levelSceneName}' is not valid.");

            SceneManager.SetActiveScene(levelScene);

            await FadeCanvasGroupAsync(backgroundGroup, 0f, backgroundFadeOutDuration, cancellationToken);

            Scene startScene = gameObject.scene;
            await SceneManager.UnloadSceneAsync(startScene).ToUniTask(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    async UniTask LoadSceneAdditiveAsync(string sceneName, CancellationToken cancellationToken)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOperation == null)
            throw new InvalidOperationException($"{nameof(GameStartController)} on {name}: failed to load scene '{sceneName}'. Add it to Build Settings.");

        while (!loadOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    static async UniTask FadeCanvasGroupAsync(CanvasGroup canvasGroup, float targetAlpha, float durationSeconds, CancellationToken cancellationToken)
    {
        if (canvasGroup == null)
            throw new InvalidOperationException($"{nameof(FadeCanvasGroupAsync)}: {nameof(canvasGroup)} is not assigned.");

        float startAlpha = canvasGroup.alpha;
        if (durationSeconds <= 0f)
        {
            SetCanvasGroupAlpha(canvasGroup, targetAlpha);
            return;
        }

        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.Clamp01(elapsed / durationSeconds);
            SetCanvasGroupAlpha(canvasGroup, Mathf.Lerp(startAlpha, targetAlpha, blend));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        SetCanvasGroupAlpha(canvasGroup, targetAlpha);
    }

    static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        canvasGroup.alpha = alpha;
        bool visible = alpha > 0.01f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
