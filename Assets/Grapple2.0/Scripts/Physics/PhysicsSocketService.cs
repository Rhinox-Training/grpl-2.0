using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Rhinox.XR.Grapple
{
    public class PhysicsSocketService : IPhysicsService
    {
        private bool _isInitialized = false;
        private BoneManager _boneManager;

        private SphereCollider _sphereColliderL;
        private SphereCollider _sphereColliderR;

        private bool _enabledL = false;
        private bool _enabledR = false;

        public PhysicsSocketService(BoneManager boneManager, GameObject parentObject)
        {
            _boneManager = boneManager;

            if (parentObject != null)
            {
                _sphereColliderL = parentObject.AddComponent<SphereCollider>();
                _sphereColliderL.isTrigger = true;
                _sphereColliderL.enabled = false;
                _sphereColliderR = parentObject.AddComponent<SphereCollider>();
                _sphereColliderR.enabled = false;
                _sphereColliderR.isTrigger = true;
            }
        }

        public void Initialize()
        {

            _isInitialized = true;
        }

        public bool GetIsInitialised()
        {
            return _isInitialized;
        }

        public void Update()
        {
            //_sphereColliderL.center
        }

        public void SetEnabled(bool newState, Hand handedness)
        {
            switch (handedness)
            {
                case Hand.Left:
                    _enabledL = newState;
                    break;
                case Hand.Right:
                    _enabledR = newState;
                    break;
                case Hand.Both:
                    _enabledL = newState;
                    _enabledR = newState;
                    break;
            }
        }

        public bool GetIsEnabled(Hand handedness)
        {
            switch (handedness)
            {
                case Hand.Left:
                    return _enabledL;
                case Hand.Right:
                    return _enabledR;
                case Hand.Both:
                    return _enabledL && _enabledR;
                default:
                    return false;
            }
        }
    }
}