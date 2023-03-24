using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLButtonInteractable : GRPLInteractable
    {
        [Header("Debug drawing")]
        [SerializeField] private bool _drawDebug = false;
        
        [Header("Poke parameters")] [SerializeField]
        private Transform _interactableBaseTransform;
        public Transform ButtonBaseTransform => _interactableBaseTransform;
        [SerializeField] private Transform _interactObject;
        public Transform ButtonSurface => _interactObject;

        [Header("Activation parameters")] [SerializeField] [Range(0f, 1f)]
        private float _selectStartPercentage = 0.25f;

        [SerializeField] private float _jointBehindButtonStopDelay = 5f;
        
        private const float _initialInteractOffset = 0.5f;
        
        public float SelectStartPercentage => _selectStartPercentage;
        private float _maxPressDistance;
        public float MaxPressedDistance => _maxPressDistance;

        public event Action OnInteractStarted;
        public event Action OnInteractEnded;
        public event Action OnProximityStarted;
        public event Action OnProximityEnded;

        public Bounds PressBounds { get; private set; }

        protected override void Initialize()
        {
            // Calculate the initial distance between the interact object and base transform
            _maxPressDistance = Vector3.Dot(_interactObject.transform.position - _interactableBaseTransform.position,
                -1f * _interactableBaseTransform.forward);

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
        private protected override void InteractStarted() => OnInteractStarted?.Invoke();
        private protected override void InteractStopped()
        {
            OnInteractEnded?.Invoke();
            ButtonSurface.transform.position = ButtonBaseTransform.position +
                                               -_maxPressDistance * ButtonBaseTransform.forward;
        }

        private protected override void ProximityStarted() => OnProximityStarted?.Invoke();
        private protected override void ProximityStopped() => OnProximityEnded?.Invoke();

        //-----------------------
        // COROUTINES
        //-----------------------
        [Obsolete]
        private IEnumerator BehindInteractedCoroutine()
        {
            PLog.Info<GrappleItLogger>($"Joint behind button, waiting for {_jointBehindButtonStopDelay} seconds", this);

            yield return new WaitForSecondsRealtime(_jointBehindButtonStopDelay);
            PLog.Warn<GrappleItLogger>("Too long behind button, stopping the interaction");
            SetState(GRPLInteractionState.Proximate);
        }

        //-----------------------
        // INHERITED METHODS
        //-----------------------
        public override bool CheckForInteraction(RhinoxJoint joint)
        {
            if (!gameObject.activeInHierarchy)
                return false;

            float closestDistance = MaxPressedDistance;

            // Cache the button fields that get reused
            Transform buttonBaseTransform = ButtonBaseTransform;

            var back = -buttonBaseTransform.forward;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(joint.JointPosition, buttonBaseTransform.position,
                    back))
                return false;

            // Check if the projected joint pos is within the button bounding box
            if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition, buttonBaseTransform.position,
                    Vector3.back, PressBounds))
                return false;

            // Projects the joint pos onto the normal out of the button and gets the distance
            float pokeDistance =
                InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition,
                    buttonBaseTransform.position, back);


            pokeDistance -= joint.JointRadius;
            if (pokeDistance < 0f)
            {
                pokeDistance = 0f;
            }

            closestDistance = Math.Min(pokeDistance, closestDistance);

            ButtonSurface.transform.position = buttonBaseTransform.position +
                                               -closestDistance * buttonBaseTransform.forward;

            float pressPercentage = 1 - (closestDistance / MaxPressedDistance);

            return pressPercentage > SelectStartPercentage;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = null;
            float closestDist = float.MaxValue;

            var normalPos = ButtonBaseTransform.position;
            var normal = -ButtonBaseTransform.forward;
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
                buttonObject.transform.localPosition -= _initialInteractOffset * ButtonBaseTransform.forward;
                
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