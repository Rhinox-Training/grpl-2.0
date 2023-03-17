using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class PhysicsSocketService : IPhysicsService
    {
        public UnityEvent<Hand, GameObject> OnObjectGrabbed = new();
        public UnityEvent<Hand, GameObject> OnObjectDropped = new();

        public UnityEvent<Hand> OnGrabStarted = new();
        public UnityEvent<Hand> OnGrabEnded = new();

        public float ColliderActivationDelay = 1.0f;



        private bool _isInitialized = false;
        private JointManager _jointManager;
        private GestureRecognizer _gestureRecognizer;

        private RhinoxGesture _grabGesture;

        private GameObject _colliderObjL;
        private GameObject _colliderObjR;
        private BoxCollider _ColliderL;
        private BoxCollider _ColliderR;

        private GameObject _socketObjL;
        private GameObject _socketObjR;

        private readonly Vector3 _handSocketAndColliderOffset = new Vector3(0f, -0.03f, 0.025f);//magic numbers got from testing
        private readonly Vector3 _colliderSize = new Vector3(0.065f, 0.045f, 0.08f);//magic numbers got from testing

        private bool _enabledL = true;
        private bool _enabledR = true;

        private GameObject _potentialGrabItemL = null;
        private GameObject _potentialGrabItemR = null;
        private GameObject _grabbedItemL = null;
        private GameObject _grabbedItemR = null;

        public PhysicsSocketService(JointManager jointManager, GestureRecognizer gestureRecognizer, GameObject parentObject)
        {
            _jointManager = jointManager;
            _gestureRecognizer = gestureRecognizer;

            //creates gameobjects that follow each hand and have a collider on them to know if an object is in grabbing range.
            //under these objects are another empty gameobject that is the socket where items get childed to and get re-oriented to when they need to get socketed into the hand.
            if (parentObject != null)
            {
                #region Left Hand Creation and Init
                _colliderObjL = new GameObject("Collider Obj LEFT");
                _colliderObjL.transform.parent = parentObject.transform;

                _socketObjL = new GameObject("Socket Left");
                _socketObjL.transform.parent = _colliderObjL.transform;
                //needs to be rotate 90�, otherwise object would go through handpalm and this one is rotate antoher 180�, because it's the opposite of left hand
                _socketObjL.transform.SetLocalPositionAndRotation(_handSocketAndColliderOffset, Quaternion.Euler(0f, 0f, 270f));

                var colliderEventsL = _colliderObjL.AddComponent<PhysicsEventHandler>();
                colliderEventsL.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsL.Hand = Hand.Left;

                _ColliderL = _colliderObjL.AddComponent<BoxCollider>();
                _ColliderL.isTrigger = true;
                _ColliderL.enabled = false;
                _ColliderL.center = _handSocketAndColliderOffset;
                _ColliderL.size = _colliderSize;
                #endregion

                #region Right Hand Creating and Init
                _colliderObjR = new GameObject("Collider Obj RIGHT");
                _colliderObjR.transform.parent = parentObject.transform;

                _socketObjR = new GameObject("Socket Right");
                _socketObjR.transform.parent = _colliderObjR.transform;
                //needs to be rotate 90�, otherwise object would go through handpalm.
                _socketObjR.transform.SetLocalPositionAndRotation(_handSocketAndColliderOffset, Quaternion.Euler(0f, 0f, 90f));

                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.Hand = Hand.Right;

                _ColliderR = _colliderObjR.AddComponent<BoxCollider>();
                _ColliderR.enabled = false;
                _ColliderR.isTrigger = true;
                _ColliderR.center = _handSocketAndColliderOffset;
                _ColliderR.size = _colliderSize;
                #endregion
            }
        }

        ~PhysicsSocketService()
        {
            if (!_jointManager.AreJointsInitialised && !_isInitialized)
                return;

            _jointManager.TrackingAcquired -= TrackingAcquired;
            _jointManager.TrackingLost -= TrackingLost;

            // Add these function ass listeners
            // This assured that the grabbing of objects and hand colliders don't have weird behaviour
            OnGrabStarted.RemoveListener(_jointManager.DisableHandCollisions);
            OnGrabEnded.RemoveListener(_jointManager.EnableHandCollisionsAfterDelay);

            if (_grabGesture != null)
            {
                _grabGesture.OnRecognized.RemoveAllListeners();
                _grabGesture.OnUnrecognized.RemoveAllListeners();
            }

            var colliderEventsL = _colliderObjL.GetComponent<PhysicsEventHandler>();
            if (colliderEventsL != null)
            {
                colliderEventsL.EnterEvent.RemoveListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.RemoveListener(OnHandTriggerExit);
            }

            var colliderEventsR = _colliderObjR.GetComponent<PhysicsEventHandler>();
            if (colliderEventsR != null)
            {
                colliderEventsR.EnterEvent.RemoveListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.RemoveListener(OnHandTriggerExit);
            }
        }

        public void TryInitialize()
        {
            if (!_jointManager.AreJointsInitialised && !_isInitialized)
                return;

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;


            // Add these function as listeners
            // This assured that the grabbing of objects and hand colliders don't have weird behaviour
            OnGrabStarted.AddListener(_jointManager.DisableHandCollisions);
            OnGrabEnded.AddListener(_jointManager.EnableHandCollisionsAfterDelay);

            _jointManager.ColliderActivationDelay = ColliderActivationDelay;

            //getting the grab gesture and linking events
            if (_grabGesture.Name == null)
            {
                _grabGesture = _gestureRecognizer.Gestures.Find(x => x.Name == "Grab");
                if (_grabGesture.Name != null)
                {
                    _grabGesture.OnRecognized.AddListener(TryGrab);
                    _grabGesture.OnUnrecognized.AddListener(TryDrop);
                }
            }

            _ColliderL.enabled = true;
            _ColliderR.enabled = true;

            _isInitialized = true;
        }

        public bool GetInitialised()
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

            if (_enabledL)
            {
                //updating the obj with the is-in-grabbing reach collider
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, Hand.Left, out var palmBone);
                _colliderObjL.transform.position = palmBone.JointPosition;
                _colliderObjL.transform.rotation = palmBone.JointRotation;
            }

            if (_enabledR)
            {
                //updating the obj with the is-in-grabbing reach collider
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, Hand.Right, out var palmBone);
                _colliderObjR.transform.position = palmBone.JointPosition;
                _colliderObjR.transform.rotation = palmBone.JointRotation;
            }
        }

        #region Grab & Drop Logic
        public void TryGrab(Hand hand)
        {
            if (!_enabledL && !_enabledR)
                return;

            switch (hand)
            {
                case Hand.Left:
                    if (_enabledL)
                        TryGrabInternal(Hand.Left, _potentialGrabItemL, ref _grabbedItemL, ref _grabbedItemR, _socketObjL);
                    break;
                case Hand.Right:
                    if (_enabledR)
                        TryGrabInternal(Hand.Right, _potentialGrabItemR, ref _grabbedItemR, ref _grabbedItemL, _socketObjR);
                    break;
                case Hand.Both:
                    TryGrabInternal(Hand.Left, _potentialGrabItemL, ref _grabbedItemL, ref _grabbedItemR, _socketObjL);
                    TryGrabInternal(Hand.Right, _potentialGrabItemR, ref _grabbedItemR, ref _grabbedItemL, _socketObjR);
                    break;
                case Hand.Invalid:
                default:
                    Debug.LogError(
                        $"{nameof(PhysicsSocketService)} - {nameof(TryGrab)}, function called with incorrect hand {hand}. Only left, right or both is supported!");
                    break;
            }
        }

        public void TryDrop(Hand hand)
        {
            if (!_enabledL && !_enabledR)
                return;

            switch (hand)
            {
                case Hand.Left:
                    if (_enabledL)
                        TryDropInternal(Hand.Left, ref _grabbedItemL, _grabbedItemR);
                    break;
                case Hand.Right:
                    if (_enabledR)
                        TryDropInternal(Hand.Right, ref _grabbedItemR, _grabbedItemL);
                    break;
                case Hand.Both:
                    TryDropInternal(Hand.Left, ref _grabbedItemL, _grabbedItemR);
                    TryDropInternal(Hand.Right, ref _grabbedItemR, _grabbedItemL);
                    break;
                case Hand.Invalid:
                default:
                    Debug.LogError(
                        $"{nameof(PhysicsSocketService)} - {nameof(TryDrop)}, function called with incorrect hand {hand}. Only left, right or both is supported!");
                    break;
            }
        }

        private void TryGrabInternal(Hand hand, GameObject potentialGrabItem, ref GameObject grabbedItemCurrentHand, ref GameObject grabbedItemOtherHand, GameObject socket)
        {
            if (potentialGrabItem != null && grabbedItemCurrentHand == null)
            {
                //switch hand if the potential object is currently grabbed by the other hand
                if (potentialGrabItem == grabbedItemOtherHand)
                {
                    grabbedItemOtherHand.GetComponent<GRPLBaseInteractable>().Dropped();
                    OnObjectDropped.Invoke(hand, grabbedItemOtherHand);
                    OnGrabEnded.Invoke(hand);
                    grabbedItemOtherHand = null;
                }

                potentialGrabItem.GetComponent<GRPLBaseInteractable>().Grabbed(socket, hand);

                grabbedItemCurrentHand = potentialGrabItem;

                OnObjectGrabbed.Invoke(hand, grabbedItemCurrentHand);
                OnGrabStarted.Invoke(hand);
            }
        }

        private void TryDropInternal(Hand hand, ref GameObject grabbedItemCurrentHand, GameObject grabbedItemOtherHand)
        {
            if (grabbedItemCurrentHand != null)
            {
                if (grabbedItemCurrentHand != grabbedItemOtherHand)
                {
                    grabbedItemCurrentHand.GetComponent<GRPLBaseInteractable>().Dropped();

                    OnObjectDropped.Invoke(hand, grabbedItemCurrentHand);
                    OnGrabEnded.Invoke(hand);
                }

                grabbedItemCurrentHand = null;
            }
        }
        #endregion

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