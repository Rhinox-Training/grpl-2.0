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

        internal virtual void InteractStarted() {}
        internal virtual void InteractStopped() {}

        internal virtual void HoverStarted() {}
        internal virtual void HoverStopped() {}
        
    }
}