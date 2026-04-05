using UnityEngine;
using ArcadeVP;
using Unity.Cinemachine;

public class VehicleActivator : MonoBehaviour, IInteractable
{
    public string Prompt => "Enter (E)";
    public MonoBehaviour[] drivingScripts;
    public CinemachineCamera carCamera;
    public Transform playerExitPosition;
    GameObject currentInteractor;

    public void Interact(GameObject interactor)
    {
        Debug.Log("Vehicle inetracted");
        if (currentInteractor != null)
            return;

        currentInteractor = interactor;

        currentInteractor.SetActive(false);
        ActivateCar();
    }

    void ActivateCar()
    {
        foreach (var script in drivingScripts)
            script.enabled = true;

        carCamera.Priority = 20;
    }

    public void ExitCar()
    {
        foreach (var script in drivingScripts)
            script.enabled = false;

        currentInteractor.transform.position = playerExitPosition.position;
        currentInteractor.transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        currentInteractor.SetActive(true);
        carCamera.Priority = 0;
        currentInteractor = null;
    }

    public void InteractCanceled(GameObject interactor)
    {
    }
}
