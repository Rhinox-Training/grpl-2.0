using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private UnityEvent _onHoverStart;
    [SerializeField] private UnityEvent _onHoverEnd;

    // Unity event props
    public UnityEvent OnSelectStart => _onSelectStart;
    public UnityEvent OnSelectEnd => _onSelectEnd;
    public UnityEvent OnHoverStart => _onHoverStart;
    public UnityEvent OnHoverEnd => _onHoverEnd;

    private void OnValidate()
    {
        Assert.AreNotEqual(_interactable,null,"PokeInteractableUnityWrapper, GRPL Poke Interactible not set");
        
    }

    private void OnEnable()
    {
        
        _interactable.OnSelectStarted += OnInteractableSelectStarted;
        _interactable.OnSelectEnd += OnInteractableSelectEnded;
        _interactable.OnHoverStarted += OnInteractableHoverStarted;
        _interactable.OnHoverEnd += OnInteractableHoverEnded;
    }

    private void OnDisable()
    {
        _interactable.OnSelectStarted -= OnInteractableSelectStarted;
        _interactable.OnSelectEnd -= OnInteractableSelectEnded;
        _interactable.OnHoverStarted -= OnInteractableHoverStarted;
        _interactable.OnHoverEnd -= OnInteractableHoverEnded;
   }

    private void OnInteractableSelectStarted() => OnSelectStart.Invoke();
    private void OnInteractableSelectEnded() => OnSelectEnd.Invoke();
    private void OnInteractableHoverStarted() => OnHoverStart.Invoke();
    private void OnInteractableHoverEnded() => OnHoverEnd.Invoke();
}
