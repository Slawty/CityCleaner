using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class Player : MonoBehaviour
{
    public SimplePlayerMovement movement;
    public MouseLook mouseLook;
    public CinemachineCamera playerCamera;

    void OnEnable()
    {
        SetPlayerActive(true);
    }

    void OnDisable()
    {
        SetPlayerActive(false);
    }

    public void SetPlayerActive(bool active)
    {
        movement.enabled = active;
        mouseLook.enabled = active;
        playerCamera.Priority = active ? 10 : 0;
    }
}
