using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class IntroSequenceController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool useIntro = true;

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
    bool playerDeactivatedForIntro;

    void Awake()
    {
        ResolveReferences();

        if (!useIntro)
        {
            introFinished = true;
            DisableIntroCamera();
            return;
        }

        LockGameplayForIntro();
    }

    void Start()
    {
        if (!useIntro)
            return;

        if (!HasRequiredReferences())
        {
            Debug.LogError($"{nameof(IntroSequenceController)} on {name}: missing intro references.", this);
            ReleaseGameplay();
            return;
        }

        PlayIntro();
    }

    void OnDestroy()
    {
        activeSequence?.Kill();

        if (!introFinished)
            ReleaseGameplay();
    }

    void ResolveReferences()
    {
        if (introCamera == null)
            introCamera = transform.Find("CM_Intro_Overview")?.GetComponent<CinemachineCamera>();
        if (cameraRig == null)
            cameraRig = transform.Find("Intro_Overview_Pos");
        if (endPosition == null)
            endPosition = transform.Find("Intro_Target_Pos");
    }

    bool HasRequiredReferences()
    {
        return introCamera != null && cameraRig != null && endPosition != null;
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
        {
            playerObject.SetActive(false);
            playerDeactivatedForIntro = true;
        }

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

        if (playerDeactivatedForIntro)
            Managers.Player.gameObject.SetActive(true);
        else
            Managers.Player.SetPlayerActive(true);

        Managers.UI.SetHudVisible(true);
        Managers.Input.UnblockInteraction(this);
    }
}
