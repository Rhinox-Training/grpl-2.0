using Rhinox.XR.Grapple.It;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

/// <summary>
/// A Unity component used to wrap a <see cref="GRPLInteractable"/> and expose its events as Unity events in the editor.
/// </summary>
public class GRPLInteractableUnityWrapper : MonoBehaviour
{
    /// <summary>
    /// the reference to the GRPLInteractable component to be wrapped.
    /// </summary>
    [SerializeField] private GRPLInteractable _interactable;

    /// <summary>
    /// A Unity event called when an object is in proximity of the GRPLInteractable.
    /// </summary>
    [SerializeField] private UnityEvent _onProximityStart;
    /// <summary>
    /// A Unity event called when an object is no longer in proximity of the GRPLInteractable.
    /// </summary>
    [SerializeField] private UnityEvent _onProximityEnd;
    /// <summary>
    /// A Unity event called when an object starts interacting with the GRPLInteractable
    /// </summary>
    [SerializeField] private UnityEvent _onInteractedStart;
    /// <summary>
    /// A Unity event called when an object stops interacting with the GRPLInteractable.
    /// </summary>
    [SerializeField] private UnityEvent _onInteractedEnd;

    
    /// <summary>
    /// A getter for the _onInteractedStart property.
    /// </summary>
    public UnityEvent OnInteractedStart => _onInteractedStart;
    /// <summary>
    /// A getter for the _onInteractedEnd property.
    /// </summary>
    public UnityEvent OnInteractedEnd => _onInteractedEnd;
    /// <summary>
    /// A getter for the _onProximityStart property.
    /// </summary>
    public UnityEvent OnProximityStart => _onProximityStart;
    /// <summary>
    /// A getter for the _onProximityEnd property.
    /// </summary>
    public UnityEvent OnProximityEnd => _onProximityEnd;

    /// <summary>
    /// Asserts that the _interactable property is not null.
    /// </summary>
    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable, null, "[GRPLInteractableUnityWrapper], Interactible was not set!");
    }

    /// <summary>
    /// Subscribes to the events of the _interactable component.
    /// </summary>
    private void OnEnable()
    {
        _interactable.OnInteractStarted += OnInteractableInteractStarted;
        _interactable.OnInteractEnded += OnInteractableEnded;
        _interactable.OnProximityStarted += OnInteractableProximityStarted;
        _interactable.OnProximityEnded += OnInteractableProximityEnded;
    }

    /// <summary>
    /// Unsubscribes from the events of the _interactable component.
    /// </summary>
    private void OnDisable()
    {
        _interactable.OnInteractStarted -= OnInteractableInteractStarted;
        _interactable.OnInteractEnded -= OnInteractableEnded;
        _interactable.OnProximityStarted -= OnInteractableProximityStarted;
        _interactable.OnProximityEnded -= OnInteractableProximityEnded;
    }

    /// <summary>
    /// A method called when the GRPLInteractable starts interacting with an object.
    /// Invokes the _onInteractedStart Unity event.
    /// </summary>
    /// <param name="obj"></param>
    private void OnInteractableInteractStarted(GRPLInteractable obj) => OnInteractedStart.Invoke();
    /// <summary>
    /// A method called when the GRPLInteractable stops interacting with an object. Invokes the _onInteractedEnd Unity event.
    /// </summary>
    /// <param name="obj"></param>
    private void OnInteractableEnded(GRPLInteractable obj) => OnInteractedEnd.Invoke();
    /// <summary>
    /// A method called when an object enters the proximity of the GRPLInteractable. Invokes the _onProximityStart Unity event.
    /// </summary>
    /// <param name="obj"></param>
    private void OnInteractableProximityStarted(GRPLInteractable obj) => OnProximityStart.Invoke();
    /// <summary>
    /// A method called when an object leaves the proximity of the GRPLInteractable. Invokes the _onProximityEnd Unity event.
    /// </summary>
    private void OnInteractableProximityEnded(GRPLInteractable obj) => OnProximityEnd.Invoke();
}