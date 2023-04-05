using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This is a Button interactible that can be used for both UI and Mesh based buttons. <br />
    /// The base transform is the button base. <br />
    /// The interact object is the button surface. <br />
    /// </summary>
    /// <remarks> The vector from the base to the surface should follow the local forward vector!</remarks>
    public class GRPLButtonInteractable : GRPLInteractable
    {
        [Header("Debug drawing")]
        [SerializeField] private bool _drawDebug;

        [Header("Poke parameters")]
        [SerializeField] private Transform _interactableBaseTransform;
        [SerializeField] private Transform _interactObject;

        [Header("Activation parameters")]
        [SerializeField] private bool _useInteractDelay = true;
        [SerializeField] private float _interactDelay = 0.25f;
        [Range(0f, 1f)][SerializeField] private float _selectStartPercentage = 0.25f;

        private Bounds _pressBounds;


        private RhinoxJoint _previousInteractJoint;
        private float _maxPressDistance;
        private const float INITIAL_INTERACT_OFFSET = 0.25f;
        private bool _isOnCooldown = false;

        protected override void Initialize()
        {
            // Calculate the initial distance between the interact object and base transform
            var position = _interactableBaseTransform.position;
            _maxPressDistance = Vector3.Dot(_interactObject.transform.position - position,
                _interactableBaseTransform.forward);

            var boundExtends = _interactObject.gameObject.GetObjectBounds().extents;
            boundExtends.z += 0.005f;

            _pressBounds = new Bounds()
            {
                center = position,
                extents = boundExtends
            };
        }

        //-----------------------
        // INHERITED EVENTS
        //-----------------------
        protected override void InteractStopped()
        {
            if (_useInteractDelay)
                StartCoroutine(DisableInteractibleForDuration());
            _interactObject.transform.position = _interactableBaseTransform.position +
                                                 _maxPressDistance * _interactableBaseTransform.forward;


            base.InteractStopped();
        }

        protected override void ProximityStopped()
        {
            _interactObject.transform.position = _interactableBaseTransform.position +
                                                 _maxPressDistance * _interactableBaseTransform.forward;
            base.ProximityStopped();
        }

        //-----------------------
        // COROUTINES
        //-----------------------
        private IEnumerator DisableInteractibleForDuration()
        {
            _isOnCooldown = true;
            yield return new WaitForSecondsRealtime(_interactDelay);
            _isOnCooldown = false;
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            if (!gameObject.activeInHierarchy || _isOnCooldown)
                return false;

            float closestDistance = _maxPressDistance;

            // Cache the button fields that get reused
            Transform buttonBaseTransform = _interactableBaseTransform;

            var forward = buttonBaseTransform.forward;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(joint.JointPosition, buttonBaseTransform.position,
                    forward))
                return false;

            // Check if the projected joint pos is within the button bounding box
            if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition, buttonBaseTransform.position,
                    transform.forward, _pressBounds))
                return false;

            // Projects the joint pos onto the normal out of the button and gets the distance
            float pokeDistance =
                InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition,
                    buttonBaseTransform.position, forward);


            pokeDistance -= joint.JointRadius;
            if (pokeDistance < 0f)
            {
                pokeDistance = 0f;
            }

            closestDistance = Math.Min(pokeDistance, closestDistance);

            _interactObject.transform.position = buttonBaseTransform.position +
                                                 closestDistance * buttonBaseTransform.forward;

            float pressPercentage = 1 - (closestDistance / _maxPressDistance);

            if (pressPercentage > _selectStartPercentage)
            {
                _previousInteractJoint = joint;
                return true;
            }

            return false;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = null;
            float closestDist = float.MaxValue;

            var normalPos = _interactableBaseTransform.position;
            var normal = _interactableBaseTransform.forward;

            foreach (var joint in joints)
            {
                if (_forceInteractibleJoint && joint.JointID != _forcedInteractJointID)
                    continue;

                if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition,
                        normalPos, normal, _pressBounds))
                    continue;

                var distance =
                    InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition, normalPos, normal);
                if (distance < closestDist)
                {
                    outJoint = joint;
                    closestDist = distance;
                }
            }

            return outJoint != null;
        }

        public override bool ShouldInteractionCheckStop()
        {
            if (State != GRPLInteractionState.Interacted)
                return false;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(_previousInteractJoint.JointPosition,
                    _interactableBaseTransform.position,
                    _interactableBaseTransform.forward))
            {
                ShouldPerformInteractCheck = false;
                return true;
            }

            ShouldPerformInteractCheck = true;
            return false;
        }


        //-----------------------
        // EDITOR ONLY METHODS
        //-----------------------
#if UNITY_EDITOR
        /// <summary>
        /// Creates and links the button surface to the button base transform.
        /// </summary>
        /// <warning>Using this method multiple times can result in duplicates of the ButtonBaseTransform and ButtonSurface object</warning>
        private void Reset()
        {
            if (_interactableBaseTransform == null)
            {
                var buttonBase = new GameObject("Button base");
                buttonBase.transform.SetParent(transform, false);
                _interactableBaseTransform = buttonBase.transform;
            }

            if (_interactObject == null)
            {
                var buttonObject = new GameObject("Button press object");
                buttonObject.transform.SetParent(transform, false);

                // Set the position of the button press object
                buttonObject.transform.localPosition = INITIAL_INTERACT_OFFSET * _interactableBaseTransform.forward;

                _interactObject = buttonObject.transform;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebug)
                return;

            if (_interactableBaseTransform != null)
            {
                using (new eUtility.GizmoColor(Color.cyan))
                {
                    var pos = _interactableBaseTransform.position;
                    Handles.Label(pos, "Press limit");
                    Gizmos.DrawWireSphere(pos, 0.01f);
                }
            }

            if (_interactObject != null)
            {
                using (new eUtility.GizmoColor(Color.red))
                {
                    var pos = _interactObject.position;
                    Handles.Label(pos, "Press surface");
                    Gizmos.DrawWireSphere(pos, 0.01f);
                }
            }
        }
#endif
    }
}