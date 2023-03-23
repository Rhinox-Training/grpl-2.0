using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class PokeInteractableUnityWrapper : MonoBehaviour
{
    [SerializeField] private GRPLButtonInteractable _interactable;
    
    // Unity events
    [SerializeField] private UnityEvent _onSelectStart;
    [SerializeField] private UnityEvent _onSelectEnd;
    [SerializeField] private UnityEvent _onProximityStart;
    [SerializeField] private UnityEvent _onProximityEnd;

    // Unity event props
    public UnityEvent OnSelectStart => _onSelectStart;
    public UnityEvent OnSelectEnd => _onSelectEnd;
    public UnityEvent OnProximityStart => _onProximityStart;
    public UnityEvent OnProximityEnd => _onProximityEnd;

    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable,null,"PokeInteractableUnityWrapper, GRPL Poke Interactible not set");
        
    }

    private void OnEnable()
    {
        
        _interactable.OnSelectStarted += OnInteractableSelectStarted;
        _interactable.OnSelectEnd += OnInteractableSelectEnded;
        _interactable.OnProximityStarted += OnInteractableProximityStarted;
        _interactable.OnProximityEnded += OnInteractableProximityEnded;
    }

    private void OnDisable()
    {
        _interactable.OnSelectStarted -= OnInteractableSelectStarted;
        _interactable.OnSelectEnd -= OnInteractableSelectEnded;
        _interactable.OnProximityStarted -= OnInteractableProximityStarted;
        _interactable.OnProximityEnded -= OnInteractableProximityEnded;
   }

    private void OnInteractableSelectStarted() => OnSelectStart.Invoke();
    private void OnInteractableSelectEnded() => OnSelectEnd.Invoke();
    private void OnInteractableProximityStarted() => OnProximityStart.Invoke();
    private void OnInteractableProximityEnded() => OnProximityEnd.Invoke();
}
