using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
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
        private Vector3 _slidDirHor = Vector3.zero;
        private float _sliderWitdth = 0f;
        private bool _boolToBeNamed = false;

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

        private void CalculateBounds()
        {
            var trans = (RectTransform)transform;
            _sliderWitdth = trans.sizeDelta.x;

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

                _slidDirHor = _corners[3] - _corners[0];
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

        //protected override 

        /// <summary>
        /// Calculates the slider value (0f->1f) range depending on the <see cref="SliderDirections"/> 
        /// </summary>
        /// <param name="sliderPos">The worldposition of the slider object</param>
        /// <param name="sliderForward">The forward vector of the slider</param>
        /// <param name="jointPos">The position of the joint that is trying to interact with the slider</param>
        /// <returns></returns>
        private float CalculateSliderValueFromSliderDirection(Vector3 jointPos)
        {
            Vector3 jointDir = jointPos - _corners[0];
            Vector3 projectedjoint = Vector3.Project(jointDir, _slidDirHor);

            //if (!_boolToBeNamed)
            _slider.handleRect.GetChild(0).transform.SetPosition(jointPos.x, jointPos.y);

            return projectedjoint.magnitude / _sliderWitdth;
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

            _boolToBeNamed = true;
            StartCoroutine(Fade());
        }

        protected override void InteractStopped()
        {
            base.InteractStopped();

            _boolToBeNamed = false;
        }

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
                StartCoroutine(Fade(false));
        }


        private void OnDrawGizmos()
        {
            //Gizmos.DrawWireCube(_pressBounds.center, _pressBounds.extents);
            //GUIContentHelper.PushColor(new Color(1f, 0f, 1f, 1f));
            //Gizmos.DrawRay(_pressBounds.center, -transform.forward);
            //GUIContentHelper.PopColor();
        }
    }
}