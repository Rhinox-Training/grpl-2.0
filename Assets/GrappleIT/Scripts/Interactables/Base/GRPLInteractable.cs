using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This abstract base class is used for Grapple interactables. <br />
    /// If all pure abstract methods are correctly implemented, the derived interactables should work seamlessly with
    /// the <see cref="GRPLInteractableManager"/>. 
    /// </summary>
    public abstract class GRPLInteractable : MonoBehaviour
    {
        public delegate void InteractableEvent(GRPLInteractable grappleInteractable);
        public static InteractableEvent InteractableCreated = null;
        public static InteractableEvent InteractableDestroyed = null;

        public event Action<GRPLInteractable> OnInteractStarted;
        public event Action<GRPLInteractable> OnInteractEnded;
        public event Action<GRPLInteractable> OnProximityStarted;
        public event Action<GRPLInteractable> OnProximityEnded;
        
        protected virtual void Initialize() { }
        protected virtual void Destroyed() { }

        private GRPLInteractionState _state = GRPLInteractionState.Active;
        public GRPLInteractionState State => _state;
        
        public bool ShouldPerformInteractCheck { set; get; } = true;
        
        private void Start()
        {
            Initialize();
            InteractableCreated?.Invoke(this);
        }

        private void OnDestroy()
        {
            InteractableDestroyed?.Invoke(this);
            Destroyed();
        }

        private void OnEnable() => _state = GRPLInteractionState.Active;

        private void OnDisable() => _state = GRPLInteractionState.Disabled;

        public void SetState(GRPLInteractionState newState)
        {
            if(_state == newState)
                return;
            
            switch (_state)
            {
                case GRPLInteractionState.Proximate:
                    ProximityStopped();
                    break;
                case GRPLInteractionState.Interacted:
                    InteractStopped();
                    break;
            }
            
            switch (newState)
            {
                case GRPLInteractionState.Proximate:
                    ProximityStarted();
                    break;
                case GRPLInteractionState.Interacted:
                    InteractStarted();
                    break;
            }

            _state = newState;
        }

        protected virtual void InteractStarted() => OnInteractStarted?.Invoke(this);
        protected virtual void InteractStopped() => OnInteractEnded?.Invoke(this);

        protected virtual void ProximityStarted() => OnProximityStarted?.Invoke(this);
        protected virtual void ProximityStopped() => OnProximityEnded?.Invoke(this);

        /// <summary>
        /// Check, when currently interacting. If the interaction should not get checked anymore.
        /// I.e. When the previous interact joint is now behind the button press object.
        /// </summary>
        public virtual bool ShouldInteractionCheckStop() => false;

        /// <summary>
        /// Check whether the given joint activates the interaction for this interactable.
        /// </summary>
        /// <param name="joint">The joint to check with</param>
        /// <returns>Whether the interaction is now happening</returns>
        public abstract bool CheckForInteraction(RhinoxJoint joint);

        /// <summary>
        /// This function defines which joint should get used when checking for interactions.  <br />
        /// An example could be a projected distance check, where the joint with the lowest distance gets returned.
        /// </summary>
        /// <param name="joints"> A collection of joints to check.</param>
        /// <param name="joint"> An out parameter for a valid joint, if one was found</param>
        /// <returns>Whether a valid joint was found.</returns>
        public abstract bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint);


        /// <summary>
        /// Checks if point p1 is closer to the interactible than point p2.
        /// </summary>
        /// <param name="p1">The main point</param>
        /// <param name="p2">The other point</param>
        /// <returns>A boolean representing whether the point is closer or not.</returns>
        public virtual bool IsPointCloserThanOtherPoint(Vector3 p1, Vector3 p2)
        {
            var position = transform.position;
            float distanceSqr1 = (position - p1).sqrMagnitude;
            float distanceSqr2 = (position - p2).sqrMagnitude;
            return distanceSqr1 < distanceSqr2;
        }
    }
}