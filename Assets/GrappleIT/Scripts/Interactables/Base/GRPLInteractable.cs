using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

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

        [Header("Proximate detection parameters")]
        [SerializeField] private float _proximateRadius = .5f;

        [Header("Interact detection parameters")]
        [SerializeField] protected bool _forceInteractibleJoint = false;
        [SerializeField] protected XRHandJointID _forcedInteractJointID = XRHandJointID.IndexTip;
        
        public float ProximateRadius => _proximateRadius;
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

        protected virtual void OnEnable() => _state = GRPLInteractionState.Active;

        protected virtual void OnDisable() => _state = GRPLInteractionState.Disabled;

        public void SetState(GRPLInteractionState newState)
        {
            if (_state == newState)
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
        /// Returns the reference position for this interactible. <br />
        /// This can be used for proximate interaction checking.
        /// </summary>
        /// <returns>The referencePoint</returns>
        public virtual Vector3 GetReferencePoint() => transform.position; 
        
        /// <summary>
        /// Check whether the given joint activates the interaction for this interactable.
        /// </summary>
        /// <param name="joint">The joint to check with</param>
        /// <param name="hand">The hand that the joint is from</param>
        /// <returns>Whether the interaction is now happening</returns>
        public abstract bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand);

        /// <summary>
        /// This function defines which joint should get used when checking for interactions.  <br />
        /// An example could be a projected distance check, where the joint with the lowest distance gets returned.
        /// </summary>
        /// <param name="joints"> A collection of joints to check.</param>
        /// <param name="outJoint"> An out parameter for a valid joint, if one was found</param>
        /// <returns>Whether a valid joint was found.</returns>
        public abstract bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint);


        /// <summary>
        /// Checks if point p1 is closer to the interactible than point p2.
        /// </summary>
        /// <param name="p1">The main point</param>
        /// <param name="p2">The other point</param>
    }
}