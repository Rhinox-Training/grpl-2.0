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
    /// The GRPLOneWayLever class is a subclass of GRPLLeverBase and provides functionality for one-way levers in
    /// a 3D environment. The class contains the LeverActivated and LeverStopped events for lever activation and lever
    /// stopping. <br /> Additionally, the class includes fields for debugging and drawing.
    /// </summary>
    public class GRPLOneWayLever : GRPLLeverBase
    {
        /// <summary>
        /// A boolean flag to indicate if debugging is enabled.
        /// </summary>
        [Header("Debug Parameters")]
        [SerializeField] private bool _drawDebug = false;

        /// <summary>
        /// A boolean flag to indicate if drawing the arc is enabled. Hidden if _drawDebug is false.
        /// </summary>
        [Space(10f)]
        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawArc = false;

        /// <summary>
        /// A boolean flag to indicate if drawing the grab range is enabled. Hidden if _drawDebug is false.
        /// </summary>
        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawGrabRange = false;

        /// <summary>
        /// A boolean flag to indicate if drawing the arc extends is enabled. Hidden if _drawDebug is false.
        /// </summary>
        [SerializeField]
        [HideIfField(false, "_drawDebug", 0f)]
        private bool _drawArcExtends = false;

        /// <summary>
        /// Triggered when the lever is activated.
        /// </summary>
        public event Action<GRPLOneWayLever> LeverActivated;

        /// <summary>
        /// Triggered when the lever is stopped.
        /// </summary>
        public event Action<GRPLOneWayLever> LeverStopped;


        private bool _isLeverActivated = false;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
        /// <summary>
        ///  Checks if the lever is interacted and calls the CheckLeverActivationAndSetLeverTransform method to
        /// determine if the lever has been activated or stopped. If the lever has been activated, invokes the
        /// LeverActivated event. If the lever has been stopped, invokes the LeverStopped event.
        /// </summary>
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
        /// Calculates the angle between the projected position of the joint and the original handle position.
        /// Sets the transform of the lever according to the current interact joint. Returns whether the angle of the
        /// lever has exceeded the interact angle.
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
        /// Calculates the angle between the projected position of the joint and the original handle position.
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
        /// <summary>
        /// Initializes the initial handle position and rotation.
        /// </summary>
        protected override void Initialize()
        {
            _initialHandlePos = _handleTransform.position;
            _initialHandleRot = _handleTransform.rotation.eulerAngles;
        }

        /// <summary>
        /// Checks if a joint and hand are interacting with the lever.
        /// Returns true if the joint and hand are interacting with the lever, false otherwise.
        /// </summary>
        /// <param name="joint">The interact joint</param>
        /// <param name="hand">The hand on which this joint resides</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the current interact joint, if it is found.
        /// </summary>
        /// <param name="joints">The joints on the current hand.</param>
        /// <param name="joint">An out parameter holding the interaction joint.</param>
        /// <param name="hand">The current hand.</param>
        /// <returns>Whether an interact joint was found.</returns>
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
        /// <summary>
        /// Draws the desired gizmos.
        /// </summary>
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