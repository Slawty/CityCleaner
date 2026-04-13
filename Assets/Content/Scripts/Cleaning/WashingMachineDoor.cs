using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;

public class WashingMachineDoor : MonoBehaviour, IInteractable
{
    public UnityAction OnDoorOpened;
    public UnityAction OnDoorClosed;
    public Transform Door;
    public string Prompt => prompt;
    string prompt;
    bool isClosed = true;
    float openAngle = 150f;
    Quaternion closedRot;
    Quaternion openRot;

    void Start()
    {
        closedRot = Door.localRotation;
        openRot = Quaternion.Euler(0, openAngle, 0) * closedRot;
        UpdatePrompt();
    }

    public void Interact(GameObject interactor)
    {
        if (isClosed)
            OpenDoor();
        else
            CloseDoor();
    }

    public void InteractReleased(GameObject interactor)
    {

    }


    public void OpenDoor()
    {
        Door.DOLocalRotateQuaternion(openRot, 0.25f);
        isClosed = false;
        UpdatePrompt();
        OnDoorOpened?.Invoke();
    }

    void CloseDoor()
    {
        Door.DOLocalRotateQuaternion(closedRot, 0.25f);
        isClosed = true;
        UpdatePrompt();
        OnDoorClosed?.Invoke();
    }

    void UpdatePrompt()
    {
        prompt = isClosed ? "Open" : "Close";
        // Managers.UI.ShowInteractText(prompt);
    }


}
