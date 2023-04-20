using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// The GRPLOneWayLever class is a subclass of GRPLLeverBase and provides functionality for two-way levers in a 3D environment. <br /><br />
    /// It has four events (LeverForwardActivated, LeverForwardStopped, LeverBackwardActivated, LeverBackwardStopped) to notify when the lever is activated or stopped in either direction. <br /><br />
    /// It also has some properties that can be used for debugging. <br /><br />
    /// The OnValidate method is used to set the minimum angle that the lever can move, while the Update method is used to update the lever's rotation and process the current angle.
    /// </summary>
    public class GRPLTwoWayLever : GRPLLeverBase
    {
        public event Action<GRPLTwoWayLever> LeverForwardActivated;
        public event Action<GRPLTwoWayLever> LeverForwardStopped;
        public event Action<GRPLTwoWayLever> LeverBackwardActivated;
        public event Action<GRPLTwoWayLever> LeverBackwardStopped;

        private bool _forwardActive;
        private bool _backwardActive;

        [Header("Debug Parameters")]
        [SerializeField]
        private bool _drawDebug = false;

        [Space(10f)]

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawLeverParts = false;

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawArc = false;

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawLeverExtends = false;

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawGrabRange = false;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
        private void OnValidate()
        {
            _interactMinAngle = Mathf.Clamp(_interactMinAngle, 0, _leverMaxAngle / 2);
        }

        private void Update()
        {
            if (State != GRPLInteractionState.Interacted)
                return;

            // Project the joint on the lever plane
            Vector3 projectedPos =
                _previousInteractJoint.JointPosition.ProjectOnPlaneAndTranslate(_baseTransform.position,
                    _baseTransform.right);

            float angle = GetLeverRotation(projectedPos);

            // Set the final rotation
            Quaternion newRot = Quaternion.identity;
            newRot.eulerAngles = _initialHandleRot + new Vector3(angle, 0, 0);
            _stemTransform.rotation = newRot;

            ProcessLeverAngle(angle);
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
        /// <summary>
        /// Checks the angle of the lever and invokes the necessary events.
        /// </summary>
        /// <param name="angle">The current angle of the lever.</param>
        /// <remarks>The angle of the lever should be calculated with <see cref="GetLeverRotation"/></remarks>
        private void ProcessLeverAngle(float angle)
        {
            float angleAbs = Mathf.Abs(angle);
            if (angleAbs <= _interactMinAngle)
            {
                if (_forwardActive)
                {
                    _forwardActive = false;
                    LeverForwardStopped?.Invoke(this);
                }

                if (_backwardActive)
                {
                    _backwardActive = false;
                    LeverBackwardStopped?.Invoke(this);
                }

                return;
            }

            if (angle < 0)
            {
                if (!_forwardActive)
                {
                    _forwardActive = true;
                    LeverForwardActivated?.Invoke(this);
                }
            }
            else if (angle > 0)
            {
                if (!_backwardActive)
                {
                    _backwardActive = true;
                    LeverBackwardActivated?.Invoke(this);
                }
            }
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
        protected override void Initialize()
        {
            _initialHandlePos = _handleTransform.position;
            _initialHandleRot = _handleTransform.rotation.eulerAngles;
        }

        protected override float GetLeverRotation(Vector3 projectedPos)
        {
            Vector3 basePos = _baseTransform.position;

            // Calculate vector from the base to the projected joint
            Vector3 baseToJoint = projectedPos - basePos;

            // Vector3.SignedAngle returns an angle in [-180;180] degrees
            float angle = Vector3.SignedAngle(_baseTransform.forward, baseToJoint, _baseTransform.right);

            angle = Mathf.Clamp(angle, -_leverMaxAngle / 2, _leverMaxAngle / 2);
            return angle;
        }

        public override Transform GetReferenceTransform()
        {
            return _handleTransform;
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            if (_gestureRecognizer == null)
            {
                PLog.Warn<GRPLITLogger>("[GRPLOneWayLever, CheckForInteraction()], Gesture recognizer is null!");
                return false;
            }
            // Get the current gesture from the target hand
            RhinoxGesture gestureOnHand = _gestureRecognizer.GetCurrentGestureOfHand(hand);

            // If there is currently no gesture on the target hand
            if (gestureOnHand == null)
                return false;

            //If the gesture does not have the target name, return false
            if (!gestureOnHand.Name.Equals(_grabGestureName))
                return false;

            float jointDistSq = joint.JointPosition.SqrDistanceTo(_handleTransform.position);

            if (State != GRPLInteractionState.Interacted)
            {
                bool gestureRecognizedThisFrame = hand == RhinoxHand.Left
                    ? _gestureRecognizer.LeftHandGestureRecognizedThisFrame
                    : _gestureRecognizer.RightHandGestureRecognizedThisFrame;

                if (!gestureRecognizedThisFrame)
                    return false;

                // Return whether the interact joint is in range
                if (jointDistSq < _grabRadius * _grabRadius)
                {
                    _previousInteractJoint = joint;
                    return true;
                }

                return false;
            }

            if (State == GRPLInteractionState.Interacted && !_ignoreDistanceOnGrab)
            {
                if (jointDistSq < _grabRadius * _grabRadius)
                {
                    _previousInteractJoint = joint;
                    return true;
                }

                return false;
            }

            return State == GRPLInteractionState.Interacted;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint,
            RhinoxHand hand)
        {
            joint = joints.FirstOrDefault(x => x.JointID == _forcedInteractJointID);

            return joint != null;
        }


#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!_drawDebug)
                return;

            if (_drawLeverParts)
                DrawLeverTransforms();

            // Calculate arc parameters
            Transform transform1 = transform;
            Vector3 basePos = _baseTransform.position;
            Vector3 handlePos = _handleTransform.position;

            Vector3 direction = handlePos - basePos;
            direction.Normalize();
            Gizmos.DrawSphere(basePos, .01f);
            float arcRadius = Vector3.Distance(basePos, handlePos);

            if (_drawArc)
                DrawArc(basePos, direction, transform1.right, arcRadius);

            if (_drawLeverExtends)
                DrawLeverExtends(basePos, direction, transform1.right, arcRadius);

            if (_drawGrabRange)
                DrawGrabRange();
        }

        private void DrawLeverExtends(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            {
                // Draw lever start
                Vector3 beginPos = arcCenter + direction * arcRadius;
                Handles.Label(beginPos, "Lever start");
                Gizmos.DrawSphere(beginPos, .01f);
            }

            float maxAngleOneSide = _leverMaxAngle / 2;
            {
                // Draw forward MaxPos
                var dir = direction;
                dir = Quaternion.AngleAxis(-maxAngleOneSide, arcNormal) * dir; // rotate it
                Vector3 result = arcCenter + dir * arcRadius;
                Handles.Label(result, "Forward max");
                Gizmos.DrawSphere(result, .005f);
            }
            {
                // Draw forward MinPos
                var dir = direction;
                dir = Quaternion.AngleAxis(-_interactMinAngle, arcNormal) * dir; // rotate it
                Vector3 result = arcCenter + dir * arcRadius;
                Handles.Label(result, "Forward min");
                Gizmos.DrawSphere(result, .005f);
            }
            {
                // Draw backward MaxPos
                var dir = direction;
                dir = Quaternion.AngleAxis(maxAngleOneSide, arcNormal) * dir; // rotate it
                Vector3 result = arcCenter + dir * arcRadius;
                Handles.Label(result, "Backward min");
                Gizmos.DrawSphere(result, .005f);
            }
            {
                // Draw backward MinPos
                var dir = direction;
                dir = Quaternion.AngleAxis(_interactMinAngle, arcNormal) * dir; // rotate it
                Vector3 result = arcCenter + dir * arcRadius;
                Handles.Label(result, "Backward min");
                Gizmos.DrawSphere(result, .005f);
            }
        }

        private void DrawArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            float maxAngleOneSide = _leverMaxAngle / 2;
            {
                var backDir = direction;
                using (new eUtility.GizmoColor(Color.green))
                {
                    backDir = Quaternion.AngleAxis(_interactMinAngle, arcNormal) * backDir; // rotate it
                    GizmoExtensions.DrawSolidArc(arcCenter, backDir, arcNormal, arcRadius,
                        (maxAngleOneSide - _interactMinAngle), true);
                }
            }
            {
                var forwardDir = direction;
                using (new eUtility.GizmoColor(Color.green))
                {
                    forwardDir = Quaternion.AngleAxis(-maxAngleOneSide, arcNormal) * forwardDir; // rotate it
                    GizmoExtensions.DrawSolidArc(arcCenter, forwardDir, arcNormal, arcRadius,
                        (maxAngleOneSide - _interactMinAngle), true);
                }
            }
            {
                var forwardDir = direction;
                forwardDir = Quaternion.AngleAxis(-_interactMinAngle, arcNormal) * forwardDir; // rotate it
                using (new eUtility.GizmoColor(Color.red))
                {
                    GizmoExtensions.DrawSolidArc(arcCenter, forwardDir, arcNormal, arcRadius,
                        2 * _interactMinAngle, true);
                }
            }
        }

        private void Reset()
        {
            _baseTransform = new GameObject("Base").transform;
            _baseTransform.SetParent(this.transform);

            _baseTransform.localPosition = Vector3.zero;

            Transform baseVis = new GameObject("Base_Visuals").transform;
            baseVis.SetParent(_baseTransform.transform, false);

            _stemTransform = new GameObject("Stem").transform;
            _stemTransform.SetParent(_baseTransform, false);

            Transform stemVis = new GameObject("Stem_Visuals").transform;
            stemVis.SetParent(_stemTransform.transform, false);

            _handleTransform = new GameObject("Handle").transform;
            _handleTransform.SetParent(_stemTransform, false);

            var newLocalPos = Vector3.zero;
            newLocalPos.z += .5f;
            _handleTransform.localPosition = newLocalPos;

            Transform handleVis = new GameObject("Handle_Visuals").transform;
            handleVis.SetParent(_handleTransform.transform, false);
        }
#endif
    }
}