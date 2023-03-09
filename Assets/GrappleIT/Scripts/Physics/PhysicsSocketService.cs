using UnityEngine;
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
        private JointManager _jointManager;

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
        private const float DROPPING_THRESHOLD = 0.062f;//prev: 0.065


        private GameObject _potentialGrabItemL = null;
        private GameObject _potentialGrabItemR = null;
        private GameObject _grabbedItemL = null;
        private GameObject _grabbedItemR = null;

        public PhysicsSocketService(JointManager jointManager, GameObject parentObject)
        {
            _jointManager = jointManager;

            //creates gameobjects that follow each hand and have a collider on them to know if an object is in grabbing range.
            //under these objects are another empty gameobject that is the socket where items get childed to and get re-oriented to when they need to get socketed into the hand.
            if (parentObject != null)
            {
                #region Left Hand Creation and Init
                _colliderObjL = new GameObject("Collider Obj LEFT");
                _colliderObjL.transform.parent = parentObject.transform;

                _SocketObjL = new GameObject("Socket Left");
                _SocketObjL.transform.parent = _colliderObjL.transform;
                _SocketObjL.transform.SetLocalPositionAndRotation(new(0f, -0.03f, 0.025f), Quaternion.Euler(0f, 0f, 270f));

                var colliderEventsL = _colliderObjL.AddComponent<PhysicsEventHandler>();
                colliderEventsL.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsL.Hand = Hand.Left;

                _ColliderL = _colliderObjL.AddComponent<BoxCollider>();
                _ColliderL.isTrigger = true;
                _ColliderL.enabled = false;
                _ColliderL.center = new(0f, -0.03f, 0.0225f);
                _ColliderL.size = new(0.06f, 0.035f, 0.07f);
                #endregion

                #region Right Hand Creating and Init
                _colliderObjR = new GameObject("Collider Obj RIGHT");
                _colliderObjR.transform.parent = parentObject.transform;

                _SocketObjR = new GameObject("Socket Right");
                _SocketObjR.transform.parent = _colliderObjR.transform;
                //_SocketObjR.transform.localScale = new(-1f, 1f, 1f);//flipping X axis
                _SocketObjR.transform.SetLocalPositionAndRotation(new(0f, -0.03f, 0.025f), Quaternion.Euler(0f, 0f, 90f));

                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.Hand = Hand.Right;

                _ColliderR = _colliderObjR.AddComponent<BoxCollider>();
                _ColliderR.enabled = false;
                _ColliderR.isTrigger = true;
                _ColliderR.center = new(0f, -0.03f, 0.0225f);
                _ColliderR.size = new(0.06f, 0.035f, 0.07f);
                #endregion
            }
        }

        public void TryInitialize()
        {
            if (!_jointManager.IsInitialised)
                return;

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;

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
                //updating the obj with the is-in-grabbing reach collider and caching the palmbone.
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, Hand.Left, out var palmBone);
                _colliderObjL.transform.position = palmBone.JointPosition;
                _colliderObjL.transform.rotation = palmBone.JointRotation;
                ///
                ///Grabbing logic
                ///
                _currentHandStateL = GetCurrentHandState(palmBone, Hand.Left);

                if (_potentialGrabItemL != null && _grabbedItemL == null
                    && _currentHandStateL == HandState.Grabbing && _previousHandStateL != HandState.Grabbing)
                {
                    //switch hand if the left potential object is currently grabbed by the right hand
                    if (_potentialGrabItemL == _grabbedItemR)
                    {
                        _grabbedItemR.GetComponent<GRPLBaseInteractable>().Dropped();
                        _grabbedItemR = null;
                    }

                    _potentialGrabItemL.GetComponent<GRPLBaseInteractable>().Grabbed(_SocketObjL, Hand.Left);

                    _grabbedItemL = _potentialGrabItemL;
                }
                ///
                ///Dropping logic
                ///
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
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, Hand.Right, out var palmBone);
                _colliderObjR.transform.position = palmBone.JointPosition;
                _colliderObjR.transform.rotation = palmBone.JointRotation;

                _currentHandStateR = GetCurrentHandState(palmBone, Hand.Right);

                ///
                ///Grabbing logics
                ///
                if (_potentialGrabItemR != null && _grabbedItemR == null
                    && _currentHandStateR == HandState.Grabbing && _previousHandStateR != HandState.Grabbing)
                {
                    //switch hand if the right potential object is currently grabbed by the left hand
                    if (_potentialGrabItemR == _grabbedItemL)
                    {
                        _grabbedItemL.GetComponent<GRPLBaseInteractable>().Dropped();
                        _grabbedItemL = null;
                    }

                    _potentialGrabItemR.GetComponent<GRPLBaseInteractable>().Grabbed(_SocketObjR, Hand.Right);

                    _grabbedItemR = _potentialGrabItemR;

                }
                ///
                ///Dropping logic
                ///
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

        #region Hand Trigger Logic
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
        #endregion

        #region State Logic
        private void TrackingAcquired(Hand hand) => SetHandEnabled(true, hand);

        private void TrackingLost(Hand hand) => SetHandEnabled(false, hand);

        //checks if hand is doing a grabbing, dropping or neutral motion
        HandState GetCurrentHandState(RhinoxJoint palmJoint, Hand hand)
        {
            if (hand == Hand.Both)
                return HandState.Neutral;

            float total = 0f;
            _jointManager.TryGetJointFromHandById(XRHandJointID.ThumbTip, hand, out var thumbTip);
            total += Vector3.SqrMagnitude(palmJoint.JointPosition - thumbTip.JointPosition);
            _jointManager.TryGetJointFromHandById(XRHandJointID.IndexTip, hand, out var indexTip);
            total += Vector3.SqrMagnitude(palmJoint.JointPosition - indexTip.JointPosition);
            _jointManager.TryGetJointFromHandById(XRHandJointID.MiddleTip, hand, out var middleTip);
            total += Vector3.SqrMagnitude(palmJoint.JointPosition - middleTip.JointPosition);
            _jointManager.TryGetJointFromHandById(XRHandJointID.RingTip, hand, out var ringTip);
            total += Vector3.SqrMagnitude(palmJoint.JointPosition - ringTip.JointPosition);
            _jointManager.TryGetJointFromHandById(XRHandJointID.LittleTip, hand, out var littleTip);
            total += Vector3.SqrMagnitude(palmJoint.JointPosition - littleTip.JointPosition);

            if (total <= GRABBING_THRESHOLD)
                return HandState.Grabbing;
            else if (total >= DROPPING_THRESHOLD)
                return HandState.Dropping;
            else
                return HandState.Neutral;
        }

        public void SetHandEnabled(bool newState, Hand handedness)
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

        public bool GetIsHandEnabled(Hand handedness)
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
        #endregion
    }
}