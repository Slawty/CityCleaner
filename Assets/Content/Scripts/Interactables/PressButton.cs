using UnityEngine;
using System;
using UnityEngine.Events;

public class PressButton : MonoBehaviour, IInteractable
{
    public enum ButtonState { Available, Unavailable, InUse }

    public string Prompt => "Press Button";

    public Color AvailableColor = Color.green;
    public Color UnavailableColor = Color.red;
    public Color InUseColor = Color.yellow;

    public event UnityAction OnButtonPressed;
    public event UnityAction OnButtonReleased;
    public Renderer buttonRenderer;

    private ButtonState currentState = ButtonState.Unavailable;
    public ButtonState CurrentState => currentState;

    private Material buttonMaterial;

    void Awake()
    {
        buttonMaterial = buttonRenderer.material;
        UpdateColor();
    }

    public void SetState(ButtonState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (currentState == ButtonState.Available)
        {
            buttonMaterial.SetColor("_BaseColor", AvailableColor);
        }
        else if (currentState == ButtonState.Unavailable)
        {
            buttonMaterial.SetColor("_BaseColor", UnavailableColor);
        }
        else if (currentState == ButtonState.InUse)
        {
            buttonMaterial.SetColor("_BaseColor", InUseColor);
        }
    }

    public void Interact(GameObject interactor)
    {
        OnButtonPressed?.Invoke();
    }

    public void InteractReleased(GameObject interactor)
    {

    }
}