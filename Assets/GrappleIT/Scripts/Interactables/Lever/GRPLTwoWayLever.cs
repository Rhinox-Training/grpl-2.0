using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLTwoWayLever : GRPLLeverBase
    {
        public event Action<GRPLTwoWayLever> LeverForwardActivated;
        public event Action<GRPLTwoWayLever> LeverForwardStopped;
        public event Action<GRPLTwoWayLever> LeverBackwardActivated;
        public event Action<GRPLTwoWayLever> LeverBackwardStopped;

        private bool _forwardActive;
        private bool _backwardActive;

        [Header("Debug Parameters")] [SerializeField]
        private bool _drawDebug = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawLeverParts = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawArc = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawLeverExtends = false;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
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

        public override Vector3 GetReferencePoint()
        {
            return _handleTransform.position;
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            // Get the current gesture from the target hand
            RhinoxGesture gestureOnHand = _gestureRecognizer.GetGestureOnHand(hand);

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

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint)
        {
            joint = joints.FirstOrDefault(x => x.JointID == ForcedInteractJointID);

            return joint != null;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
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
                DrawLeverExtends();
        }

        private void DrawLeverExtends()
        {
        }

        private void DrawArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            float maxAngleOneSide = _leverMaxAngle / 2;

            {
                Handles.color = Color.green;
                var backDir = direction;
                backDir = Quaternion.AngleAxis(maxAngleOneSide, arcNormal) * backDir; // rotate it
                Handles.DrawSolidArc(arcCenter, arcNormal, backDir, -(maxAngleOneSide - _interactMinAngle),
                    arcRadius);
            }
            {
                Handles.color = Color.green;
                var forwardDir = direction;
                forwardDir = Quaternion.AngleAxis(-maxAngleOneSide, arcNormal) * forwardDir; // rotate it
                Handles.DrawSolidArc(arcCenter, arcNormal, forwardDir, (maxAngleOneSide - _interactMinAngle),
                    arcRadius);
                
                
            }
            
        }
#endif
    }
}