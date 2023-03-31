using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class GRPLButtonInteractableUnityWrapper : MonoBehaviour
{
    [SerializeField] private GRPLButtonInteractable _interactable;

    // Unity events
    [SerializeField] private UnityEvent _onProximityStart;
    [SerializeField] private UnityEvent _onProximityEnd;
    [SerializeField] private UnityEvent _onInteractedStart;
    [SerializeField] private UnityEvent _onInteractedEnd;
    // Unity event props
    public UnityEvent OnInteractedStart => _onInteractedStart;
    public UnityEvent OnInteractedEnd => _onInteractedEnd;
    public UnityEvent OnProximityStart => _onProximityStart;
    public UnityEvent OnProximityEnd => _onProximityEnd;

    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable, null, "GRPLButtonInteractableUnityWrapper, GRPL Poke Interactible not set");
    }

    private void OnEnable()
    {

        _interactable.OnInteractStarted += OnInteractableInteractStarted;
        _interactable.OnInteractEnded += OnInteractableEnded;
        _interactable.OnProximityStarted += OnInteractableProximityStarted;
        _interactable.OnProximityEnded += OnInteractableProximityEnded;
    }

    private void OnDisable()
    {
        _interactable.OnInteractStarted -= OnInteractableInteractStarted;
        _interactable.OnInteractEnded -= OnInteractableEnded;
        _interactable.OnProximityStarted -= OnInteractableProximityStarted;
        _interactable.OnProximityEnded -= OnInteractableProximityEnded;
    }

    private void OnInteractableInteractStarted(GRPLInteractable obj) => OnInteractedStart.Invoke();
    private void OnInteractableEnded(GRPLInteractable obj) => OnInteractedEnd.Invoke();
    private void OnInteractableProximityStarted(GRPLInteractable obj) => OnProximityStart.Invoke();
    private void OnInteractableProximityEnded(GRPLInteractable obj) => OnProximityEnd.Invoke();
}