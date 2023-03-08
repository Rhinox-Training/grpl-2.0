using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public sealed class PhysicsSocketService : IPhysicsService
    {
        enum HandState
        {
            Neutral,
            Grabbing,
            Dropping
        }

        private bool _isInitialized = false;
        private BoneManager _boneManager;

        private GameObject _colliderObjL;
        private GameObject _colliderObjR;
        private BoxCollider _ColliderL;
        private BoxCollider _ColliderR;

        private GameObject _SocketObjL;
        private GameObject _SocketObjR;

        private bool _enabledL = true;
        private bool _enabledR = true;

        private HandState _currentHandStateL = HandState.Neutral;
        private HandState _previousHandStateL = HandState.Neutral;
        private HandState _currentHandStateR = HandState.Neutral;
        private HandState _previousHandStateR = HandState.Neutral;

        private const float GRABBING_THRESHOLD = 0.05f;
        private const float DROPPING_THRESHOLD = 0.065f;//prev: 0.072


        private GameObject _potentialGrabItemL = null;
        private GameObject _potentialGrabItemR = null;
        private GameObject _grabbedItemL = null;
        private GameObject _grabbedItemR = null;

        private Vector3 HandSocketPositionalOffset = Vector3.zero;
        private Quaternion HandSocketRotationalOffset = Quaternion.identity;

        public PhysicsSocketService(BoneManager boneManager, GameObject parentObject, Vector3 positionalOffset, Quaternion rotationalOffset)
        {
            _boneManager = boneManager;
            HandSocketPositionalOffset = positionalOffset;
            HandSocketRotationalOffset = rotationalOffset;

            if (parentObject != null)
            {
                _colliderObjL = new GameObject("Collider Obj LEFT");
                _colliderObjL.transform.parent = parentObject.transform;

                _SocketObjL = new GameObject("Socket Left");
                _SocketObjL.transform.parent = _colliderObjL.transform;
                //_SocketObjL.transform.SetLocalPositionAndRotation(new(0f, -0.0425f, 0.0335f), Quaternion.Euler(-10f, 35f, 90f));
                _SocketObjL.transform.SetLocalPositionAndRotation(new(0f, -0.0425f, 0.0335f), Quaternion.Euler(10f, 145f, 90f));
                //_SocketObjL.transform.SetLocalPositionAndRotation(new(0f, -0.0425f, 0.0335f), Quaternion.Euler(-10f, 145f, 90f));

                var colliderEventsL = _colliderObjL.AddComponent<PhysicsEventHandler>();
                colliderEventsL.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsL.Hand = Hand.Left;

                _ColliderL = _colliderObjL.AddComponent<BoxCollider>();
                _ColliderL.isTrigger = true;
                _ColliderL.enabled = false;
                _ColliderL.center = new(0f, -0.03f, 0.0225f);
                _ColliderL.size = new(0.06f, 0.035f, 0.07f);

                _colliderObjR = new GameObject("Collider Obj RIGHT");
                _colliderObjR.transform.parent = parentObject.transform;

                _SocketObjR = new GameObject("Socket Right");
                _SocketObjR.transform.parent = _colliderObjR.transform;
                _SocketObjR.transform.SetLocalPositionAndRotation(new(0f, -0.0425f, 0.0335f), Quaternion.Euler(-10f, 35f, 90f));

                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.Hand = Hand.Right;

                _ColliderR = _colliderObjR.AddComponent<BoxCollider>();
                _ColliderR.enabled = false;
                _ColliderR.isTrigger = true;
                _ColliderR.center = new(0f, -0.03f, 0.0225f);
                _ColliderR.size = new(0.06f, 0.035f, 0.07f);
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

                if (_potentialGrabItemL != null && _grabbedItemL == null
                    && _currentHandStateL == HandState.Grabbing && _previousHandStateL != HandState.Grabbing)
                {
                    if (_potentialGrabItemL == _grabbedItemR)
                    {
                        _grabbedItemR.GetComponent<GRPLBaseInteractable>().Dropped();
                        _grabbedItemR = null;
                    }

                    _potentialGrabItemL.GetComponent<GRPLBaseInteractable>().Grabbed(_SocketObjL, HandSocketPositionalOffset, HandSocketRotationalOffset);

                    _grabbedItemL = _potentialGrabItemL;
                }
                //Dropping logic
                else
                {
                    if (_grabbedItemL != null && _currentHandStateL == HandState.Dropping &&
                        _previousHandStateL != HandState.Dropping)
                    {
                        if (_grabbedItemL != _grabbedItemR)
                        {
                            _grabbedItemL.GetComponent<GRPLBaseInteractable>().Dropped();
                        }

                        _grabbedItemL = null;
                    }
                }

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

                //Grabbing logics
                if (_potentialGrabItemR != null && _grabbedItemR == null
                    && _currentHandStateR == HandState.Grabbing && _previousHandStateR != HandState.Grabbing)
                {
                    if (_potentialGrabItemR == _grabbedItemL)
                    {
                        _grabbedItemL.GetComponent<GRPLBaseInteractable>().Dropped();
                        _grabbedItemL = null;
                    }

                    _potentialGrabItemR.GetComponent<GRPLBaseInteractable>().Grabbed(_SocketObjR, HandSocketPositionalOffset, HandSocketRotationalOffset);

                    _grabbedItemR = _potentialGrabItemR;

                }
                //Dropping logic
                else
                {
                    if (_grabbedItemR != null && _currentHandStateR == HandState.Dropping &&
                        _previousHandStateR != HandState.Dropping)
                    {
                        if (_grabbedItemL != _grabbedItemR)
                        {
                            _grabbedItemR.GetComponent<GRPLBaseInteractable>().Dropped();
                        }

                        _grabbedItemR = null;
                    }
                }
                _previousHandStateR = _currentHandStateR;
            }
            #endregion
        }

        public void OnHandTriggerEnter(GameObject triggerObj, GameObject otherObj, Hand hand)
        {
            var grplInteractableCmp = otherObj.GetComponent<GRPLBaseInteractable>();
            if (grplInteractableCmp == null)
                return;

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