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
    /// <summary>
    /// This is a valve interactable.<br />
    /// 
    /// </summary>
    /// <remarks>Only one hand can interact with it at the same time.</remarks>
    /// <dependencies><see cref="GRPLJointManager"/><br /><see cref="GRPLGestureRecognizer"/></dependencies>
    public class GRPLValve : GRPLInteractable
    {
        [Space(10f)]
        [Header("Valve Settings")]
        [Space(5f)]
        [SerializeField] private Transform _visualTransform = null;
        [SerializeField] private GameObject _TEST = null;

        [Header("Grab parameters")]
        [SerializeField] private string _grabGestureName = "Grab";
        [SerializeField][Range(0f, .5f)] private float _grabRadius = .1f;
        [SerializeField][Range(0f, .25f)] private float _grabTolaranceRadius = .025f;

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


        private static GRPLJointManager _jointManager = null;
        private RhinoxJoint _interactingJoint = null;
        private RhinoxGesture _grabGesture = null;
        private RhinoxHand _currentHandHolding = RhinoxHand.Invalid;

        private Vector3 _grabbedVec = Vector3.positiveInfinity;

        private float _minRadius = 0f;
        private float _maxRadius = 0f;
        private float _currentValveRotation = 0f;

        private bool _canHandGrabL = false;
        private bool _canHandGrabR = false;

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
            //Check if jointPosition is inside torus, these calculations happen in 2 steps.
            //1. check if inside Annulus
            // -First projects the interacting joint on the valves plane
            // -checks if the projected point is within the minimum and maximum radius' prjPointCenterAnnulus
            //2. check if projected point is withing tollerance radius
            // -Reposition projected point to center line of min and max radius'
            // -check if distance between this point and joint position is within tollerance radius

            Vector3 projectedJoint = joint.JointPosition.ProjectOnPlaneAndTranslate(transform.position, -transform.forward);
            float dstValveToPrjJointSqr = transform.position.SqrDistanceTo(projectedJoint);
            bool isInRange = dstValveToPrjJointSqr >= _minRadius * _minRadius && dstValveToPrjJointSqr <= _maxRadius * _maxRadius;
            //early return if not inside annulus (min and max radius)
            if (!isInRange)
                return IsGrabbed;

            //calculate worldposition of projected point in center of min and max range of annulus.
            Vector3 prjPointCenterAnnulus = ((projectedJoint - transform.position).normalized * _grabRadius) + transform.position;
            //float dst2 = MathF.Abs(prjPointCenterAnnulus.SqrDistanceTo(joint.JointPosition));
            float dstRecenterdPointToJointSqr = MathF.Abs(prjPointCenterAnnulus.SqrDistanceTo(joint.JointPosition));
            isInRange &= dstRecenterdPointToJointSqr <= _grabTolaranceRadius * _grabTolaranceRadius;

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

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint, RhinoxHand hand)
        {
            outJoint = joints.FirstOrDefault(x => x.JointID == _forcedInteractJointID);
            return outJoint != null;
        }

        protected override void InteractStopped()
        {
            TryLetGo(_currentHandHolding);

            base.InteractStopped();
        }

        /// <summary>
        /// Function that allows external code to grab the valve if given hand is in range.
        /// </summary>
        /// <param name="hand">The hand that will be used to grab and interact with the valve.</param>
        public void TryGrab(RhinoxHand hand)
        {
            //if the given hand was invalid or the given hand cannot grab this object, do early return
            if (hand == RhinoxHand.Invalid ||
                hand == RhinoxHand.Left && !_canHandGrabL ||
                hand == RhinoxHand.Right && !_canHandGrabR ||
                hand == _currentHandHolding)
                return;

            IsGrabbed = true;
            _grabbedVec = _interactingJoint.JointPosition - transform.position;
            _currentHandHolding = hand;
        }

        /// <summary>
        /// Function that allows external code to grab the valve if given hand is the hand that is interacting with it.
        /// </summary>
        /// <param name="hand">The hand that should try to let go of the valve.</param>
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
                //calculate delta angle between current hand position and previous frame hand position
                float dAngle = Vector3.SignedAngle(_grabbedVec,
                    (_interactingJoint.JointPosition - transform.position), -transform.forward);

                //apply the delta rotation to total
                //and clamp to given max values
                //if max value is hit event will be invoked.
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

                _grabbedVec = _interactingJoint.JointPosition - transform.position;

                OnValueUpdate?.Invoke(this, _currentValveRotation);

                //ROTATE VISUAL
                _visualTransform.localEulerAngles = _visualTransform.localEulerAngles.With(null, null, _currentValveRotation);
            }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (_drawGizmos)
            {
                var trans = transform;

                using (new eUtility.GizmoColor(0f, 1f, 0f, .5f))
                {
                    GizmoExtensions.DrawSolidAnnulus(trans.position, trans.right, -trans.forward,
                    _grabRadius - _grabTolaranceRadius, _grabRadius + _grabTolaranceRadius, true, 12);
                    //GizmoExtensions.DrawSolidArc(transform.position, -transform.up, transform.right, .5f, 480f, true, 40);
                    //GizmoExtensions.DrawSolidTorus(trans.position, trans.right, -trans.forward, _grabRadius, _grabTolaranceRadius);
                    //GizmoExtensions.DrawWireTorus(trans.position, trans.right, -trans.forward, _grabRadius, _grabTolaranceRadius);
                }
            }
            if (_TEST == null)
                return;

            //Vector3 joint = _TEST.transform.position;
            //Gizmos.DrawSphere(joint, 0.01f);

            //var projectedJoint = joint.ProjectOnPlaneAndTranslate(transform.position, -transform.forward);
            //float dstValveToPrjJointSqr = transform.position.SqrDistanceTo(projectedJoint);

            //float minRadius = _grabRadius - _grabTolaranceRadius;
            //float maxRadius = _grabRadius + _grabTolaranceRadius;
            //bool isInRange = dstValveToPrjJointSqr >= minRadius * minRadius && dstValveToPrjJointSqr <= maxRadius * maxRadius;


            //var s = ((projectedJoint - transform.position).normalized * _grabRadius) + transform.position;
            //float dst2 = MathF.Abs(s.SqrDistanceTo(joint));
            //bool isInRange2 = dst2 <= _grabTolaranceRadius * _grabTolaranceRadius;


            //if (isInRange && isInRange2)
            //{
            //    using (new eUtility.GizmoColor(0f, 1f, 0f, .5f))
            //    {
            //        Gizmos.DrawSphere(s, 0.005f);
            //    }
            //}
            //else if (isInRange || isInRange2)
            //{
            //    using (new eUtility.GizmoColor(1f, 1f, 0f, .5f))
            //    {
            //        Gizmos.DrawSphere(s, 0.005f);
            //    }
            //}
            //else
            //{
            //    using (new eUtility.GizmoColor(1f, 0f, 0f, .5f))
            //    {
            //        Gizmos.DrawSphere(s, 0.005f);
            //    }
            //}
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