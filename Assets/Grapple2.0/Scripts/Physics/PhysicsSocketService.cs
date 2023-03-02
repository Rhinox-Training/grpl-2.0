using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    public sealed class PhysicsSocketService : IPhysicsService
    {
        private bool _isInitialized = false;
        private BoneManager _boneManager;

        private GameObject _colliderObjL;
        private GameObject _colliderObjR;

        private SphereCollider _sphereColliderL;
        private SphereCollider _sphereColliderR;

        private bool _enabledL = true;
        private bool _enabledR = true;

        //private bool _isHoldingL = false;
        //private bool _isHoldingR = false;

        private const float GRABBING_THRESHOLD = 0.04f;
        private const float DROPPING_THRESHOLD = 0.072f;

        private GameObject _potentialGrabItemL = null;
        private GameObject _potentialGrabItemR = null;

        private GameObject _grabbedItemL = null;
        private GameObject _grabbedItemR = null;


        public PhysicsSocketService(BoneManager boneManager, GameObject parentObject)
        {
            _boneManager = boneManager;

            if (parentObject != null)
            {
                _colliderObjL = new GameObject("Collider Obj LEFT");
                _colliderObjL.transform.parent = parentObject.transform;
                var colliderEventsL = _colliderObjL.AddComponent<PhysicsEventHandler>();
                colliderEventsL.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsL.Hand = Hand.Left;

                _sphereColliderL = _colliderObjL.AddComponent<SphereCollider>();
                _sphereColliderL.isTrigger = true;
                _sphereColliderL.enabled = false;
                _sphereColliderL.center = new(0, -0.04f, 0.04f);//hardcoded values gathered from lot's of eyeballing and testing
                _sphereColliderL.radius = 0.05f;

                _colliderObjR = new GameObject("Collider Obj RIGHT");
                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.Hand = Hand.Right;

                _colliderObjR.transform.parent = parentObject.transform;
                _sphereColliderR = _colliderObjR.AddComponent<SphereCollider>();
                _sphereColliderR.enabled = false;
                _sphereColliderR.isTrigger = true;
                _sphereColliderR.center = new(0, -0.04f, 0.04f); //hardcoded values gathered from lot's of eyeballing and testing
                _sphereColliderR.radius = 0.05f;
            }
        }

        public void TryInitialize()
        {
            if (!_boneManager.IsInitialised)
                return;

            _sphereColliderL.enabled = true;
            _sphereColliderR.enabled = true;

            _isInitialized = true;
        }

        public bool GetIsInitialised()
        {
            return _isInitialized;
        }

        public void Update()
        {
            if (!_isInitialized)
                return;

            var palmBoneE = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.Palm);


            //float total = 0f;
            //var thumbTip = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.ThumbTip);
            ////Debug.Log($"Thumb: {Vector3.SqrMagnitude(palmBoneE.BonePosition - thumbTip.BonePosition)}");
            //total += Vector3.SqrMagnitude(palmBoneE.BonePosition - thumbTip.BonePosition);
            //var indexTip = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.IndexTip);
            ////Debug.Log($"Index: {Vector3.SqrMagnitude(palmBoneE.BonePosition - indexTip.BonePosition)}");
            //total += Vector3.SqrMagnitude(palmBoneE.BonePosition - indexTip.BonePosition);
            //var middleTip = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.MiddleTip);
            ////Debug.Log($"Middle: {Vector3.SqrMagnitude(palmBoneE.BonePosition - middleTip.BonePosition)}");
            //total += Vector3.SqrMagnitude(palmBoneE.BonePosition - middleTip.BonePosition);
            //var ringTip = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.RingTip);
            ////Debug.Log($"Ring: {Vector3.SqrMagnitude(palmBoneE.BonePosition - ringTip.BonePosition)}");
            //total += Vector3.SqrMagnitude(palmBoneE.BonePosition - ringTip.BonePosition);
            //var littleTip = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.LittleTip);
            ////Debug.Log($"Pinky: {Vector3.SqrMagnitude(palmBoneE.BonePosition - littleTip.BonePosition)}");
            //total += Vector3.SqrMagnitude(palmBoneE.BonePosition - littleTip.BonePosition);

            //Debug.Log($"total: {total}");

            //if the left socket AND the right socket are both not enabled no need to update.
            if (!_enabledL && !_enabledR)
                return;

            if (_enabledL)
            {
                var palmBone = _boneManager.GetBoneFromHandById(Hand.Left, XRHandJointID.Palm);
                _colliderObjL.transform.position = palmBone.BonePosition;
                _colliderObjL.transform.rotation = palmBone.BoneRotation;

                if (_potentialGrabItemL != null && IsTryingToGrab(palmBone, Hand.Left))
                {
                    _potentialGrabItemL.transform.parent = _colliderObjL.transform;
                    _grabbedItemL = _potentialGrabItemL;
                    _potentialGrabItemL = null;
                }
                else if (_grabbedItemL != null && IsTryingToDrop(palmBone, Hand.Left))
                {
                    _grabbedItemL.transform.parent = null;
                    _grabbedItemL = null;
                }

            }

            if (_enabledR)
            {
                var palmBone = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.Palm);
                _colliderObjR.transform.position = palmBone.BonePosition;
                _colliderObjR.transform.rotation = palmBone.BoneRotation;

                if (_potentialGrabItemR != null && IsTryingToGrab(palmBone, Hand.Right))
                {
                    _potentialGrabItemR.transform.parent = _colliderObjR.transform;
                    _grabbedItemR = _potentialGrabItemR;
                    _potentialGrabItemR = null;
                }
                else if (_grabbedItemR != null && IsTryingToDrop(palmBone, Hand.Right))
                {
                    _grabbedItemR.transform.parent = null;
                    _grabbedItemR = null;
                }
            }

        }

        public void OnHandTriggerEnter(GameObject triggerObj, GameObject otherObj, Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    _potentialGrabItemL = otherObj;
                    break;
                case Hand.Right:
                    _potentialGrabItemR = otherObj;
                    break;
                default:
                    break;
            }
        }

        public void OnHandTriggerExit(GameObject triggerObj, GameObject otherObj, Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    if (_potentialGrabItemL == otherObj)
                        _potentialGrabItemL = null;
                    break;
                case Hand.Right:
                    if (_potentialGrabItemR == otherObj)
                        _potentialGrabItemR = null;
                    break;
                default:
                    break;
            }
            //Debug.Log($"\"{hand}\" Left \"{otherObj}\"");
            //Debug.Log("EVENT \\o/");
        }

        bool IsTryingToGrab(RhinoxBone palmBone, Hand hand)
        {
            if (hand == Hand.Both)
                return false;

            if (hand == Hand.Left && _grabbedItemL != null)
                return false;

            if (hand == Hand.Right && _grabbedItemR != null)
                return false;

            //var thumbTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.ThumbTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - thumbTip.BonePosition) > GRABBING_THRESHOLD)
            //    return false;

            //var indexTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.IndexTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - indexTip.BonePosition) > GRABBING_THRESHOLD)
            //    return false;

            //var middleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.MiddleTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - middleTip.BonePosition) > GRABBING_THRESHOLD)
            //    return false;

            //var ringTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.RingTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - ringTip.BonePosition) > GRABBING_THRESHOLD)
            //    return false;

            //var littleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.LittleTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - littleTip.BonePosition) > GRABBING_THRESHOLD)
            //    return false;

            float total = 0f;
            var thumbTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.ThumbTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - thumbTip.BonePosition);
            var indexTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.IndexTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - indexTip.BonePosition);
            var middleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.MiddleTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - middleTip.BonePosition);
            var ringTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.RingTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - ringTip.BonePosition);
            var littleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.LittleTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - littleTip.BonePosition);

            if (total > GRABBING_THRESHOLD)
                return false;

            return true;
        }

        bool IsTryingToDrop(RhinoxBone palmBone, Hand hand)
        {
            if (hand == Hand.Both)
                return false;

            if (hand == Hand.Left && _grabbedItemL == null)
                return false;

            if (hand == Hand.Right && _grabbedItemR == null)
                return false;

            //var thumbTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.ThumbTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - thumbTip.BonePosition) > DROPPING_THRESHOLD)
            //    return false;

            //var indexTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.IndexTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - indexTip.BonePosition) > DROPPING_THRESHOLD)
            //    return false;

            //var middleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.MiddleTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - middleTip.BonePosition) > DROPPING_THRESHOLD)
            //    return false;

            //var ringTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.RingTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - ringTip.BonePosition) > DROPPING_THRESHOLD)
            //    return false;

            //var littleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.LittleTip);
            //if (Vector3.SqrMagnitude(palmBone.BonePosition - littleTip.BonePosition) > DROPPING_THRESHOLD)
            //    return false;
            float total = 0f;
            var thumbTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.ThumbTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - thumbTip.BonePosition);
            var indexTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.IndexTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - indexTip.BonePosition);
            var middleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.MiddleTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - middleTip.BonePosition);
            var ringTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.RingTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - ringTip.BonePosition);
            var littleTip = _boneManager.GetBoneFromHandById(hand, XRHandJointID.LittleTip);
            total += Vector3.SqrMagnitude(palmBone.BonePosition - littleTip.BonePosition);

            if (total < DROPPING_THRESHOLD)
                return false;

            return true;
        }

        public void SetEnabled(bool newState, Hand handedness)
        {
            switch (handedness)
            {
                case Hand.Left:
                    _enabledL = newState;
                    _sphereColliderL.enabled = newState;
                    break;
                case Hand.Right:
                    _enabledR = newState;
                    _sphereColliderR.enabled = newState;
                    break;
                case Hand.Both:
                    _enabledL = newState;
                    _sphereColliderL.enabled = newState;
                    _enabledR = newState;
                    _sphereColliderR.enabled = newState;
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