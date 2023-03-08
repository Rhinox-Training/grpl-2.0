using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLBaseInteractable : MonoBehaviour
    {
        protected Transform _previousParentTransform = null;

        protected Rigidbody _rigidBody = null;

        protected bool _wasKinematic;
        protected bool _hadGravity;

        protected bool _isValid = true;

        //protected void StartInteractable()
        //{
        //    if (TryGetComponent(out _rigidBody))
        //    {
        //        _wasKinematic = _rigidBody.isKinematic;
        //        _hadGravity = _rigidBody.useGravity;
        //        _previousParentTransform = transform.parent;
        //    }
        //    else
        //        _isValid = false;
        //}

        public virtual void OnValidate()
        {
            //trygetcomponent
            //gameObject.GetComponentInChildren<>
        }

        public virtual void Start()
        {
            if (TryGetComponent(out _rigidBody))
            {
                _wasKinematic = _rigidBody.isKinematic;
                _hadGravity = _rigidBody.useGravity;
                _previousParentTransform = transform.parent;
            }
            else
                _isValid = false;
        }

        public virtual void Grabbed(GameObject parent, Vector3 positionalOffset, Quaternion rotationalOffset)
        {
            if (!_isValid)
                return;

            _wasKinematic = _rigidBody.isKinematic;
            _hadGravity = _rigidBody.useGravity;

            _rigidBody.isKinematic = true;
            _rigidBody.useGravity = false;
            _previousParentTransform = transform.parent;

            gameObject.transform.parent = parent.transform;
        }

        public virtual void Dropped()
        {
            if (!_isValid)
                return;

            _rigidBody.isKinematic = _wasKinematic;
            _rigidBody.useGravity = _hadGravity;

            gameObject.transform.parent = _previousParentTransform;
        }
    }
}
