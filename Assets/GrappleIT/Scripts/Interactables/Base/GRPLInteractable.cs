using Rhinox.GUIUtils.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This is an abstract base class used for Grapple interactables.
    /// The derived interactables work seamlessly with the <see cref="GRPLInteractableManager"/> if all the pure abstract methods are
    /// correctly implemented. This class provides events and methods for detecting proximity and interaction with
    /// the interactable object.
    /// </summary>
    /// <remarks>The <see cref="Start"/> and <see cref="OnDestroy"/> should NOT be overwritten.<br />
    /// Use <see cref="Initialize"/> <see cref="Destroyed"/> respectively instead.</remarks>
    /// <dependencies />
    public abstract class GRPLInteractable : MonoBehaviour
    {
        /// <summary>
        ///  A delegate used to handle events for InteractableCreated and InteractableDestroyed.
        /// </summary>
        public delegate void InteractableEvent(GRPLInteractable grappleInteractable);

        /// <summary>
        /// A static event that is invoked when a new instance of GRPLInteractable is created.
        /// </summary>
        public static InteractableEvent InteractableCreated = null;

        /// <summary>
        /// A static event that is invoked when an instance of GRPLInteractable is destroyed.
        /// </summary>
        public static InteractableEvent InteractableDestroyed = null;

        /// <summary>
        /// The radius around the interactable object that determines when a user is close enough to it and should be considered proximate.
        /// </summary>
        [Header("Proximate detection parameters")] [SerializeField]
        private float _proximateRadius = .5f;

#if UNITY_EDITOR
        [SerializeField] private bool _showProximateRadius = false;
#endif

        /// <summary>
        /// The hand joint that is used to determine the proximity of a user to the interactable object.
        /// </summary>
        [SerializeField] private XRHandJointID _proximateJointID = XRHandJointID.MiddleMetacarpal;

        /// <summary>
        /// A boolean that determines whether a forced interactable joint is used instead of calculating it at runtime.
        /// </summary>
        [Header("Interact detection parameters")] [SerializeField]
        protected bool _forceInteractibleJoint = false;

        /// <summary>
        /// The forced interactable joint ID to be used if _forceInteractibleJoint is true.
        /// </summary>
        [SerializeField] [HideIfField(false, "_forceInteractibleJoint")]
        protected XRHandJointID _forcedInteractJointID = XRHandJointID.IndexTip;

        /// <summary>
        /// A read-only property that returns _proximateJointID.
        /// </summary>
        public XRHandJointID ProximateJointID => _proximateJointID;

        /// <summary>
        /// A read-only property that returns _proximateRadius.
        /// </summary>
        public float ProximateRadius => _proximateRadius;

        /// <summary>
        ///  An event that is invoked when interaction with the interactable object starts.
        /// </summary>
        public event Action<GRPLInteractable> OnInteractStarted;

        /// <summary>
        /// An event that is invoked when interaction with the interactable object ends.
        /// </summary>
        public event Action<GRPLInteractable> OnInteractEnded;

        /// <summary>
        /// An event that is invoked when the user is close enough to the interactable object for proximity detection.
        /// </summary>
        public event Action<GRPLInteractable> OnProximityStarted;

        /// <summary>
        /// An event that is invoked when the user is no longer close enough to the interactable object for proximity detection.
        /// </summary>
        public event Action<GRPLInteractable> OnProximityEnded;

        /// <summary>
        /// The current state of the interactable object.
        /// </summary>
        private GRPLInteractionState _state = GRPLInteractionState.Active;

        /// <summary>
        /// A read-only property that returns the current state of the interactable object.
        /// </summary>
        public GRPLInteractionState State => _state;

        /// <summary>
        /// A boolean that determines whether interaction checks should be performed.
        /// </summary>
        public bool ShouldPerformInteractCheck { set; get; } = true;

        protected virtual void Start()
        {
            Initialize();
            InteractableCreated?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            InteractableDestroyed?.Invoke(this);
            Destroyed();
        }

        /// <summary>
        /// A virtual method that can be overridden in derived classes to perform initialization tasks.
        /// </summary>
        protected virtual void Initialize()
        {
        }
        /// <summary>
        /// A virtual method that can be overridden in derived classes to perform tasks when the interactable object is destroyed.
        /// </summary>
        protected virtual void Destroyed()
        {
        }

        protected virtual void OnEnable() => _state = GRPLInteractionState.Active;

        protected virtual void OnDisable() => _state = GRPLInteractionState.Disabled;

        /// <summary>
        /// Sets the new state for this interactable object and invokes the according events.
        /// </summary>
        /// <remarks>Return early if the newState equals the current state of the interactable object.</remarks>
        /// <param name="newState">The new state for the interactable object.</param>
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
        /// Returns the reference transform for this interactible. <br />
        /// This can be used for proximate interaction checking.
        /// </summary>
        /// <returns>The reference transform.</returns>
        public virtual Transform GetReferenceTransform() => transform;

        /// <summary>
        /// Check whether the given joint activates the interaction for this interactable.
        /// </summary>
        /// <param name="joint">The joint to check with.</param>
        /// <param name="hand">The hand that the joint is from.</param>
        /// <returns>Whether the interaction is now happening.</returns>
        public abstract bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand);

        /// <summary>
        /// This function defines which joint should get used when checking for interactions.  <br />
        /// An example could be a projected distance check, where the joint with the lowest distance gets returned.
        /// </summary>
        /// <param name="joints"> A collection of joints to check.</param>
        /// <param name="outJoint"> An out parameter for a valid joint, if one was found.</param>
        /// <param name="hand">The hand that is currently getting checked.</param>
        /// <returns>Whether a valid joint was found.</returns>
        public abstract bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint,
            RhinoxHand hand);

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (_showProximateRadius)
            {
                using (new eUtility.GizmoColor(1f, 1f, 1f, 1f))
                {
                    Gizmos.DrawWireSphere(transform.position, _proximateRadius);
                }
            }
        }
#endif
    }
}