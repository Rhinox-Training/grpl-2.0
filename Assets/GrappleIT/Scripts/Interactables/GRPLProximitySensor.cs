using Rhinox.GUIUtils.Attributes;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.XR.Grapple.It
{
    [RequireComponent(typeof(Collider))]
    public class GRPLProximitySensor : MonoBehaviour
    {
        [Layer][SerializeField] private int _handLayer = 3;
        public int HandLayer
        {
            get { return _handLayer; }
            set { _handLayer = value; }
        }

        [Space(15f)]
        [SerializeField] private UnityEvent OnSensorEnter = null;
        [Space(10f)]
        [SerializeField] private UnityEvent OnSensorExit = null;

        public void SetIgnoreList(RhinoxJointCapsule[] rhinoxJoinCapsules)
        {
            Collider proximityCollider = GetComponent<Collider>();
            foreach (var jointCapsule in rhinoxJoinCapsules)
            {
                if (jointCapsule != null)
                    Physics.IgnoreCollision(proximityCollider, jointCapsule.JointCollider);
            }
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


        #region SensorEnter
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
        #endregion

        #region SensorExit
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
        #endregion
    }
}