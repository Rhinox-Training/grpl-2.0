using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class PhysicsSocketService : IPhysicsService
    {
        public UnityEvent<RhinoxHand, GameObject> OnObjectGrabbed = new();
        public UnityEvent<RhinoxHand, GameObject> OnObjectDropped = new();

        public UnityEvent<RhinoxHand> OnGrabStarted = new();
        public UnityEvent<RhinoxHand> OnGrabEnded = new();

        public float ColliderActivationDelay = 1.0f;



        private bool _isInitialized = false;
        private GRPLJointManager _jointManager;
        private GestureRecognizer _gestureRecognizer;

        private RhinoxGesture _grabGesture;

        private GameObject _colliderObjL;
        private GameObject _colliderObjR;
        private BoxCollider _colliderL;
        private BoxCollider _colliderR;

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

        public PhysicsSocketService(GestureRecognizer gestureRecognizer, GameObject parentObject)
        {
            GRPLJointManager.GlobalInitialized += Initialize;

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

                //needs to be rotate 90°, otherwise object would go through handpalm and this one is rotate antoher 180°, because it's the opposite of left hand
                _socketObjL.transform.SetLocalPositionAndRotation(_handSocketAndColliderOffset, Quaternion.Euler(0f, 0f, 270f));

                var colliderEventsL = _colliderObjL.AddComponent<PhysicsEventHandler>();
                colliderEventsL.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsL.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsL.RhinoxHand = RhinoxHand.Left;

                _colliderL = _colliderObjL.AddComponent<BoxCollider>();
                _colliderL.isTrigger = true;
                _colliderL.enabled = false;
                _colliderL.center = _handSocketAndColliderOffset;
                _colliderL.size = _colliderSize;
                #endregion

                #region Right Hand Creating and Init
                _colliderObjR = new GameObject("Collider Obj RIGHT");
                _colliderObjR.transform.parent = parentObject.transform;


                _socketObjR = new GameObject("Socket Right");
                _socketObjR.transform.parent = _colliderObjR.transform;

                //needs to be rotate 90°, otherwise object would go through handpalm.
                _socketObjR.transform.SetLocalPositionAndRotation(_handSocketAndColliderOffset, Quaternion.Euler(0f, 0f, 90f));

                var colliderEventsR = _colliderObjR.AddComponent<PhysicsEventHandler>();
                colliderEventsR.EnterEvent.AddListener(OnHandTriggerEnter);
                colliderEventsR.ExitEvent.AddListener(OnHandTriggerExit);
                colliderEventsR.RhinoxHand = RhinoxHand.Right;

                _colliderR = _colliderObjR.AddComponent<BoxCollider>();
                _colliderR.enabled = false;
                _colliderR.isTrigger = true;
                _colliderR.center = _handSocketAndColliderOffset;
                _colliderR.size = _colliderSize;
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
                _grabGesture.RemoveListenerOnRecognized(TryGrab);// OnRecognized.RemoveAllListeners();
                _grabGesture.RemoveListenerOnUnRecognized(TryDrop);// .OnUnrecognized.RemoveAllListeners();
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

        public void Initialize(GRPLJointManager jointManager)
        {
            if (_isInitialized || jointManager == null)
                return;
            _jointManager = jointManager;
            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;

            _colliderObjL.layer = _jointManager.HandLayer;
            _socketObjL.layer = _jointManager.HandLayer;
            _colliderObjR.layer = _jointManager.HandLayer;
            _socketObjR.layer = _jointManager.HandLayer;

            // Add these function as listeners
            // This assured that the grabbing of objects and hand colliders don't have weird behaviour
            OnGrabStarted.AddListener(_jointManager.DisableHandCollisions);
            OnGrabEnded.AddListener(_jointManager.EnableHandCollisionsAfterDelay);

            _jointManager.ColliderActivationDelay = ColliderActivationDelay;

            //getting the grab gesture and linking events
            if (_grabGesture == null)
            {
                _grabGesture = _gestureRecognizer.Gestures.Find(x => x.Name == "Grab");
                if (_grabGesture != null)
                {
                    _grabGesture.AddListenerOnRecognized(TryGrab);// .OnRecognized.AddListener(TryGrab);
                    _grabGesture.AddListenerOnUnRecognized(TryDrop);// .OnUnrecognized.AddListener(TryDrop);
                }
            }

            _colliderL.enabled = true;
            _colliderR.enabled = true;

            _isInitialized = true;
        }

        public bool IsInitialized()
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
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, RhinoxHand.Left, out var palmBone);
                _colliderObjL.transform.position = palmBone.JointPosition;
                _colliderObjL.transform.rotation = palmBone.JointRotation;
            }

            if (_enabledR)
            {
                //updating the obj with the is-in-grabbing reach collider
                _jointManager.TryGetJointFromHandById(XRHandJointID.Palm, RhinoxHand.Right, out var palmBone);
                _colliderObjR.transform.position = palmBone.JointPosition;
                _colliderObjR.transform.rotation = palmBone.JointRotation;
            }
        }

        #region Grab & Drop Logic
        public void TryGrab(RhinoxHand hand)
        {
            if (!_enabledL && !_enabledR)
                return;

            switch (hand)
            {
                case RhinoxHand.Left:
                    if (_enabledL)
                        TryGrabInternal(RhinoxHand.Left, _potentialGrabItemL, ref _grabbedItemL, ref _grabbedItemR, _socketObjL);
                    break;
                case RhinoxHand.Right:
                    if (_enabledR)
                        TryGrabInternal(RhinoxHand.Right, _potentialGrabItemR, ref _grabbedItemR, ref _grabbedItemL, _socketObjR);
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLITLogger>($"{nameof(PhysicsSocketService)} - {nameof(TryGrab)}, " +
                        $"function called with incorrect Hand {hand}. Only left or right is supported!");
                    break;
            }
        }

        public void TryDrop(RhinoxHand hand)
        {
            if (!_enabledL && !_enabledR)
                return;

            switch (hand)
            {
                case RhinoxHand.Left:
                    if (_enabledL)
                        TryDropInternal(RhinoxHand.Left, ref _grabbedItemL, _grabbedItemR);
                    break;
                case RhinoxHand.Right:
                    if (_enabledR)
                        TryDropInternal(RhinoxHand.Right, ref _grabbedItemR, _grabbedItemL);
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLITLogger>($"{nameof(PhysicsSocketService)} - {nameof(TryDrop)}, " +
                        $"function called with incorrect hand {hand}. Only left or right is supported!");
                    break;
            }
        }

        private void TryGrabInternal(RhinoxHand hand, GameObject potentialGrabItem, ref GameObject grabbedItemCurrentHand, ref GameObject grabbedItemOtherHand, GameObject socket)
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

        private void TryDropInternal(RhinoxHand hand, ref GameObject grabbedItemCurrentHand, GameObject grabbedItemOtherHand)
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
        public void OnHandTriggerEnter(GameObject triggerObj, GameObject otherObj, RhinoxHand hand)
        {
            var grplInteractableCmp = otherObj.GetComponent<GRPLBaseInteractable>();
            if (grplInteractableCmp == null)
                return;

            switch (hand)
            {
                case RhinoxHand.Left:
                    _potentialGrabItemL = otherObj;
                    break;
                case RhinoxHand.Right:
                    _potentialGrabItemR = otherObj;
                    break;
                default:
                    break;
            }
        }

        public void OnHandTriggerExit(GameObject triggerObj, GameObject otherObj, RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    if (_potentialGrabItemL == otherObj)
                        _potentialGrabItemL = null;
                    break;
                case RhinoxHand.Right:
                    if (_potentialGrabItemR == otherObj)
                        _potentialGrabItemR = null;
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region State Logic
        private void TrackingAcquired(RhinoxHand hand) => SetHandEnabled(true, hand);

        private void TrackingLost(RhinoxHand hand) => SetHandEnabled(false, hand);

        public void SetHandEnabled(bool newState, RhinoxHand handedness)
        {
            switch (handedness)
            {
                case RhinoxHand.Left:
                    _enabledL = newState;
                    _colliderL.enabled = newState;
                    break;
                case RhinoxHand.Right:
                    _enabledR = newState;
                    _colliderR.enabled = newState;
                    break;
            }
        }

        public bool IsHandEnabled(RhinoxHand handedness)
        {
            switch (handedness)
            {
                case RhinoxHand.Left:
                    return _enabledL;
                case RhinoxHand.Right:
                    return _enabledR;
                default:
                    return false;
            }
        }
        #endregion
    }
}