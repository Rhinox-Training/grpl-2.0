using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections.Generic;
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
        public event Action<float> OnValueUpdate;
        public Slider.Direction SliderDirection => _sliderDirection;
        public float SliderValue => _slider.value;


        private Slider _slider;
        private Slider.Direction _sliderDirection = Slider.Direction.LeftToRight;
        private Bounds _pressBounds;

        protected override void Initialize()
        {
            if (!TryGetComponent(out _slider))
                PLog.Error<GRPLITLogger>($"[{nameof(GRPLUISliderInteractable)}] {nameof(Initialize)}: " +
                    $"No slider Component was found!", this);

            _sliderDirection = _slider.direction;

            var trans = (RectTransform)transform;
            if (trans != null)
            {
                Vector3[] corners = new Vector3[4];
                trans.GetWorldCorners(corners);

                float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
                float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;

                foreach (var corner in corners)
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

                //extend the slider x-bound a bit to make it easier to get the 0% and 100%
                //extend the slider z-bound for the plane projection
                _pressBounds.extents = new Vector3(_pressBounds.extents.x + 0.005f,
                                                   _pressBounds.extents.y,
                                                   _pressBounds.extents.z + 0.005f);
            }


            var boundExtends = RectTransformUtility.CalculateRelativeRectTransformBounds(transform);
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

            if (pokeDistance <= _pressBounds.extents.z)
            {
                _slider.value = CalculateSliderValueFromSliderDirection(sliderPos, sliderForward, joint.JointPosition);
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
            var projectedPos = Vector3.ProjectOnPlane(jointPos, sliderForward);

            switch (_sliderDirection)
            {
                case Slider.Direction.LeftToRight:
                    return jointPos.x.Map(sliderPos.x - (_pressBounds.extents.x / 2f), sliderPos.x + (_pressBounds.extents.x / 2f), 0f, 1f);
                case Slider.Direction.RightToLeft:
                    return jointPos.x.Map(sliderPos.x - (_pressBounds.extents.x / 2f), sliderPos.x + (_pressBounds.extents.x / 2f), 1f, 0f);
                case Slider.Direction.BottomToTop:
                    return jointPos.y.Map(sliderPos.y - (_pressBounds.extents.y / 2f), sliderPos.y + (_pressBounds.extents.y / 2f), 0f, 1f);
                case Slider.Direction.TopToBottom:
                    return jointPos.y.Map(sliderPos.y - (_pressBounds.extents.y / 2f), sliderPos.y + (_pressBounds.extents.y / 2f), 1f, 0f);
                default:
                    PLog.Error($"[{nameof(GRPLUISliderInteractable)}] {nameof(CalculateSliderValueFromSliderDirection)}: " +
                    $"SliderDirection: {_sliderDirection} was not valid!", this);
                    break;
            }

            return _slider.value;
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
    }
}