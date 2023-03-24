using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This abstract base class is used for Grapple interactables. <br />
    /// If all pure abstract methods are correctly implemented, the derived interactables should work seamlessly with
    /// the <see cref="InteractableManager"/>. 
    /// </summary>
    public abstract class GRPLInteractable : MonoBehaviour
    {
        public delegate void InteractableEvent(GRPLInteractable grappleInteractable);
        public static InteractableEvent InteractableCreated = null;
        public static InteractableEvent InteractableDestroyed = null;

        protected virtual void Initialize() { }
        protected virtual void Destroyed() { }

        private GRPLInteractionState _state = GRPLInteractionState.Active;
        public GRPLInteractionState State => _state;
        
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

        private protected virtual void InteractStarted() {}
        private protected virtual void InteractStopped() {}

        private protected virtual void ProximityStarted() {}
        private protected virtual void ProximityStopped() {}

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
    }
}