using Rhinox.Perceptor;
using Rhinox.XR.Grapple.It;
using UnityEngine;

public class ButtonTest : MonoBehaviour
{
    [SerializeField] private GRPLButtonInteractable _button;

    private void OnEnable()
    {
        if (!_button)
            return;
        OnDisable();

        _button.ButtonDown += OnButtonDown;
        _button.ButtonUp += OnButtonUp;
        _button.ButtonPressed += OnButtonPressed;
    }

    private void OnDisable()
    {
        if (!_button)
            return;

        _button.ButtonDown -= OnButtonDown;
        _button.ButtonUp -= OnButtonUp;
        _button.ButtonPressed -= OnButtonPressed;
    }
    
    private void OnButtonUp(GRPLButtonInteractable obj)
    {
        PLog.Info<GRPLITLogger>("Button up.");
    }

    private void OnButtonDown(GRPLButtonInteractable obj)
    {
        PLog.Info<GRPLITLogger>("Button down.");
    }

    private void OnButtonPressed(GRPLButtonInteractable obj)
    {
        PLog.Info<GRPLITLogger>("Button pressed.");
    }


}