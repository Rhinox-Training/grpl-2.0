using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

/// <summary>
/// This component can be used to wrap a GRPL Interactable for unity events in editor.
/// </summary>
public class GRPLTwoWayLeverUnityWrapper : MonoBehaviour
{
    [SerializeField] private GRPLTwoWayLever _interactable;

    [SerializeField] private UnityEvent _leverForwardActivated;
    [SerializeField] private UnityEvent _leverForwardStopped;
    [SerializeField] private UnityEvent _leverBackwardActivated;
    [SerializeField] private UnityEvent _leverBackwardStopped;

    // Unity event props
    public UnityEvent OnLeverForwardActivated => _leverForwardActivated;
    public UnityEvent OnLeverForwardStopped => _leverForwardStopped;
    public UnityEvent OnLeverBackwardActivated => _leverBackwardActivated;
    public UnityEvent OnLeverBackwardStopped => _leverBackwardStopped;

    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable, null, "[GRPLTwoWayLeverUnityWrapper], Lever was not set!");
    }

    private void OnEnable()
    {

        _interactable.LeverForwardActivated += OnLeverInteractableForwardActivated;
        _interactable.LeverForwardStopped += OnLeverInteractableForwardStopped;
        _interactable.LeverBackwardActivated += OnLeverInteractableBackwardActivated;
        _interactable.LeverBackwardStopped += OnLeverInteractableBackwardStopped;
    }

    private void OnDisable()
    {
        _interactable.LeverForwardActivated -= OnLeverInteractableForwardActivated;
        _interactable.LeverForwardStopped -= OnLeverInteractableForwardStopped;
        _interactable.LeverBackwardActivated -= OnLeverInteractableBackwardActivated;
        _interactable.LeverBackwardStopped -= OnLeverInteractableBackwardStopped;
    }

    private void OnLeverInteractableForwardActivated(GRPLTwoWayLever _) => OnLeverForwardActivated.Invoke();
    private void OnLeverInteractableForwardStopped(GRPLTwoWayLever _) => OnLeverForwardStopped.Invoke();
    private void OnLeverInteractableBackwardActivated(GRPLTwoWayLever _) => OnLeverBackwardActivated.Invoke();
    private void OnLeverInteractableBackwardStopped(GRPLTwoWayLever _) => OnLeverBackwardStopped.Invoke();

}
