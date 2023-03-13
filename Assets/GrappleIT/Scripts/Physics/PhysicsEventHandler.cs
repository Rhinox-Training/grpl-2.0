using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.XR.Grapple.It
{
    public class PhysicsEventHandler : MonoBehaviour
    {
        public class PhysicsEvent : UnityEvent<GameObject, GameObject, Hand>
        { }

        public PhysicsEvent EnterEvent { get; private set; } = new PhysicsEvent();
        public PhysicsEvent ExitEvent { get; private set; } = new PhysicsEvent();

        public Hand Hand;

        private void Awake()
        {
            if (EnterEvent == null)
                EnterEvent = new PhysicsEvent();
            if (ExitEvent == null)
                ExitEvent = new PhysicsEvent();
        }

        private void OnTriggerEnter(Collider other)
        {
            EnterEvent.Invoke(gameObject, other.gameObject, Hand);
        }

        private void OnTriggerExit(Collider other)
        {
            ExitEvent.Invoke(gameObject, other.gameObject, Hand);
        }
    }
}