using System;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLButtonInteractable : GRPLInteractable
    {
        [Header("Poke parameters")] [SerializeField]
        private Transform _interactableBaseTransform;

        public Transform ButtonBaseTransform => _interactableBaseTransform;
        [SerializeField] private Transform _interactObject;
        public Transform ButtonSurface => _interactObject;

        [Header("Activation parameters")] [SerializeField] [Range(0f, 1f)]
        private float _selectStartPercentage = 0.25f;

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

        private protected override void InteractStarted()
        {
            OnInteractStarted?.Invoke();
        }

        private protected override void InteractStopped()
        {
            OnInteractEnded?.Invoke();
        }

        private protected override void ProximityStarted()
        {
            OnProximityStarted?.Invoke();
        }

        private protected override void ProximityStopped()
        {
            OnProximityEnded?.Invoke();
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
            float pokeDistance = InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition,buttonBaseTransform.position,back);


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
        // DEBUG DRAW
        //-----------------------
        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(InteractableMathUtils._projectedPos,0.01f);

        }
    }
}