using Rhinox.GUIUtils.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(Collider))]
    public class GRPLProximitySensor : MonoBehaviour
    {
        [Layer][SerializeField] private int _handLayer = 3;

        [Space(15f)]
        public UnityEvent<GameObject> OnSensorEnter = new();
        [Space(10f)]
        public UnityEvent<GameObject> OnSensorExit = new();

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorEnter.Invoke(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorExit.Invoke(other.gameObject);
        }
    }
}