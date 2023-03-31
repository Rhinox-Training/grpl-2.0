using Rhinox.GUIUtils.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// A simple trigger sensor to detect if a Rhinox hand has entered/exited it.
    /// </summary>
    /// <remarks />
    /// <dependencies><see cref="Collider"/></dependencies>
    [RequireComponent(typeof(Collider))]
    public class GRPLTriggerSensor : MonoBehaviour
    {
        [Layer][SerializeField] private int _handLayer = 3;
        [Space(15f)]
        [SerializeField] private UnityEvent OnSensorEnter = null;
        [Space(10f)]
        [SerializeField] private UnityEvent OnSensorExit = null;

        public int HandLayer
        {
            get { return _handLayer; }
            set { _handLayer = value; }
        }

        /// <summary>
        /// Takes an array of <see cref="RhinoxJointCapsule"/> to be ignored from trigger the sensor.
        /// </summary>
        /// <param name="rhinoxJoinCapsules">Array of RhinoxJointCapsule gotten from <see cref="GRPLJointManager"/></param>
        public void SetIgnoreList(RhinoxJointCapsule[] rhinoxJoinCapsules)
        {
            Collider proximityCollider = GetComponent<Collider>();
            foreach (var jointCapsule in rhinoxJoinCapsules)
            {
                if (jointCapsule != null)
                    Physics.IgnoreCollision(proximityCollider, jointCapsule.JointCollider);
            }
        }

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorEnter?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _handLayer)
                OnSensorExit?.Invoke();
        }

        //============
        // SensorEnter
        //============
        public void AddListenerOnSensorEnter(UnityAction action)
        {
            if (OnSensorEnter == null)
                OnSensorEnter = new UnityEvent();

            OnSensorEnter.AddListener(action);
        }

        public void RemoveListenerOnSensorEnter(UnityAction action)
        {
            OnSensorEnter?.RemoveListener(action);
        }

        public void RemoveAllListenersOnSensorEnter()
        {
            OnSensorEnter?.RemoveAllListeners();
        }

        public void InvokeOnSensorEnter()
        {
            OnSensorEnter?.Invoke();
        }

        //==========
        // SensorExit
        //==========
        public void AddListenerOnSensorExit(UnityAction action)
        {
            if (OnSensorExit == null)
                OnSensorExit = new UnityEvent();

            OnSensorExit.AddListener(action);
        }

        public void RemoveListenerOnSensorExit(UnityAction action)
        {
            OnSensorExit?.RemoveListener(action);
        }

        public void RemoveAllListenersOnSensorExit()
        {
            OnSensorExit?.RemoveAllListeners();
        }

        public void InvokeOnSensorExit()
        {
            OnSensorExit?.Invoke();
        }
    }
}