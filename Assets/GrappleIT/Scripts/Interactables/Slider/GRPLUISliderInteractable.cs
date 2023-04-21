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
    /// This class represents an interactable slider. It derives from
    /// GRPLInteractable, and it contains properties and methods to handle interaction with a hands. It also
    /// extends the basic functionality of a Unity Slider component to add hand tracking specific features.
    /// </summary>
    /// <remarks />
    /// <dependencies>Unity's built in<see cref="Slider"/></dependencies>
    public class GRPLUISliderInteractable : GRPLInteractable
    {
        /// <summary>
        /// A float that determines the duration of the fade-in effect when the slider is interacted with.
        /// </summary>
        [Header("Handle Fade settings")]
        [SerializeField] float _fadeInDuration = .15f;

        /// <summary>
        /// A float that determines the duration of the fade-out effect when the slider is not interacted with.
        /// </summary>
        [SerializeField] float _fadeOutDuration = .5f;

#if UNITY_EDITOR
        [Header("Gizmo")]
        [SerializeField] private bool _showBoundingBox;
#endif

        /// <summary>
        /// A Transform component that represents the slider's handle.
        /// </summary>
        [Header("Transforms")]
        [SerializeField] private Transform _handleTransform;

        /// <summary>
        /// A float property that returns the current value of the slider.
        /// </summary>
        public float SliderValue => _slider.value;

        /// <summary>
        /// An event that is triggered whenever the slider's value is updated.
        /// </summary>
        public event Action<GRPLUISliderInteractable, float> OnValueUpdate;

        /// <summary>
        /// A Bounds struct that defines the boundaries of the slider's interaction area.
        /// </summary>
        private Bounds _pressBounds;

        /// <summary>
        /// A Slider component that represents the slider.
        /// </summary>
        private Slider _slider;

        /// <summary>
        /// A Vector3 array that contains the corners of the slider's interaction area.
        /// </summary>
        private Vector3[] _corners = new Vector3[4];

        /// <summary>
        /// A float that represents the width of the slider.
        /// </summary>
        private float _sliderWidth = 0f;

        /// <summary>
        /// A float that represents the height of the slider.
        /// </summary>
        private float _sliderHeight = 0f;

        /// <summary>
        /// A Coroutine that controls the fade-in and fade-out effects of the slider.
        /// </summary>
        private Coroutine _fadeCoroutine;

        /// <summary>
        /// An Image component that represents the slider's handle image.
        /// </summary>
        private Image _handleImage = null;

        /// <summary>
        /// A RhinoxJoint component that represents the previous interaction joint for the slider.
        /// </summary>
        private RhinoxJoint _previousInteractJoint;

        /// <summary>
        /// This method initializes the slider's components and sets its initial values.
        /// </summary>
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
        /// This method calculates the boundaries of the slider's interaction area.
        /// </summary>
        private void CalculateBounds()
        {
            var trans = (RectTransform)transform;
            _sliderWidth = trans.sizeDelta.x;
            _sliderHeight = trans.sizeDelta.y;

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
                    size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ)
                };
            }
        }

        /// <summary>
        /// This method returns the Transform component of the slider's handle.
        /// </summary>
        /// <returns>The Transform component of the slider's handle.</returns>
        public override Transform GetReferenceTransform()
        {
            return _handleTransform;
        }

        /// <summary>
        /// This method checks whether a given RhinoxJoint is interacting with the slider.
        /// It returns a boolean value that indicates whether the interaction is successful.
        /// </summary>
        /// <param name="joint">The interaction joint.</param>
        /// <param name="hand">The hand on which this joint resides</param>
        /// <returns>Whether the interaction is successful</returns>
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
                OnValueUpdate?.Invoke(this, _slider.value);
                _previousInteractJoint = joint;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This method calculates the value of the slider based on the position of the interaction joint. 
        /// </summary>
        /// <param name="jointPos">The position of the joint that is trying to interact with the slider</param>
        /// <returns></returns>
        private float CalculateSliderValueFromSliderDirection(Vector3 jointPos)
        {
            var projectedJointOnPlane = Vector3.ProjectOnPlane(jointPos, -transform.forward);
            var projectedPointInLocalSpace = transform.InverseTransformPoint(projectedJointOnPlane);

            float handYPos = Mathf.Clamp(projectedPointInLocalSpace.y, _sliderHeight * -.5f, _sliderHeight * .5f);

            _slider.handleRect.GetChild(0).transform.SetLocalPosition(0f, handYPos, 0f);

            return Mathf.Clamp01(projectedPointInLocalSpace.x.Map(_sliderWidth * -.5f, _sliderWidth * .5f, 0f, 1f));
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint,
            RhinoxHand hand)
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

            // Check if the joint pos is in front of the plane that is defined by the Slider
            if (!InteractableMathUtils.IsPositionInFrontOfPlane(_previousInteractJoint.JointPosition,
                                                                transform.position, -transform.forward))
            {
                ShouldPerformInteractCheck = false;

                var val = CalculateSliderValueFromSliderDirection(_previousInteractJoint.JointPosition);
                _slider.value = val;
                OnValueUpdate?.Invoke(this, _slider.value);

                return true;
            }

            ShouldPerformInteractCheck = true;
            return false;
        }

        protected override void InteractStarted()
        {
            base.InteractStarted();
            //stop coroutine so that the previous doesn't influence the current one
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(Fade());
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
                _fadeCoroutine = StartCoroutine(Fade(false));
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (_showBoundingBox)
            {
                Gizmos.DrawWireCube(transform.position, _pressBounds.size);
            }
        }
#endif
    }
}