using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.XR.Hands;
using System.Linq;
using UnityEngine;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using Sirenix.OdinInspector;
using UnityEditor;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.XR.Grapple.It
{
    public class GRPLLever : GRPLInteractable
    {
        [Space(5)] [Header("Lever parameters")] [SerializeField]
        private Transform _baseTransform;

        [SerializeField] private Transform _stemTransform;
        [SerializeField] private Transform _handleTransform;

        [SerializeField] [RangeField(0, "_leverMaxAngle")]
        private float _interactMinAngle = 90f;

        [SerializeField] [Range(0, 360f)] private float _leverMaxAngle = 180f;
        [SerializeField] private Vector3 _leverFollowRange = new Vector3(.6f, .6f, .6f);

        [Header("Grab parameters")] [SerializeField]
        private string _grabGestureName = "Grab";

        [SerializeField] private float _grabRadius = .1f;

        [Header("Debug Parameters")] [SerializeField]
        private bool _drawDebug = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawArc = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawGrabRange = false;

        [SerializeField] [HideIfFieldFalse("_drawDebug", 0f)]
        private bool _drawArcExtends = false;


        public event Action<GRPLLever> LeverActivated;
        public event Action<GRPLLever> LeverStopped;

        private GRPLGestureRecognizer _gestureRecognizer;

        private Vector3 _initialHandlePos;
        private Vector3 _initialHandleRot;
        private RhinoxJoint _previousInteractJoint;


        private bool _isLeverActivated = false;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
        private void Awake()
        {
            // Clamp the activation angle
            _interactMinAngle = Mathf.Clamp(_interactMinAngle, 0, _leverMaxAngle);

            //Force the ForcedInteractJoint
            ForceInteractibleJoint = true;
            ForcedInteractJointID = XRHandJointID.Palm;

            // Link to gesture recognizer
            GRPLGestureRecognizer.GestureRecognizerGlobalInitialized += OnGestureRecognizerGlobalInitialized;

            // Set initial state
            _initialHandlePos = _handleTransform.position;
            _initialHandleRot = _handleTransform.rotation.eulerAngles;
        }

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
        private float GetLeverRotation(Vector3 projectedPos)
        {
            Vector3 basePos = _baseTransform.position;

            // Calculate vector from the base to the projected joint
            Vector3 baseToJoint = projectedPos - basePos;

            // Calculate the vector from the base to the initial handle pos
            Vector3 baseToInitialHandle = _initialHandlePos - basePos;

            float angle = 0;

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
        // EVENT REACTIONS
        //-----------------------
        /// <summary>
        /// Event reaction to the globalInitialized event of the Gesture Recognizer.<br />
        /// Saves the recognizer in a private field.
        /// </summary>
        /// <param name="obj"></param>
        private void OnGestureRecognizerGlobalInitialized(GRPLGestureRecognizer obj)
        {
            _gestureRecognizer = obj;
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
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

            // Return whether the interact joint is in range
            float jointDistSq = joint.JointPosition.SqrDistanceTo(_handleTransform.position);

            if (jointDistSq < _grabRadius * _grabRadius)
            {
                _previousInteractJoint = joint;
                return true;
            }

            return false;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint)
        {
            joint = joints.FirstOrDefault(x => x.JointID == ForcedInteractJointID);

            return joint != null;
        }

        //-----------------------
        // EDITOR ONLY METHODS
        //-----------------------
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
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
            {
                Gizmos.DrawWireSphere(_handleTransform.position, _grabRadius);
            }

            if (_drawArcExtends)
                DrawArcExtends(basePos, direction, transform1.right, arcRadius);
        }

        private void DrawArcHandles(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius)
        {
            // Calculate the Arc radius
            Handles.color = Color.red;

            // Draw the arc itself
            Handles.DrawSolidArc(arcCenter, arcNormal, direction, _interactMinAngle, arcRadius);
            {
                var dir = direction;

                dir = Quaternion.AngleAxis(_interactMinAngle, arcNormal) * dir; // rotate it
                Handles.color = Color.green;
                Handles.DrawSolidArc(arcCenter, arcNormal, dir, _leverMaxAngle - _interactMinAngle,
                    arcRadius);
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
            newLocalPos.y += _leverFollowRange.y / 4;
            _handleTransform.localPosition = newLocalPos;

            Transform handleVis = new GameObject("Handle_Visuals").transform;
            handleVis.SetParent(_handleTransform.transform, false);
        }
#endif
    }
}