using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections;
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
        [SerializeField] float _fadeInDuration = .15f;
        [SerializeField] float _fadeOutDuration = .5f;

        public float SliderValue => _slider.value;
        public event Action<float> OnValueUpdate;

        private Bounds _pressBounds;
        private Slider _slider;
        private Vector3[] _corners = new Vector3[4];
        private float _sliderWitdth = 0f;

        private Coroutine _fadeCorotine;
        private Image _handleImage = null;
        private RhinoxJoint _previousInteractJoint;

        protected override void Initialize()
        {
            if (!TryGetComponent(out _slider))
                PLog.Error<GRPLITLogger>($"[{nameof(GRPLUISliderInteractable)}] {nameof(Initialize)}: " +
                    $"No slider Component was found!", this);

            _slider.direction = Slider.Direction.LeftToRight;

            if (!_slider.handleRect.GetChild(0).TryGetComponent(out _handleImage))
                PLog.Error<GRPLITLogger>($"[GRPLUISliderInteractable:Initialize], " +
                    $"Slider Handle does not have an image Component", this);

            //force setting handle image to invisible on start
            _handleImage.color = new Color(_handleImage.color.r,
                                           _handleImage.color.g,
                                           _handleImage.color.b,
                                           0f);

            CalculateBounds();
        }

        /// <summary>
        /// Helper method to get the bounds of a <see cref="RectTransform"/>.
        /// </summary>
        private void CalculateBounds()
        {
            var trans = (RectTransform)transform;
            _sliderWitdth = trans.sizeDelta.x;

            if (trans != null)
            {
                trans.GetWorldCorners(_corners);

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
            }
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
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
                var val = CalculateSliderValueFromSliderDirection(joint.JointPosition);
                _slider.value = val;
                OnValueUpdate?.Invoke(_slider.value);

                _previousInteractJoint = joint;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Calculates the slider value (0f->1f) range depending. 
        /// </summary>
        /// <param name="jointPos">The position of the joint that is trying to interact with the slider</param>
        /// <returns></returns>
        private float CalculateSliderValueFromSliderDirection(Vector3 jointPos)
        {
            var projectedJointOnPlane = Vector3.ProjectOnPlane(jointPos, -transform.forward);
            var projectedPointInLocalSpace = transform.InverseTransformPoint(projectedJointOnPlane);

            _slider.handleRect.GetChild(0).transform.SetLocalPosition(0f, projectedPointInLocalSpace.y, 0f);

            return projectedPointInLocalSpace.x.Map(_sliderWitdth * -.5f, _sliderWitdth * .5f, 0f, 1f);
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

        public override bool ShouldInteractionCheckStop()
        {
            if (State != GRPLInteractionState.Interacted)
                return false;

            // Check if the joint pos is in front of the plane that is defined by the button
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(_previousInteractJoint.JointPosition,
                                                                transform.position, -transform.forward))
            {
                ShouldPerformInteractCheck = false;

                var val = CalculateSliderValueFromSliderDirection(_previousInteractJoint.JointPosition);
                _slider.value = val;
                OnValueUpdate?.Invoke(_slider.value);

                return true;
            }

            ShouldPerformInteractCheck = true;
            return false;
        }

        protected override void InteractStarted()
        {
            base.InteractStarted();
            //stop coroutine so that the previous doesn't influence the current one
            if (_fadeCorotine != null)
                StopCoroutine(_fadeCorotine);

            _fadeCorotine = StartCoroutine(Fade());
        }

        /// <summary>
        /// Coroutine to fade in/out the handle icon on the slider.
        /// </summary>
        /// <param name="isFadeIn">Bool that changes the internal logic to act as fade in
        /// if <see langword="true"/> or as a fade out if <see langword="false"/></param>
        /// <returns></returns>
        private IEnumerator Fade(bool isFadeIn = true)
        {
            Color initialColor = _handleImage.color;
            Color targetColor;

            if (isFadeIn)
                targetColor = new Color(initialColor.r, initialColor.g, initialColor.b, 1f);
            else
                targetColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);

            float elapsedTime = 0f;
            float fadeDuration = isFadeIn ? _fadeInDuration : _fadeOutDuration;

            while (elapsedTime <= fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                _handleImage.color = Color.Lerp(initialColor, targetColor, elapsedTime / fadeDuration);
                yield return null;
            }

            if (isFadeIn)
                _fadeCorotine = StartCoroutine(Fade(false));
        }
    }
}