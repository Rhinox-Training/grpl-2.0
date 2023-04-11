using log4net.Util;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLValve : GRPLInteractable
    {
        [Header("Grab parameters")]
        [SerializeField] private string _grabGestureName = "Grab";
        [SerializeField] private float _grabRadius = .1f;
        [SerializeField] private float _grabTolaranceRadius = .025f;

        [Header("Valve Settings")]
        [SerializeField] float _fullyOpenAngle = 0f;
        [SerializeField] float _fullyClosedAngle = 360f;

        [Header("Gizmos")]
        [SerializeField] bool _drawGizmos = true;

        public bool IsGrabbed { get; protected set; } = false;
        public float CurrentValveRotation => _valveRotation;

        //righty tighty, lefty loosey
        public Action<GRPLValve> OnFullyClosed;
        public Action<GRPLValve> OnFullyOpen;
        public Action<GRPLValve, float> OnValueChanged;


        private float _minRadius;
        private float _maxRadius;
        private float _valveRotation = 180f;

        private RhinoxGesture _grabGesture;
        private static GRPLJointManager _jointManager;
        private bool _canHandGrabL;
        private bool _canHandGrabR;
        private RhinoxHand _currentHandHolding;
        private RhinoxJoint _interactingJoint;

        protected void Awake()
        {
            _forceInteractibleJoint = true;
            _forcedInteractJointID = XRHandJointID.MiddleProximal;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _minRadius = _grabRadius - _grabTolaranceRadius;
            _maxRadius = _grabRadius + _grabTolaranceRadius;
            GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;
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

        private void TrackingLost(RhinoxHand hand)
        {
            TryLetGo(hand);
        }

        private void GestureRecognizerInitialized(GRPLGestureRecognizer gestureRecognizer)
        {
            if (_grabGesture == null)
            {
                _grabGesture = gestureRecognizer.Gestures.Find(x => x.Name == _grabGestureName);

                if (_grabGesture != null)
                {
                    _grabGesture.AddListenerOnRecognized(TryGrab);
                    _grabGesture.AddListenerOnUnRecognized(TryLetGo);
                }
            }
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
                _grabGesture.AddListenerOnUnRecognized(TryLetGo);
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
                _grabGesture.RemoveListenerOnUnRecognized(TryLetGo);
            }
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            float dst = transform.position.SqrDistanceTo(joint.JointPosition);

            //                      min radius                 max radius
            bool isInRange = dst >= _minRadius * _minRadius && dst <= _maxRadius * _maxRadius;

            if (isInRange)
                _interactingJoint = joint;

            switch (hand)
            {
                case RhinoxHand.Left:
                    _canHandGrabL = isInRange;
                    break;
                case RhinoxHand.Right:
                    _canHandGrabR = isInRange;
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }

            //var projected = Vector3.ProjectOnPlane(joint.JointPosition, -transform.forward);
            //var smtsmt = transform.position - projected;
            //TOOD: CLEAN THIS UP
            //if (dst >= _minRadius * _minRadius && dst <= _maxRadius * _maxRadius)
            //{
            //    switch (hand)
            //    {
            //        case RhinoxHand.Left:
            //            _canHandGrabL = true;
            //            break;
            //        case RhinoxHand.Right:
            //            _canHandGrabR = true;
            //            break;
            //        case RhinoxHand.Invalid:
            //        default:
            //            break;
            //    }
            //    //Debug.Log("GOOD");
            //}
            //else
            //{
            //    switch (hand)
            //    {
            //        case RhinoxHand.Left:
            //            _canHandGrabL = false;
            //            break;
            //        case RhinoxHand.Right:
            //            _canHandGrabR = false;
            //            break;
            //        case RhinoxHand.Invalid:
            //        default:
            //            break;
            //    }
            //}

            return IsGrabbed;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = joints.FirstOrDefault(x => x.JointID == _forcedInteractJointID);
            return outJoint != null;
        }

        public void TryGrab(RhinoxHand hand)
        {
            //if the given hand was invalid or the given hand cannot grab this object, do early return
            if (hand == RhinoxHand.Invalid ||
                hand == RhinoxHand.Left && !_canHandGrabL ||
                hand == RhinoxHand.Right && !_canHandGrabR ||
                hand == _currentHandHolding)
                return;

            IsGrabbed = true;
            _currentHandHolding = hand;
        }

        public void TryLetGo(RhinoxHand hand)
        {
            if (_currentHandHolding == hand)
            {
                IsGrabbed = false;
                _currentHandHolding = RhinoxHand.Invalid;
            }
        }

        private void Update()
        {
            if (IsGrabbed)
            {

            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_drawGizmos)
            {
                var trans = transform;

                using (new eUtility.GizmoColor(0f, 1f, 0f, .5f))
                {
                    GizmoExtensions.DrawSolidDonut(trans.position, trans.right, -trans.forward,
                        _minRadius, _maxRadius);
                }
            }
        }
#endif
    }
}