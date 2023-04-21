using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This component defines a group of interactables that can only be interacted with one at a time. It provides
    /// functionality to enable and disable a list of interactables based on which one is currently being interacted
    /// with. It also provides the ability to filter out interactables that cannot be interacted with based on the
    /// current state of the group.    /// </summary>
    public class GRPLInteractibleGroup : MonoBehaviour
    {
        /// <summary>
        /// Determines whether there should be a delay before interactables can be interacted with again.
        /// </summary>
        [Header("Delayed activation parameters")]
        [SerializeField] private bool _delayInteractibleReactivation = true;

        /// <summary>
        /// The time to wait before interactables can be interacted with again.
        /// </summary>
        [SerializeField] private float _delayInteractibleReactivationTime = 0.25f;

        /// <summary>
        /// A list of interactables that are part of the group.
        /// </summary>
        private List<GRPLInteractable> _interactables;

        /// <summary>
        /// The current interacted object in the group.
        /// </summary>
        private GRPLInteractable _currentInteractedObject;

        /// <summary>
        /// Whether the group is on cooldown
        /// </summary>
        private bool _isCoolingDown = false;

        /// <summary>
        /// Initializes the list of interactables in the group.
        /// </summary>
        private void Awake()
        {
            _interactables = new List<GRPLInteractable>();
            foreach (var interactable in transform.GetComponentsInChildren<GRPLInteractable>())
                _interactables.Add(interactable);
        }

        /// <summary>
        /// Subscribes to the OnInteractStarted and OnInteractEnded events of the interactables in the group.
        /// </summary>
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

        /// <summary>
        /// Unsubscribes from the OnInteractStarted and OnInteractEnded events of the interactables in the group.
        /// </summary>
        private void OnDisable()
        {
            // Unsubscribe to the interacted events
            foreach (var interactable in _interactables)
            {
                interactable.OnInteractStarted -= OnInteractedStarted;
                interactable.OnInteractEnded -= OnInteractedEnded;
            }
        }

        /// <summary>
        /// Sets the current interacted object in the group to the interactable that has just been interacted with.
        /// </summary>
        /// <param name="interactable">The interactable that has just been interacted with.</param>
        private void OnInteractedStarted(GRPLInteractable interactable)
        {
            _currentInteractedObject = interactable;
        }

        /// <summary>
        /// Resets the current interacted object to null and starts the delay coroutine if the _delayInteractibleReactivation flag is set.
        /// </summary>
        /// <param name="interactable">The interactable that has just ended its interaction.</param>
        private void OnInteractedEnded(GRPLInteractable interactable)
        {
            if (_delayInteractibleReactivation)
                StartCoroutine(EnableInteractablesAfterDelay());
            _currentInteractedObject = null;
        }

        /// <summary>
        /// A coroutine that enables the cooldown, waits for "_delayInteractibleReactivationTime" seconds and then
        /// disables the cooldown.
        /// </summary>
        private IEnumerator EnableInteractablesAfterDelay()
        {
            _isCoolingDown = true;
            yield return new WaitForSecondsRealtime(_delayInteractibleReactivationTime);
            _isCoolingDown = false;
        }

        /// <summary>
        /// Removes all the interactables from this group in the given collection of that can not be interacted with.
        /// </summary>
        /// <param name="otherInteractables"></param>
        public void FilterImpossibleInteractables(ref List<GRPLInteractable> otherInteractables)
        {
            // If there is no current interacted object or the group is not on cooldown,
            // return
            if (_currentInteractedObject == null && !_isCoolingDown)
                return;

            // Loop over all the interactables in the group
            foreach (var interactable in _interactables)
            {
                // If the group is not on cooldown,
                // don't remove the current interacted object
                if (!_isCoolingDown)
                {
                    if (interactable == _currentInteractedObject)
                        continue;
                }

                // If the interactable is in the list of other interactables,
                // remove it
                if (otherInteractables.Contains(interactable))
                {
                    otherInteractables.Remove(interactable);
                    interactable.SetState(GRPLInteractionState.Active);
                }
            }
        }
    }
}