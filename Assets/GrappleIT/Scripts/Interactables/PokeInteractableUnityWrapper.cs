using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class PokeInteractableUnityWrapper : MonoBehaviour
{
    [SerializeField] private GRPLButtonInteractable _interactable;
    
    // Unity events
    [SerializeField] private UnityEvent _onInteractedStart;
    [SerializeField] private UnityEvent _OnInteractedEnd;
    [SerializeField] private UnityEvent _onProximityStart;
    [SerializeField] private UnityEvent _onProximityEnd;

    // Unity event props
    public UnityEvent OnInteractedStart => _onInteractedStart;
    public UnityEvent OnInteractedEnd => _OnInteractedEnd;
    public UnityEvent OnProximityStart => _onProximityStart;
    public UnityEvent OnProximityEnd => _onProximityEnd;

    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable,null,"PokeInteractableUnityWrapper, GRPL Poke Interactible not set");
        
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

    private void OnInteractableInteractStarted() => OnInteractedStart.Invoke();
    private void OnInteractableEnded() => OnInteractedEnd.Invoke();
    private void OnInteractableProximityStarted() => OnProximityStart.Invoke();
    private void OnInteractableProximityEnded() => OnProximityEnd.Invoke();
}
