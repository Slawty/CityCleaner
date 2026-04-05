using UnityEngine;
public interface IInteractable
{
    public string Prompt { get; }
    void Interact(GameObject interactor);
    void InteractCanceled(GameObject interactor);
}