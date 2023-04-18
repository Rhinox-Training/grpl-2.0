using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// Base class to make an object grabbele, this can be via a bounding box or via a list of trigge rcolliders used as bounding box.
    /// </summary>
    /// <remarks />
    /// <dependencies><see cref="GRPLGestureRecognizer"/></dependencies>
    public class GRPLGrabbableBase : GRPLInteractable
    {
        [Space(10f)]
        [Header("Grab parameters")]
        [SerializeField] private string _grabGestureName = "Grab";

        [Header("Bounding box settings")]
        [SerializeField] private bool _useCollidersInsteadOfBoundingBox = false;

        [SerializeField]
        [HideIfField(true, nameof(_useCollidersInsteadOfBoundingBox))]
        private bool _showBoundingBox;
        [SerializeField]
        [HideIfField(true, nameof(_useCollidersInsteadOfBoundingBox))]
        private Vector3 _boundingBoxExtensionValues = new Vector3(0.5f, 0.5f, 0.5f);

        [SerializeField]//TODO: ASK SENSIORS
        [HideIfField(false, nameof(_useCollidersInsteadOfBoundingBox))]
        private List<Collider> _boundColliders;

        public bool IsGrabbed { get; protected set; } = false;

        protected Transform _previousParentTransform = null;
        protected Rigidbody _rigidBody = null;

        protected bool _wasKinematic;
        protected bool _hadGravity;

        protected bool _isValid = true;
        protected RhinoxHand _currentHandHolding = RhinoxHand.Invalid;

        protected bool _canHandGrabL = false;
        protected bool _canHandGrabR = false;

        private Bounds _bounds;
        private RhinoxGesture _grabGesture;
        private static GRPLJointManager _jointManager;

        //==========
        //INITIALIZE
        //==========
        protected void Awake()
        {
            _forceInteractibleJoint = true;
            _forcedInteractJointID = XRHandJointID.MiddleProximal;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (_useCollidersInsteadOfBoundingBox)
            {
                int tempCnt = _boundColliders.Count;
                _boundColliders.RemoveAll(s => s == null);
                if (_boundColliders.Count != tempCnt)
                    PLog.Warn<GRPLITLogger>($"[{GetType()}:Initialize], " +
                        $"Bound Colliders list had empties and have been purged", this);

                foreach (var col in _boundColliders)
                {
                    if (!col.isTrigger)
                    {
                        col.isTrigger = true;
                        PLog.Warn<GRPLITLogger>($"[{GetType()}:Initialize], " +
                            $"Collider {col.name} was not a trigger, has been set to trigger.", this);
                    }
                }
            }
            else
            {
                _bounds = gameObject.GetObjectBounds();
                _bounds.extents += _boundingBoxExtensionValues;
            }

            if (TryGetComponent(out _rigidBody))
            {
                _wasKinematic = _rigidBody.isKinematic;
                _hadGravity = _rigidBody.useGravity;
                _previousParentTransform = transform.parent;
            }
            else
                _isValid = false;
        }

        private void JointManagerInitialized(GRPLJointManager jointManager)
        {
            if (_jointManager == null)
            {
                _jointManager = jointManager;

                //No need to do something on TrackingAquired
                _jointManager.TrackingLost += TrackingLost;
            }
        }

        //getting the grab gesture and linking events
        private void GestureRecognizerInitialized(GRPLGestureRecognizer gestureRecognizer)
        {
            if (_grabGesture == null)
            {
                _grabGesture = gestureRecognizer.Gestures.Find(x => x.Name == _grabGestureName);

                if (_grabGesture != null)
                {
                    _grabGesture.AddListenerOnRecognized(TryGrab);
                    _grabGesture.AddListenerOnUnRecognized(TryDrop);
                }
            }
        }

        //============
        //STATE CHANGE
        //============
        private void TrackingLost(RhinoxHand hand)
        {
            TryDrop(hand);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            GRPLJointManager.GlobalInitialized += JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;

            if (_jointManager != null)
            {
                //No need to do something on TrackingAquired
                _jointManager.TrackingLost += TrackingLost;
            }

            if (_grabGesture != null)
            {
                _grabGesture.AddListenerOnRecognized(TryGrab);
                _grabGesture.AddListenerOnUnRecognized(TryDrop);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            GRPLJointManager.GlobalInitialized -= JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized -= GestureRecognizerInitialized;

            if (_jointManager != null)
            {
                //No need to do something on TrackingAquired
                _jointManager.TrackingLost -= TrackingLost;
            }

            if (_grabGesture != null)
            {
                _grabGesture.RemoveListenerOnRecognized(TryGrab);
                _grabGesture.RemoveListenerOnUnRecognized(TryDrop);
            }
        }


        private void Update()
        {
            _bounds = gameObject.GetObjectBounds();
            _bounds.extents += _boundingBoxExtensionValues;
        }

        //=========
        //OVERRIDES
        //=========
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    if (_useCollidersInsteadOfBoundingBox)
                        _canHandGrabL = _boundColliders.Any(col => col.bounds.Contains(joint.JointPosition));
                    else
                        _canHandGrabL = _bounds.Contains(joint.JointPosition);
                    break;
                case RhinoxHand.Right:
                    if (_useCollidersInsteadOfBoundingBox)
                        _canHandGrabR = _boundColliders.Any(col => col.bounds.Contains(joint.JointPosition));
                    else
                        _canHandGrabR = _bounds.Contains(joint.JointPosition);
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }

            return IsGrabbed;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint,
            RhinoxHand hand)
        {
            outJoint = joints.FirstOrDefault(x => x.JointID == _forcedInteractJointID);
            return outJoint != null;
        }

        //==============
        //PUBLIC METHODS
        //==============
        public void TryGrab(RhinoxHand hand)
        {
            //if the given hand was invalid or the given hand cannot grab this object, do early return
            if (hand == RhinoxHand.Invalid ||
                hand == RhinoxHand.Left && !_canHandGrabL ||
                hand == RhinoxHand.Right && !_canHandGrabR)
                return;

            //if the object is being held by the same hand that is trying to grab it again,
            //then nothing should happen.
            if (_currentHandHolding != hand)
            {
                //if trying to swap hands
                if (_currentHandHolding != RhinoxHand.Invalid && hand == _currentHandHolding.GetInverse())
                {
                    DropInternal();
                }

                switch (hand)
                {
                    case RhinoxHand.Left:
                        GrabInternal(_jointManager.LeftHandSocket, RhinoxHand.Left);
                        break;
                    case RhinoxHand.Right:
                        GrabInternal(_jointManager.RightHandSocket, RhinoxHand.Right);
                        break;
                    default:
                        break;
                }

                _currentHandHolding = hand;
            }
        }

        public void TryDrop(RhinoxHand hand)
        {
            if (_currentHandHolding == hand)
            {
                DropInternal();
                _currentHandHolding = RhinoxHand.Invalid;
            }
        }

        //=================
        //PROTECTED METHODS
        //=================
        //save and change the rigidbody settings so it can properly move along with the handand it is now attached to
        protected virtual void GrabInternal(GameObject parent, RhinoxHand rhinoxHand)
        {
            if (!_isValid)
                return;

            _wasKinematic = _rigidBody.isKinematic;
            _hadGravity = _rigidBody.useGravity;

            _rigidBody.isKinematic = true;
            _rigidBody.useGravity = false;
            _previousParentTransform = transform.parent;

            gameObject.transform.parent = parent.transform;

            IsGrabbed = true;
        }

        //reinstate the changed rigidbody settings
        protected virtual void DropInternal()
        {
            if (!_isValid)
                return;

            _rigidBody.isKinematic = _wasKinematic;
            _rigidBody.useGravity = _hadGravity;

            gameObject.transform.parent = _previousParentTransform;

            IsGrabbed = false;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!_useCollidersInsteadOfBoundingBox && _showBoundingBox)
            {
                _bounds = gameObject.GetObjectBounds();

                _bounds.extents += _boundingBoxExtensionValues;

                Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            }
        }
#endif
    }
}