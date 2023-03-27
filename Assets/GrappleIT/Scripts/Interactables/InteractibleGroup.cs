using System;
using System.Collections.Generic;
using Rhinox.XR.Grapple.It;
using UnityEngine;

public class InteractibleGroup : MonoBehaviour
{
    private List<GRPLInteractable> _interactables;

    private GRPLInteractable _currentInteractedObject;

    private void Awake()
    {
        _interactables = new List<GRPLInteractable>();
        foreach (var interactable in transform.GetComponentsInChildren<GRPLInteractable>())
            _interactables.Add(interactable);
    }

    private void OnEnable()
    {
        // Subscribe to the interacted events
        foreach (var interactable in _interactables)
        {
            interactable.OnInteractStarted -= OnInteractedStarted;
            interactable.OnInteractStarted += OnInteractedStarted;

            interactable.OnInteractEnded -= OnInteractedEnded;
            interactable.OnInteractEnded += OnInteractedEnded;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to the interacted events
        foreach (var interactable in _interactables)
        {
            interactable.OnInteractStarted -= OnInteractedStarted;
            interactable.OnInteractEnded -= OnInteractedEnded;
        }
    }

    private void OnInteractedStarted(GRPLInteractable interactable)
    {
        _currentInteractedObject = interactable;
    }

    private void OnInteractedEnded(GRPLInteractable interactable)
    {
        _currentInteractedObject = null;
    }

    /// <summary>
    /// Removes all the interactables from this group in the given collection of that can not be interacted with.
    /// </summary>
    /// <param name="otherInteractables"></param>
    public void FilterImpossibleInteractables(ref List<GRPLInteractable> otherInteractables)
    {
        if (_currentInteractedObject == null)
            return;

        foreach (var interactable in _interactables)
        {
            if(interactable == _currentInteractedObject)
                continue;

            if (otherInteractables.Contains(interactable))
            {
                otherInteractables.Remove(interactable);
                interactable.SetState(GRPLInteractionState.Active);                
            }
        }
    }
}