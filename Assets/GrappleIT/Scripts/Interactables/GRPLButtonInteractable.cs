using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLButtonInteractable : GRPLInteractable
    {
        [Header("Debug drawing")]
        [SerializeField] private bool _drawDebug;
        
        [Header("Poke parameters")] [SerializeField]
        private Transform _interactableBaseTransform;

        private Transform ButtonBaseTransform => _interactableBaseTransform;
        [SerializeField] private Transform _interactObject;
        private Transform ButtonSurface => _interactObject;

        [Header("Activation parameters")] [SerializeField] [Range(0f, 1f)]
        private float _selectStartPercentage = 0.25f;

        [SerializeField] private bool _useInteractDelay = true;
        [Tooltip("The minimum time between subsequent interactions")]
        [SerializeField]private float _interactDelay = 0.25f;
        private const float INITIAL_INTERACT_OFFSET = 0.25f;
        
        public float SelectStartPercentage => _selectStartPercentage;
        private float _maxPressDistance;
        public float MaxPressedDistance => _maxPressDistance;
        
        private RhinoxJoint _previousInteractJoint;

        private Bounds PressBounds { get; set; }
        private bool _isOnCooldown = false;
        
        protected override void Initialize()
        {
            // Calculate the initial distance between the interact object and base transform
            _maxPressDistance = Vector3.Dot(_interactObject.transform.position - _interactableBaseTransform.position,
                 _interactableBaseTransform.forward);

            var boundExtends = ButtonSurface.gameObject.GetObjectBounds().extents;
            boundExtends.z += 0.005f;

            PressBounds = new Bounds()
            {
                center = ButtonBaseTransform.position,
                extents = boundExtends
            };
        }

        //-----------------------
        // INHERITED EVENTS
        //-----------------------
        private protected override void InteractStopped()
        {
            if (_useInteractDelay)
                StartCoroutine(DisableInteractibleForDuration());
            ButtonSurface.transform.position = ButtonBaseTransform.position +
                                               _maxPressDistance * ButtonBaseTransform.forward;
            
            
            base.InteractStopped();
        }

        private protected override void ProximityStopped()
        {
            ButtonSurface.transform.position = ButtonBaseTransform.position +
                                               _maxPressDistance * ButtonBaseTransform.forward;
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
        public override bool CheckForInteraction(RhinoxJoint joint)
        {
            if (!gameObject.activeInHierarchy || _isOnCooldown)
                return false;

            float closestDistance = MaxPressedDistance;

            // Cache the button fields that get reused
            Transform buttonBaseTransform = ButtonBaseTransform;

            var forward = buttonBaseTransform.forward;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(joint.JointPosition, buttonBaseTransform.position,
                    forward))
                return false;

            // Check if the projected joint pos is within the button bounding box
            if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition, buttonBaseTransform.position,
                    Vector3.back, PressBounds))
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

            ButtonSurface.transform.position = buttonBaseTransform.position +
                                               closestDistance * buttonBaseTransform.forward;

            float pressPercentage = 1 - (closestDistance / MaxPressedDistance);

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

            var normalPos = ButtonBaseTransform.position;
            var normal = ButtonBaseTransform.forward;
            foreach (var joint in joints)
            {
                if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition,
                        normalPos, normal, PressBounds))
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

        public override bool ShouldInteractionCheckStop(RhinoxJoint interactJoint)
        {
            if (State != GRPLInteractionState.Interacted)
                return false;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(_previousInteractJoint.JointPosition,
                    ButtonBaseTransform.position,
                    ButtonBaseTransform.forward))
            {
                CanInteractCheck = false;
                return true;
            }

            CanInteractCheck = true;
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
            if (ButtonBaseTransform == null)
            {
                var buttonBase = new GameObject("Button base");
                buttonBase.transform.SetParent(transform,false);
                _interactableBaseTransform = buttonBase.transform;
            }

            if (ButtonSurface == null)
            {
                var buttonObject = new GameObject("Button press object");
                buttonObject.transform.SetParent(transform,false);
                
                // Set the position of the button press object
                buttonObject.transform.localPosition = INITIAL_INTERACT_OFFSET * ButtonBaseTransform.forward;
                
                _interactObject = buttonObject.transform;
            }
        }

        private void OnDrawGizmos()
        {
            if(!_drawDebug)
                return;
            
            if (ButtonBaseTransform != null)
            {
                Gizmos.color = Color.cyan;
                var pos = ButtonBaseTransform.position;
                Handles.Label(pos, "Press limit");
                Gizmos.DrawWireSphere(pos, 0.01f);
            }

            if (ButtonSurface != null)
            {
                Gizmos.color = Color.red;
                var pos = ButtonSurface.position;
                Handles.Label(pos, "Press surface");
                Gizmos.DrawWireSphere(pos, 0.01f);
            }
            
        }
#endif
        
    }
}