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
    /// This class represents a valve that can be interacted with in a hand tracking context.
    /// It extends the <see cref="GRPLInteractable"/> class.
    /// </summary>
    /// <remarks>Only one hand can interact with it at the same time. <br />
    /// Turning the valve clockwise is considered to be closing the valve. <br />
    /// Turning the valve counter-clockwise is considered to be opening the valve. <br />
    /// </remarks>
    /// <dependencies><see cref="GRPLJointManager"/><br /><see cref="GRPLGestureRecognizer"/></dependencies>
    public class GRPLValve : GRPLInteractable
    {
        /// <summary>
        /// A reference to the valve's Transform component that holds the visual representation of the valve.
        /// </summary>
        [Space(10f)] [Header("Valve Settings")] [Space(5f)] [SerializeField]
        private Transform _visualTransform = null;

        /// <summary>
        /// The name of the gesture that triggers the valve grab.
        /// </summary>
        [Header("Grab parameters")] [SerializeField]
        private string _grabGestureName = "Grab";

        /// <summary>
        /// The radius within which the user's hand must be positioned in order to grab the valve.
        /// </summary>
        [SerializeField] [Range(0f, .5f)] private float _grabRadius = .1f;

        /// <summary>
        /// The tolerance radius within which the user's hand must be positioned in order to grab the valve.
        /// </summary>
        [SerializeField] [Range(0f, .25f)] private float _grabToleranceRadius = .025f;

        /// <summary>
        /// The angle at which the valve is fully open.
        /// </summary>
        [Header("Valve Settings")] [SerializeField]
        private float _fullyOpenAngle = 0f;

        /// <summary>
        /// The angle at which the valve is fully closed.
        /// </summary>
        [SerializeField] private float _fullyClosedAngle = 360f;

        /// <summary>
        /// Whether or not to draw gizmos in the Unity Editor.
        /// </summary>
        [Header("Gizmos")] [SerializeField] bool _drawGizmos = true;

        /// <summary>
        /// Whether or not the valve is currently being grabbed.
        /// </summary>
        public bool IsGrabbed { get; protected set; } = false;

        /// <summary>
        /// The current angle of the valve.
        /// </summary>
        public float CurrentValveRotation => _currentValveRotation;

        /// <summary>
        /// An event that is triggered when the valve is fully closed.
        /// </summary>
        public Action<GRPLValve> FullyClosed;

        /// <summary>
        /// an event that is triggered when the valve is fully open.
        /// </summary>
        public Action<GRPLValve> FullyOpen;

        /// <summary>
        /// An event that is triggered when the value of the valve is updated.
        /// </summary>
        public Action<GRPLValve, float> ValueUpdated;

        /// <summary>
        /// A reference to the joint manager used to track hand joints.
        /// </summary>
        private static GRPLJointManager _jointManager = null;

        /// <summary>
        /// The joint that is currently interacting with the valve.
        /// </summary>
        private RhinoxJoint _interactingJoint = null;

        /// <summary>
        /// The gesture used to grab the valve.
        /// </summary>
        private RhinoxGesture _grabGesture = null;

        /// <summary>
        /// The hand that is currently holding the valve.
        /// </summary>
        private RhinoxHand _currentHandHolding = RhinoxHand.Invalid;

        /// <summary>
        /// The vector representing the position where the valve was grabbed.
        /// </summary>
        private Vector3 _grabbedVec = Vector3.positiveInfinity;

        /// <summary>
        /// The minimum radius at which a hand can grab the valve.
        /// </summary>
        private float _minRadius = 0f;

        /// <summary>
        /// The maximum radius at which a hand can grab the valve.
        /// </summary>
        private float _maxRadius = 0f;

        /// <summary>
        /// The current rotation of the valve in degrees.
        /// </summary>
        private float _currentValveRotation = 0f;

        /// <summary>
        /// Whether or not the left hand is able to grab the valve.
        /// </summary>
        private bool _canHandGrabL = false;

        /// <summary>
        /// Whether or not the right hand is able to grab the valve.
        /// </summary>
        private bool _canHandGrabR = false;

        protected void Awake()
        {
            _forceInteractibleJoint = true;
            _forcedInteractJointID = XRHandJointID.MiddleProximal;
        }

        /// <summary>
        /// This method initializes the valve's rotation, minimum and maximum grab radii and checks if the
        /// visualTransform variable is not null.
        /// </summary>
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

            _minRadius = _grabRadius - _grabToleranceRadius;
            _maxRadius = _grabRadius + _grabToleranceRadius;
        }

        /// <summary>
        /// This method is called when the joint manager is initialized. It adds a listener to the TrackingLost event.
        /// </summary>
        /// <param name="jointManager"></param>
        private void JointManagerInitialized(GRPLJointManager jointManager)
        {
            if (_jointManager == null)
            {
                _jointManager = jointManager;

                //No need to do something on TrackingAquired
                _jointManager.TrackingLost += TrackingLost;
            }
        }

        /// <summary>
        /// This method is called when tracking is lost on the given hand. It tries to let go of the valve.
        /// </summary>
        /// <param name="hand"></param>
        private void TrackingLost(RhinoxHand hand)
        {
            TryLetGo(hand);
        }

        /// <summary>
        /// This method is called when the gesture recognizer is initialized. It adds listeners to the TryGrab and TryLetGo methods.
        /// </summary>
        /// <param name="gestureRecognizer"></param>
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

        /// <summary>
        /// This method checks if a joint is within range of the valve and if it is in a proper gesture state to grab
        /// the valve. If so, it returns true, else false.
        /// </summary>
        /// <param name="joint">The interaction joint.</param>
        /// <param name="hand">The hand on which this joint resides</param>
        /// <returns>Whether the interaction is successful</returns>
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            //Check if jointPosition is inside torus, these calculations happen in 2 steps.
            //1. check if inside Annulus
            // -First projects the interacting joint on the valves plane
            // -checks if the projected point is within the minimum and maximum radius' prjPointCenterAnnulus
            //2. check if projected point is withing tollerance radius
            // -Reposition projected point to center line of min and max radius'
            // -check if distance between this point and joint position is within tollerance radius

            Vector3 projectedJoint =
                joint.JointPosition.ProjectOnPlaneAndTranslate(transform.position, -transform.forward);
            float dstValveToPrjJointSqr = transform.position.SqrDistanceTo(projectedJoint);
            bool isInRange = dstValveToPrjJointSqr >= _minRadius * _minRadius &&
                             dstValveToPrjJointSqr <= _maxRadius * _maxRadius;
            //early return if not inside annulus (min and max radius)
            if (!isInRange)
                return IsGrabbed;

            //calculate worldposition of projected point in center of min and max range of annulus.
            Vector3 prjPointCenterAnnulus =
                ((projectedJoint - transform.position).normalized * _grabRadius) + transform.position;
            //float dst2 = MathF.Abs(prjPointCenterAnnulus.SqrDistanceTo(joint.JointPosition));
            float dstRecenterdPointToJointSqr = MathF.Abs(prjPointCenterAnnulus.SqrDistanceTo(joint.JointPosition));
            isInRange &= dstRecenterdPointToJointSqr <= _grabToleranceRadius * _grabToleranceRadius;

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

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint,
            RhinoxHand hand)
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
                    FullyClosed?.Invoke(this);
                }
                else if (_currentValveRotation <= _fullyOpenAngle)
                {
                    _currentValveRotation = _fullyOpenAngle;
                    FullyOpen?.Invoke(this);
                }

                _grabbedVec = _interactingJoint.JointPosition - transform.position;

                ValueUpdated?.Invoke(this, _currentValveRotation);

                //ROTATE VISUAL
                _visualTransform.localEulerAngles =
                    _visualTransform.localEulerAngles.With(null, null, _currentValveRotation);
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
                        _grabRadius - _grabToleranceRadius, _grabRadius + _grabToleranceRadius, true, 12);
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