using Rhinox.GUIUtils;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// A UI slider based on the GRPLInteractable system.<br />
    /// There is an event for each time the slider updates giving the slider value as paramter.
    /// </summary>
    /// <remarks />
    /// <dependencies>Unity's built in<see cref="Slider"/></dependencies>
    public class GRPLUISliderInteractable : GRPLInteractable
    {
        public float SliderValue => _slider.value;
        public event Action<float> OnValueUpdate;

        private Bounds _pressBounds;
        private Slider _slider;
        private Vector3[] _corners = new Vector3[4];
        private Vector3 _slidDir = Vector3.zero;

        protected override void Initialize()
        {
            if (!TryGetComponent(out _slider))
                PLog.Error<GRPLITLogger>($"[{nameof(GRPLUISliderInteractable)}] {nameof(Initialize)}: " +
                    $"No slider Component was found!", this);

            _slider.direction = Slider.Direction.LeftToRight;

            var trans = (RectTransform)transform;
            if (trans != null)
            {
                Vector3[] cornersLocal = new Vector3[4];
                trans.GetWorldCorners(_corners);
                trans.GetLocalCorners(cornersLocal);

                float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
                float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;

                foreach (var corner in _corners)
                {
                    minX = Mathf.Min(minX, corner.x);
                    minY = Mathf.Min(minY, corner.y);
                    minZ = Mathf.Min(minZ, corner.z);

                    maxX = Mathf.Max(maxX, corner.x);
                    maxY = Mathf.Max(maxY, corner.y);
                    maxZ = Mathf.Max(maxZ, corner.z);
                }

                _pressBounds = new Bounds()
                {
                    center = transform.position,
                    extents = new Vector3(maxX - minX, maxY - minY, maxZ - minZ)
                };

                _slidDir = _corners[3] - _corners[0];
            }
        }

        public override bool CheckForInteraction(RhinoxJoint joint)
        {
            if (!gameObject.activeInHierarchy)
                return false;

            var sliderPos = transform.position;
            var sliderForward = -transform.forward;

            // Check if the joint pos is in front of the slider plane
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(joint.JointPosition, sliderPos, sliderForward))
                return false;

            // Check if the projected joint pos is within the slider bounding box
            if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition, sliderPos,
                    Vector3.back, _pressBounds))
                return false;

            // Projects the joint pos onto the normal out of the slider and gets the distance
            float pokeDistance = InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition,
                    sliderPos, sliderForward);


            pokeDistance -= joint.JointRadius;
            if (pokeDistance < 0f)
                pokeDistance = 0f;

            if (pokeDistance <= 0.005f)
            {
                var val = CalculateSliderValueFromSliderDirection(sliderPos, sliderForward, joint.JointPosition);
                PLog.Info<GRPLITLogger>($"SLiderVal: {val}");
                _slider.value = val;
                OnValueUpdate?.Invoke(_slider.value);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Calculates the slider value (0f->1f) range depending on the <see cref="SliderDirections"/> 
        /// </summary>
        /// <param name="sliderPos">The worldposition of the slider object</param>
        /// <param name="sliderForward">The forward vector of the slider</param>
        /// <param name="jointPos">The position of the joint that is trying to interact with the slider</param>
        /// <returns></returns>
        private float CalculateSliderValueFromSliderDirection(Vector3 sliderPos, Vector3 sliderForward, Vector3 jointPos)
        {
            Vector3 jointDir = jointPos - _corners[0];

            Vector3 projectedjoint = Vector3.Project(jointDir, _slidDir);
            return projectedjoint.magnitude;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = null;
            float closestDist = float.MaxValue;

            var normalPos = transform.position;
            var normal = -transform.forward;
            foreach (var joint in joints)
            {
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


        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(_pressBounds.center, _pressBounds.extents);
            GUIContentHelper.PushColor(new Color(1f, 0f, 1f, 1f));
            Gizmos.DrawRay(_pressBounds.center, -transform.forward);
            GUIContentHelper.PopColor();
        }
    }
}