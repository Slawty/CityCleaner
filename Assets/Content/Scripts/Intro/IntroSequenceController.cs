using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public class IntroSequenceController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool useIntro = true;
    [SerializeField] string startSceneName = "Start Scene";

    [Header("Camera")]
    [SerializeField] CinemachineCamera introCamera;
    [SerializeField] Transform cameraRig;
    [SerializeField] Transform endPosition;

    [Header("Timing")]
    [SerializeField] float holdDuration = 1.5f;
    [SerializeField] float moveDuration = 4f;
    [SerializeField] int introCameraPriority = 26;

    [Header("Tween")]
    [SerializeField] Ease moveEase = Ease.InOutSine;

    Sequence activeSequence;
    bool introFinished;
    bool introStarted;

    public static IntroSequenceController Instance { get; private set; }
    public bool UseIntro => useIntro;

    void Awake()
    {
        Instance = this;

        if (!useIntro)
        {
            introFinished = true;
            DisableIntroCamera();
            return;
        }
    }

    void Start()
    {
        if (!useIntro)
            return;

        BeginIntroWhenReadyAsync(destroyCancellationToken).Forget();
    }

    void OnDestroy()
    {
        activeSequence?.Kill();

        if (Instance == this)
            Instance = null;

        if (!introFinished && Managers.IsInitialized)
            ReleaseGameplay();
    }

    async UniTaskVoid BeginIntroWhenReadyAsync(CancellationToken cancellationToken)
    {
        await WaitForStartSceneToUnloadAsync(cancellationToken);
        BeginIntro();
    }

    async UniTask WaitForStartSceneToUnloadAsync(CancellationToken cancellationToken)
    {
        Scene startScene = SceneManager.GetSceneByName(startSceneName);
        if (!startScene.IsValid() || !startScene.isLoaded)
            return;

        while (startScene.isLoaded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            startScene = SceneManager.GetSceneByName(startSceneName);
            if (!startScene.IsValid())
                return;
        }
    }

    void BeginIntro()
    {
        if (!useIntro || introStarted || introFinished)
            return;

        introStarted = true;
        LockGameplayForIntro();
        PlayIntro();
    }

    void DisableIntroCamera()
    {
        if (introCamera != null)
            introCamera.Priority = 0;
    }

    void LockGameplayForIntro()
    {
        Managers.UI.SetHudVisible(false);
        Managers.Input.BlockInteraction(this);

        GameObject playerObject = Managers.Player.gameObject;
        if (playerObject.activeSelf)
            playerObject.SetActive(false);

        if (introCamera != null)
            introCamera.Priority = introCameraPriority;
    }

    void PlayIntro()
    {
        IntroCameraRigMover mover = new IntroCameraRigMover(cameraRig, endPosition, holdDuration, moveDuration, moveEase);
        activeSequence = mover.Play(SyncPlayerToEndOrientation);
        activeSequence.OnComplete(ReleaseGameplay);
    }

    void SyncPlayerToEndOrientation()
    {
        Vector3 euler = endPosition.eulerAngles;
        Managers.Player.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        float pitch = euler.x;
        if (pitch > 180f)
            pitch -= 360f;

        Managers.Player.mouseLook.ResetLook(pitch);
    }

    void ReleaseGameplay()
    {
        if (introFinished)
            return;

        introFinished = true;
        DisableIntroCamera();

        Managers.Player.gameObject.SetActive(true);
        Managers.Player.SetPlayerActive(true);
        Managers.Tutorial.StartGameplay();

        Managers.UI.SetHudVisible(true);
        Managers.Input.UnblockInteraction(this);
    }
}
