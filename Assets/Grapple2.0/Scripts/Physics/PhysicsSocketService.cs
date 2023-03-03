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
        enum HandState
        {
            Neutral,
            Grabbing,
            //Holding,
            Dropping
        }

        private bool _isInitialized = false;
        private BoneManager _boneManager;

        private GameObject _colliderObjL;
        private GameObject _colliderObjR;

        private SphereCollider _ColliderL;
        //private BoxCollider _ColliderL;
        private SphereCollider _ColliderR;
        //private SphereCollider _ColliderR;

        private bool _enabledL = true;
        private bool _enabledR = true;

        private HandState _currentHandStateL = HandState.Neutral;
        private HandState _previousHandStateL = HandState.Neutral;
        private HandState _currentHandStateR = HandState.Neutral;
        private HandState _previousHandStateR = HandState.Neutral;

        //private bool _isInGrabPoseL = false;
        //private bool _wasInGrabPoseL = false;
        //private bool _isInGrabPoseR = false;
        //private bool _wasInGrabPoseR = false;
        //private bool _isHoldingR = false;

        private const float GRABBING_THRESHOLD = 0.05f;
        private const float DROPPING_THRESHOLD = 0.065f;//prev: 0.072

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

                _ColliderL = _colliderObjL.AddComponent<SphereCollider>();
                _ColliderL.isTrigger = true;
                _ColliderL.enabled = false;
                //_ColliderL.center = new(0, -0.04f, 0.04f);//hardcoded values gathered from lot's of eyeballing and testing
                _ColliderL.center = new(0, -0.03f, 0.015f);//hardcoded values gathered from lot's of eyeballing and testing
                //_ColliderL.radius = 0.05f;
                _ColliderL.radius = 0.04f;

                _colliderObjR = new GameObject("Collider Obj RIGHT");
                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.Hand = Hand.Right;

                _colliderObjR.transform.parent = parentObject.transform;
                _ColliderR = _colliderObjR.AddComponent<SphereCollider>();
                _ColliderR.enabled = false;
                _ColliderR.isTrigger = true;
                _ColliderR.center = new(0, -0.03f, 0.015f); //hardcoded values gathered from lot's of eyeballing and testing
                _ColliderR.radius = 0.04f;
            }
        }

        public void TryInitialize()
        {
            if (!_boneManager.IsInitialised)
                return;

            _ColliderL.enabled = true;
            _ColliderR.enabled = true;

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

            //if the left socket AND the right socket are both not enabled no need to update.
            if (!_enabledL && !_enabledR)
                return;

            #region LeftHandLogic
            if (_enabledL)
            {
                var palmBone = _boneManager.GetBoneFromHandById(Hand.Left, XRHandJointID.Palm);
                _colliderObjL.transform.position = palmBone.BonePosition;
                _colliderObjL.transform.rotation = palmBone.BoneRotation;

                //Grabbing logic
                _currentHandStateL = GetCurrentHandState(palmBone, Hand.Left);


                //if (_previousHandStateL == HandState.Neutral)
                //    _currentHandStateL = IsTryingToGrab(palmBone, Hand.Left) ? HandState.Grabbing : _currentHandStateL;

                if (_potentialGrabItemL != null && _grabbedItemL == null
                    && _currentHandStateL == HandState.Grabbing && _previousHandStateL != HandState.Grabbing)
                {
                    _potentialGrabItemL.transform.parent = _colliderObjL.transform;
                    _grabbedItemL = _potentialGrabItemL;
                    //_currentHandStateL = HandState.Holding;

                    Rigidbody rigidCmp = _grabbedItemL.GetComponent<Rigidbody>();
                    if (rigidCmp != null)
                    {
                        rigidCmp.useGravity = false;
                        rigidCmp.isKinematic = true;
                        rigidCmp.velocity = Vector3.zero;
                    }
                }
                //Dropping logic
                else
                {
                    //if (_previousHandStateL == HandState.Holding)
                    //    _currentHandStateL = IsTryingToDrop(palmBone, Hand.Left) ? HandState.Dropping : _currentHandStateL;

                    if (_grabbedItemL != null && _currentHandStateL == HandState.Dropping &&
                        _previousHandStateL != HandState.Dropping)
                    {
                        if (_grabbedItemL != _grabbedItemR)
                        {
                            _grabbedItemL.transform.parent = null;
                            Rigidbody rigidCmp = _grabbedItemL.GetComponent<Rigidbody>();
                            if (rigidCmp != null)
                            {
                                rigidCmp.useGravity = true;
                                rigidCmp.isKinematic = false;
                            }
                        }

                        //_currentHandStateL = HandState.Neutral;
                        _grabbedItemL = null;
                    }
                }

                //if (_currentHandStateL is HandState.Grabbing or HandState.Dropping)
                //    _currentHandStateL = HandState.Neutral;

                _previousHandStateL = _currentHandStateL;
            }
            #endregion

            #region RightHandLogic
            if (_enabledR)
            {
                var palmBone = _boneManager.GetBoneFromHandById(Hand.Right, XRHandJointID.Palm);
                _colliderObjR.transform.position = palmBone.BonePosition;
                _colliderObjR.transform.rotation = palmBone.BoneRotation;

                _currentHandStateR = GetCurrentHandState(palmBone, Hand.Right);


                //Grabbing logic
                //if (_previousHandStateR == HandState.Neutral)
                //    _currentHandStateR = IsTryingToGrab(palmBone, Hand.Right) ? HandState.Grabbing : _currentHandStateR;

                if (_potentialGrabItemR != null && _grabbedItemR == null
                    && _currentHandStateR == HandState.Grabbing && _previousHandStateR != HandState.Grabbing)
                {
                    _potentialGrabItemR.transform.parent = _colliderObjR.transform;
                    _grabbedItemR = _potentialGrabItemR;
                    //_currentHandStateR = HandState.Holding;

                    Rigidbody rigidCmp = _grabbedItemR.GetComponent<Rigidbody>();
                    if (rigidCmp != null)
                    {
                        rigidCmp.useGravity = false;
                        rigidCmp.isKinematic = true;
                        rigidCmp.velocity = Vector3.zero;
                    }
                }
                //Dropping logic
                else
                {
                    //if (_previousHandStateR == HandState.Holding)
                    //    _currentHandStateR = IsTryingToDrop(palmBone, Hand.Right) ? HandState.Dropping : _currentHandStateR;

                    if (_grabbedItemR != null && _currentHandStateR == HandState.Dropping &&
                        _previousHandStateR != HandState.Dropping)
                    {
                        if (_grabbedItemL != _grabbedItemR)
                        {
                            _grabbedItemR.transform.parent = null;
                            Rigidbody rigidCmp = _grabbedItemR.GetComponent<Rigidbody>();
                            if (rigidCmp != null)
                            {
                                rigidCmp.useGravity = true;
                                rigidCmp.isKinematic = false;
                            }
                        }

                        //_currentHandStateR = HandState.Neutral;
                        _grabbedItemR = null;
                    }
                }

                //if (_currentHandStateR is HandState.Grabbing or HandState.Dropping)
                //    _currentHandStateR = HandState.Neutral;

                _previousHandStateR = _currentHandStateR;
            }
            #endregion
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
        }

        HandState GetCurrentHandState(RhinoxBone palmBone, Hand hand)
        {
            if (hand == Hand.Both)
                return HandState.Neutral;

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


            if (total <= GRABBING_THRESHOLD)
                return HandState.Grabbing;
            else if (total >= DROPPING_THRESHOLD)
                return HandState.Dropping;
            else
                return HandState.Neutral;
        }

        bool IsTryingToGrab(RhinoxBone palmBone, Hand hand)
        {
            if (hand == Hand.Both)
                return false;


            //maybe add and if statement after each total+= for early return?

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


            //maybe add and if statement after each total+= for early return?

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

            //if (total < DROPPING_THRESHOLD)
            if (total < GRABBING_THRESHOLD)
                return false;

            return true;
        }

        public void SetEnabled(bool newState, Hand handedness)
        {
            switch (handedness)
            {
                case Hand.Left:
                    _enabledL = newState;
                    _ColliderL.enabled = newState;
                    break;
                case Hand.Right:
                    _enabledR = newState;
                    _ColliderR.enabled = newState;
                    break;
                case Hand.Both:
                    _enabledL = newState;
                    _ColliderL.enabled = newState;
                    _enabledR = newState;
                    _ColliderR.enabled = newState;
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