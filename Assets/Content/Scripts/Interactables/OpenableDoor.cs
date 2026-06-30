using System;
using DG.Tweening;
using UnityEngine;

public class OpenableDoor : MonoBehaviour
{
    [Serializable]
    class DoorPanel
    {
        public Transform door;
        public float closedAngle;
        public float openAngle;
    }

    [SerializeField] DoorPanel primaryDoor = new();
    [SerializeField] DoorPanel secondaryDoor = new();
    [SerializeField] float openDuration = 0.5f;

    Quaternion primaryClosedRotation;
    Quaternion primaryOpenRotation;
    Quaternion secondaryClosedRotation;
    Quaternion secondaryOpenRotation;
    bool isOpen;

    void Awake()
    {
        CacheDoorRotations(primaryDoor, ref primaryClosedRotation, ref primaryOpenRotation);
        CacheDoorRotations(secondaryDoor, ref secondaryClosedRotation, ref secondaryOpenRotation);
    }

    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;
        AnimateDoor(primaryDoor.door, primaryOpenRotation);
        AnimateDoor(secondaryDoor.door, secondaryOpenRotation);
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        AnimateDoor(primaryDoor.door, primaryClosedRotation);
        AnimateDoor(secondaryDoor.door, secondaryClosedRotation);
    }

    static void CacheDoorRotations(DoorPanel panel, ref Quaternion closedRotation, ref Quaternion openRotation)
    {
        if (panel.door == null)
            return;

        closedRotation = Quaternion.Euler(0f, panel.closedAngle, 0f);
        openRotation = Quaternion.Euler(0f, panel.openAngle, 0f);
    }

    void AnimateDoor(Transform door, Quaternion targetRotation)
    {
        if (door == null)
            return;

        door.DOLocalRotateQuaternion(targetRotation, openDuration);
    }
}
