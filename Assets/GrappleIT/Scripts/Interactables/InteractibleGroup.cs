using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class InteractibleGroup : MonoBehaviour
    {
        [Header("Delayed activation parameters")] [SerializeField]
        private bool _delayInteractibleReactivation = true;

        [SerializeField] private float _delayInteractibleReactivationTime = 0.25f;

        private List<GRPLInteractable> _interactables;

        private GRPLInteractable _currentInteractedObject;

        private bool _isCoolingDown = false;

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
            if (_delayInteractibleReactivation)
                StartCoroutine(EnableInteractablesAfterDelay());
            _currentInteractedObject = null;
        }

        /// <summary>
        /// Coroutine that enables the cooldown, waits for "_delayInteractibleReactivationTime" and then disables the cooldown.
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