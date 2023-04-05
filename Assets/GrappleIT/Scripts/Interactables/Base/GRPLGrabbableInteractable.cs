using Codice.ThemeImages;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLGrabbableInteractable : GRPLInteractable
    {
        public bool IsGrabbed { get; protected set; } = false;

        protected Transform _previousParentTransform = null;
        protected Rigidbody _rigidBody = null;

        protected bool _wasKinematic;
        protected bool _hadGravity;

        protected bool _isValid = true;
        protected RhinoxHand _currentHandHolding = RhinoxHand.Invalid;
        //protected bool _canBeGrabbed = false;

        protected bool _canHandGrabL = false;
        protected bool _canHandGrabR = false;


        private Bounds _bounds;
        private RhinoxGesture _grabGesture;
        private static GRPLJointManager _jointManager;

        private const float _boundsIncreasment = 1.35f;

        protected override void OnEnable()
        {
            base.OnEnable();

            GRPLJointManager.GlobalInitialized += JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;

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

            if (_grabGesture != null)
            {
                _grabGesture.RemoveListenerOnRecognized(TryGrab);
                _grabGesture.RemoveListenerOnUnRecognized(TryDrop);
            }
        }

        private void JointManagerInitialized(GRPLJointManager jointManager)
        {
            if (_jointManager == null)
                _jointManager = jointManager;
        }

        //getting the grab gesture and linking events
        private void GestureRecognizerInitialized(GRPLGestureRecognizer gestureRecognizer)
        {
            if (_grabGesture == null)
            {
                _grabGesture = gestureRecognizer.Gestures.Find(x => x.Name == "Grab");
                if (_grabGesture != null)
                {
                    _grabGesture.AddListenerOnRecognized(TryGrab);
                    _grabGesture.AddListenerOnUnRecognized(TryDrop);
                }
            }
            //else
            //{
            //    _grabGesture.AddListenerOnRecognized(TryGrab);
            //    _grabGesture.AddListenerOnUnRecognized(TryDrop);
            //}
        }

        //protected override void ProximityStarted()
        //{
        //    base.ProximityStarted();
        //}

        //protected override void ProximityStopped()
        //{
        //    base.ProximityStopped();
        //}

        protected override void Initialize()
        {
            base.Initialize();

            _bounds = gameObject.GetObjectBounds();

            //TODO: Refactor this
            _bounds.extents *= _boundsIncreasment;//increase bouds a bit with magic number

            if (TryGetComponent(out _rigidBody))
            {
                _wasKinematic = _rigidBody.isKinematic;
                _hadGravity = _rigidBody.useGravity;
                _previousParentTransform = transform.parent;
            }
            else
                _isValid = false;
        }

        private void Update()
        {
            _bounds = gameObject.GetObjectBounds();
            _bounds.extents *= _boundsIncreasment;
        }

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
                        GrabInternal(_jointManager.LeftHandParentObj, RhinoxHand.Left);
                        break;
                    case RhinoxHand.Right:
                        GrabInternal(_jointManager.RightHandParentObj, RhinoxHand.Right);
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

        //save and change the rigidbody settings so it can properly move along with the handand it is now attached to
        public virtual void GrabInternal(GameObject parent, RhinoxHand rhinoxHand)
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
        public virtual void DropInternal()
        {
            if (!_isValid)
                return;

            _rigidBody.isKinematic = _wasKinematic;
            _rigidBody.useGravity = _hadGravity;

            gameObject.transform.parent = _previousParentTransform;

            IsGrabbed = false;
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    _canHandGrabL = _bounds.Contains(joint.JointPosition);
                    break;
                case RhinoxHand.Right:
                    _canHandGrabR = _bounds.Contains(joint.JointPosition);
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }

            if (!IsGrabbed)
                return false;
            else
            {

                //PLog.Info<GRPLITLogger>("GRABBED", this);
                return true;
            }
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = joints.FirstOrDefault(x => x.JointID == XRHandJointID.MiddleProximal);
            return outJoint != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
        }
    }
}