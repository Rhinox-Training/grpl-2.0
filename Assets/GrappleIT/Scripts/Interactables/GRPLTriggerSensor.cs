using Rhinox.GUIUtils.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// A simple trigger sensor that detects if a Rhinox hand has entered/exited it.
    /// </summary>
    /// <remarks />
    /// <dependencies><see cref="Collider"/></dependencies>
    [RequireComponent(typeof(Collider))]
    public class GRPLTriggerSensor : MonoBehaviour
    {
        /// <summary>
        /// A private integer variable that stores the layer index of the Rhinox hand.
        /// </summary>
        [Layer] [SerializeField] public int HandLayer = 3;

        /// <summary>
        /// A private UnityEvent variable that stores a list of functions to be invoked when a hand enters the trigger sensor.
        /// </summary>
        [Space(15f)] [SerializeField] private UnityEvent _sensorEnter = null;

        /// <summary>
        /// A private UnityEvent variable that stores a list of functions to be invoked when a hand exits the trigger sensor.
        /// </summary>
        [Space(10f)] [SerializeField] private UnityEvent _sensorExit = null;

        /// <summary>
        ///  A private Collider variable that stores a reference to the Collider component attached to the same GameObject as this script.
        /// </summary>
        private Collider _collider;

        /// <summary>
        /// A public method that takes an array of <see cref="RhinoxJointCapsule"/> objects and sets them to be ignored
        /// from triggering the sensor.
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

        /// <summary>
        /// Sets _collider to the Collider component attached to the same GameObject as this script and sets its
        /// isTrigger property to true.
        /// </summary>
        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        /// <summary>
        ///  A private method that is called when a trigger collider enters the Collider component attached to the same
        /// GameObject as this script. If the other colliders gameObject.layer matches the _handLayer, it invokes the
        /// OnSensorEnter event.
        /// </summary>
        /// <param name="other">The collider that is entering the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == HandLayer)
                _sensorEnter?.Invoke();
        }

        /// <summary>
        ///  A private method that is called when a trigger collider exits the Collider component attached to the same
        /// GameObject as this script. If the other colliders gameObject.layer matches the _handLayer, it invokes the
        /// OnSensorExit event.
        /// </summary>
        /// <param name="other">The collider that is exiting the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == HandLayer)
                _sensorExit?.Invoke();
        }

        //============
        // SensorEnter
        //============
        /// <summary>
        /// A public method that takes a UnityAction parameter and adds it to the OnSensorEnter event's invocation list.
        /// If the event is null, it instantiates a new UnityEvent.
        /// </summary>
        /// <param name="action">The UnityAction to add.</param>
        public void AddListenerOnSensorEnter(UnityAction action)
        {
            if (_sensorEnter == null)
                _sensorEnter = new UnityEvent();

            _sensorEnter.AddListener(action);
        }

        /// <summary>
        /// A public method that removes a UnityAction parameter from the OnSensorEnter event's invocation list.
        /// </summary>
        /// <param name="action">The UnityAction to remove.</param>
        public void RemoveListenerOnSensorEnter(UnityAction action)
        {
            _sensorEnter?.RemoveListener(action);
        }

        /// <summary>
        /// A public method that removes all listeners from the OnSensorEnter event's invocation list.
        /// </summary>
        public void RemoveAllListenersOnSensorEnter()
        {
            _sensorEnter?.RemoveAllListeners();
        }

        /// <summary>
        /// A public method that invokes the OnSensorEnter event.
        /// </summary>
        public void InvokeOnSensorEnter()
        {
            _sensorEnter?.Invoke();
        }

        //==========
        // SensorExit
        //==========
        /// <summary>
        /// A public method that takes a UnityAction parameter and adds it to the OnSensorExit event's invocation list.
        /// If the event is null, it instantiates a new UnityEvent.
        /// </summary>
        /// <param name="action">The UnityAction to add.</param>
        public void AddListenerOnSensorExit(UnityAction action)
        {
            if (_sensorExit == null)
                _sensorExit = new UnityEvent();

            _sensorExit.AddListener(action);
        }

        /// <summary>
        /// A public method that removes a UnityAction parameter from the OnSensorExit event's invocation list.
        /// </summary>
        /// <param name="action">The UnityAction to remove.</param>
        public void RemoveListenerOnSensorExit(UnityAction action)
        {
            _sensorExit?.RemoveListener(action);
        }

        /// <summary>
        /// A public method that removes all listeners from the OnSensorExit event's invocation list.
        /// </summary>
        public void RemoveAllListenersOnSensorExit()
        {
            _sensorExit?.RemoveAllListeners();
        }

        /// <summary>
        /// A public method that invokes the OnSensorExit event.
        /// </summary>
        public void InvokeOnSensorExit()
        {
            _sensorExit?.Invoke();
        }
    }
}