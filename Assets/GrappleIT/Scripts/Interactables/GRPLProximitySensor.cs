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
        public UnityEvent OnSensorEnter = new();
        [Space(10f)]
        public UnityEvent OnSensorExit = new();


        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorEnter.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorExit.Invoke();
        }
    }
}