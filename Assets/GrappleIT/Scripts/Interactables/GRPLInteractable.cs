using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
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
            GRPLInteractable.InteractableCreated?.Invoke(this);
        }

        private void OnDestroy()
        {
            GRPLInteractable.InteractableDestroyed?.Invoke(this);
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
        
    }
}