using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// The GRPLOneWayLever class is a subclass of GRPLLeverBase and provides functionality for one-way levers in a 3D environment. <br /><br />
    /// The class contains the {LeverActivated} and {LeverStopped} events for lever activation and lever stopping.<br /><br />
    /// Additionally, the class includes fields for debugging and drawing.
    /// </summary>
    public class GRPLOneWayLever : GRPLLeverBase
    {
        [Header("Debug Parameters")]
        [SerializeField] private bool _drawDebug = false;

        [Space(10f)]
        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawArc = false;

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawGrabRange = false;

        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawArcExtends = false;

        public event Action<GRPLOneWayLever> LeverActivated;
        public event Action<GRPLOneWayLever> LeverStopped;

        private bool _isLeverActivated = false;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
        private void Update()
        {
            if (State != GRPLInteractionState.Interacted)
                return;

            if (CheckLeverActivationAndSetLeverTransform(_previousInteractJoint.JointPosition))
            {
                if (!_isLeverActivated)
                {
                    LeverActivated?.Invoke(this);
                    _isLeverActivated = true;
                }
            }
            else if (_isLeverActivated)
            {
                LeverStopped?.Invoke(this);
                _isLeverActivated = false;
            }
        }

        //-----------------------
        // MEMBER METHODS
        //-----------------------
        /// <summary>
        /// Set's the transform of the lever according to the current interact joint
        /// </summary>
        /// <param name="jointPos">The position of the interact joint</param>
        /// <returns>Whether the angle of the lever has exceeded the interact angle</returns>
        private bool CheckLeverActivationAndSetLeverTransform(Vector3 jointPos)
        {
            // Project the joint on the lever plane
            Vector3 projectedPos =
                jointPos.ProjectOnPlaneAndTranslate(_baseTransform.position, _baseTransform.right);

            float angle = GetLeverRotation(projectedPos);
            // Set the final rotation
            Quaternion newRot = Quaternion.identity;
            newRot.eulerAngles = _initialHandleRot + new Vector3(angle, 0, 0);

            _stemTransform.rotation = newRot;

            return angle >= _interactMinAngle;
        }

        /// <summary>
        /// Calculates the angle between the projected position of the joint and original handle pos.
        /// </summary>
        /// <param name="projectedPos">The position of the projected joint on the plane defined by the lever.</param>
        /// <returns></returns>
        protected override float GetLeverRotation(Vector3 projectedPos)
        {
            Vector3 basePos = _baseTransform.position;

            // Calculate vector from the base to the projected joint
            Vector3 baseToJoint = projectedPos - basePos;

            // Calculate the vector from the base to the initial handle pos
            Vector3 baseToInitialHandle = _initialHandlePos - basePos;

            float angle;

            //----------------------------------------------
            // Calculate the angle between the two vectors
            //----------------------------------------------

            // Calculate the rotation angle for th signedAngle calculation
            Vector3 rotationVector = Vector3.Cross(baseToInitialHandle, baseToJoint);

            //Check if the joint is in front of the plane defined by the lever
            if (InteractableMathUtils.IsPositionInFrontOfPlane(projectedPos, basePos, _baseTransform.forward))
            {
                // Vector3.SignedAngle returns an angle in [-180;180] degrees
                angle = Vector3.SignedAngle(baseToInitialHandle, baseToJoint, rotationVector);
            }
            else
            {
                angle = Vector3.SignedAngle(baseToInitialHandle, baseToJoint, -rotationVector);
                angle = (180 - Mathf.Abs(angle)) + 180;
            }

            // Make sure the clamp to 0 functions well
            float tempAngle = 360 - angle;
            if (tempAngle < (360 - _leverMaxAngle) / 2)
                angle = -tempAngle;

            angle = Mathf.Clamp(angle, 0, _leverMaxAngle);
            return angle;
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
        protected override void Initialize()
        {
            _initialHandlePos = _handleTransform.position;
            _initialHandleRot = _handleTransform.rotation.eulerAngles;
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

        //-----------------------
        // EDITOR ONLY METHODS
        //-----------------------
#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!_drawDebug)
                return;

            Transform transform1 = transform;
            Vector3 basePos = _baseTransform.position;
            Vector3 handlePos = _handleTransform.position;

            Vector3 direction = handlePos - basePos;
            direction.Normalize();
            Gizmos.DrawSphere(basePos, .01f);
            float arcRadius = Vector3.Distance(basePos, handlePos);

            if (_drawArc)
                DrawArcHandles(basePos, direction, transform1.right, arcRadius);

            if (_drawGrabRange)
                Gizmos.DrawWireSphere(_handleTransform.position, _grabRadius);

            if (_drawArcExtends)
                DrawArcExtends(basePos, direction, transform1.right, arcRadius);

            if (_drawGrabRange)
                DrawGrabRange();
        }

        private void DrawArcHandles(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            int totalAmountOfSegments = 36;
            float angleStep = 360f / totalAmountOfSegments;

            using (new eUtility.GizmoColor(Color.red))
            {
                GizmoExtensions.DrawSolidArc(arcCenter, direction, arcNormal, arcRadius, _interactMinAngle, true, (int)
                    (_interactMinAngle / angleStep));
            }

            {
                var dir = direction;
                dir = Quaternion.AngleAxis(_interactMinAngle, arcNormal) * dir; // rotate it
                using (new eUtility.GizmoColor(Color.green))
                {
                    float arcAngle = _leverMaxAngle - _interactMinAngle;
                    GizmoExtensions.DrawSolidArc(arcCenter, dir, arcNormal, arcRadius, arcAngle,
                        true, (int)(arcAngle / angleStep));
                }
            }
        }

        private void DrawArcExtends(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            using (new eUtility.GizmoColor(Color.black))
            {
                // Draw lever begin
                Vector3 beginPos = arcCenter + direction * arcRadius;
                Handles.Label(beginPos, "Arc begin");
                Gizmos.DrawSphere(beginPos, .01f);

                var dir = direction;
                dir = Quaternion.AngleAxis(_leverMaxAngle, arcNormal) * dir; // rotate it

                Vector3 result = arcCenter + dir * arcRadius;
                Handles.Label(result, "Arc end");
                Gizmos.DrawSphere(result, .005f);
            }
        }
#endif
    }
}