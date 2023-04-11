//using log4net.Util;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLValve : GRPLInteractable
    {
        [SerializeField] private Transform _visualTransform = null;

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
        public float CurrentValveRotation => _currentValveRotation;

        //righty tighty, lefty loosey
        public Action<GRPLValve> OnFullyClosed;
        public Action<GRPLValve> OnFullyOpen;
        public Action<GRPLValve, float> OnValueUpdate;


        private float _minRadius = 0f;
        private float _maxRadius = 0f;
        private float _currentValveRotation = 0f;

        private RhinoxGesture _grabGesture = null;
        private static GRPLJointManager _jointManager = null;
        private bool _canHandGrabL = false;
        private bool _canHandGrabR = false;
        private RhinoxHand _currentHandHolding = RhinoxHand.Invalid;
        private RhinoxJoint _interactingJoint = null;

        private Vector3 _grabbedVec = Vector3.positiveInfinity;

        protected void Awake()
        {
            _forceInteractibleJoint = true;
            _forcedInteractJointID = XRHandJointID.MiddleProximal;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (_visualTransform == null)
            {
                PLog.Error<GRPLITLogger>($"[GRPLValve:Initialize], " +
                    $"Visual Transform was null", this);
                return;
            }

            float currentRot = _visualTransform.localEulerAngles.z;
            if (currentRot > _fullyClosedAngle)
                _visualTransform.localEulerAngles.With(null, null, _fullyClosedAngle);
            else if (currentRot < _fullyOpenAngle)
                _visualTransform.localEulerAngles.With(null, null, _fullyOpenAngle);

            _currentValveRotation = _visualTransform.localEulerAngles.z;

            _minRadius = _grabRadius - _grabTolaranceRadius;
            _maxRadius = _grabRadius + _grabTolaranceRadius;
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
            _grabbedVec = _interactingJoint.JointPosition;
            _currentHandHolding = hand;
        }

        public void TryLetGo(RhinoxHand hand)
        {
            if (_currentHandHolding == hand)
            {
                IsGrabbed = false;
                _grabbedVec = Vector3.positiveInfinity;
                _currentHandHolding = RhinoxHand.Invalid;
            }
        }

        private void Update()
        {
            if (IsGrabbed && _interactingJoint != null)
            {
                //var proj = _interactingJoint.JointPosition.ProjectOnPlaneAndTranslate(transform.position, -transform.forward);
                //var smrse = Vector3.ProjectOnPlane(_interactingJoint.JointPosition, -transform.forward);
                float dAngle = Vector3.SignedAngle(_grabbedVec, _interactingJoint.JointPosition, -transform.forward);

                //if (Mathf.Abs(dAngle) < .5f)
                //return;

                _currentValveRotation -= dAngle;
                if (_currentValveRotation >= _fullyClosedAngle)
                {
                    _currentValveRotation = _fullyClosedAngle;
                    OnFullyClosed?.Invoke(this);
                }
                else if (_currentValveRotation <= _fullyOpenAngle)
                {
                    _currentValveRotation = _fullyOpenAngle;
                    OnFullyOpen?.Invoke(this);
                }
                _grabbedVec = _interactingJoint.JointPosition;


                OnValueUpdate?.Invoke(this, _currentValveRotation);

                //ROTATE VISUAL
                _visualTransform.localEulerAngles = _visualTransform.localEulerAngles.With(null, null, _currentValveRotation);
                // (0f,0f,0f,Space.Self);
                //_visualTransform.localEulerAngles.With(null,null,)
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

        private void Reset()
        {
            if (_visualTransform == null)
            {
                var go = new GameObject("Visual");
                _visualTransform = go.transform;
                _visualTransform.parent = transform;
            }
        }
#endif
    }
}