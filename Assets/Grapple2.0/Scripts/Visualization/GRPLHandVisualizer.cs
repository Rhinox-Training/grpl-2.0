using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// Visualizes the hands with the given prefabs.
    /// </summary>
    /// <dependencies> <see cref="GRPLJointManager"/> </dependencies>
    public class GRPLHandVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform _xrOrigin;
        [SerializeField] private GameObject _leftHandPrefab;
        [SerializeField] private GameObject _rightHandPrefab;
        [SerializeField] private Material _handMaterial;

        public Material HandMaterial => _handMaterial;
        public SkinnedMeshRenderer LeftHandRenderer => _leftHandRenderer;
        public SkinnedMeshRenderer RightHandRenderer => _rightHandRenderer;


        private GRPLJointManager _jointManager;
        private List<GameObject> _leftJoints = new List<GameObject>();
        private List<GameObject> _rightJoints = new List<GameObject>();

        private GameObject _leftHandRoot;
        private GameObject _rightHandRoot;

        private SkinnedMeshRenderer _leftHandRenderer;
        private SkinnedMeshRenderer _rightHandRenderer;

        private void Awake()
        {
            GRPLJointManager.GlobalInitialized += OnJointManagerInitialized;

            _leftHandRoot = new GameObject("Hand model Root");
            InstantiateHand(RhinoxHand.Left, _leftJoints, _leftHandRoot.transform, _leftHandPrefab,
                out _leftHandRenderer);

            _rightHandRoot = new GameObject("Hand model Root");
            InstantiateHand(RhinoxHand.Right, _rightJoints, _rightHandRoot.transform, _rightHandPrefab,
                out _rightHandRenderer);
        }

        private void OnJointManagerInitialized(GRPLJointManager jointManager)
        {
            _jointManager = jointManager;
            _jointManager.OnHandsUpdated += OnHandUpdated;
            _jointManager.TrackingAcquired += OnTrackingAcquired;
            _jointManager.TrackingLost += OnTrackingLost;

            _leftHandRoot.transform.SetParent(_jointManager.LeftHandParentObj.transform, true);
            _rightHandRoot.transform.SetParent(_jointManager.RightHandParentObj.transform, true);
        }

        private void InstantiateHand(RhinoxHand hand, List<GameObject> jointObjects, Transform parent,
            GameObject prefab,
            out SkinnedMeshRenderer handRederer)
        {
            var meshGO = Instantiate(prefab, parent);
            var children = meshGO.GetAllChildren();
            var thumbObjects = new List<GameObject>();
            var indexObjects = new List<GameObject>();
            var middleObjects = new List<GameObject>();
            var ringObjects = new List<GameObject>();
            var littleObjects = new List<GameObject>();

            foreach (var child in children)
            {
                // If the object contains the prefix, it's a joint
                if (child.name.Contains(hand.ToPrefix()))
                {
                    // Check on what finger this joint belongs
                    if (child.name.Contains(RhinoxFinger.Thumb.ToString()))
                        thumbObjects.Add(child);
                    else if (child.name.Contains(RhinoxFinger.Index.ToString()))
                        indexObjects.Add(child);
                    else if (child.name.Contains(RhinoxFinger.Middle.ToString()))
                        middleObjects.Add(child);
                    else if (child.name.Contains(RhinoxFinger.Ring.ToString()))
                        ringObjects.Add(child);
                    else if (child.name.Contains(RhinoxFinger.Little.ToString()))
                        littleObjects.Add(child);
                    else if (child.name.Contains(XRHandJointID.Wrist.ToString()))
                        jointObjects.Add(child);
                    else if (child.name.Contains(XRHandJointID.Palm.ToString()))
                        jointObjects.Add(child);
                }
            }

            jointObjects.AddRange(thumbObjects);
            jointObjects.AddRange(indexObjects);
            jointObjects.AddRange(middleObjects);
            jointObjects.AddRange(ringObjects);
            jointObjects.AddRange(littleObjects);

            //Get the skinned mesh renderer
            handRederer = meshGO.GetComponentInChildren<SkinnedMeshRenderer>();

            if (handRederer == null)
            {
                PLog.Error<GrappleLogger>(
                    "[GRPLHandVisualizer:InstantiateHand], Could not find a skinned mesh renderer on the prefab");
                gameObject.SetActive(false);
                return;
            }

            handRederer.material = HandMaterial;
        }

        private void OnHandUpdated(RhinoxHand hand)
        {
            List<GameObject> jointObjects;

            switch (hand)
            {
                case RhinoxHand.Left:
                    jointObjects = _leftJoints;
                    break;
                case RhinoxHand.Right:
                    jointObjects = _rightJoints;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error($"[GRPLHandVisualizer,OnHandUpdated], function called with invalid hand value: {hand}");
                    return;
            }

            if (!_jointManager.TryGetJointsFromHand(hand, out var updatedJoints))
                return;

            for (int index = 0; index < jointObjects.Count; ++index)
            {
                var jointTransform = jointObjects[index].transform;
                var rhinoxJoint = updatedJoints[index];
                jointTransform.position = rhinoxJoint.JointPosition;
                jointTransform.rotation = rhinoxJoint.JointRotation;
            }
        }

        private void OnTrackingAcquired(RhinoxHand hand)
        {
            SkinnedMeshRenderer meshRenderer = null;
            switch (hand)
            {
                case RhinoxHand.Left:
                    meshRenderer = _leftHandRenderer;
                    break;
                case RhinoxHand.Right:
                    meshRenderer = _rightHandRenderer;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GrappleLogger>(
                        $"[GRPLHandVisualizer,OnTrackingAcquired], function called with invalid hand value: {hand}");
                    return;
            }

            if (meshRenderer != null)
                meshRenderer.enabled = true;
        }

        private void OnTrackingLost(RhinoxHand hand)
        {
            SkinnedMeshRenderer meshRenderer = null;
            switch (hand)
            {
                case RhinoxHand.Left:
                    meshRenderer = _leftHandRenderer;
                    break;
                case RhinoxHand.Right:
                    meshRenderer = _rightHandRenderer;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GrappleLogger>(
                        $"[GRPLHandVisualizer,OnTrackingLost], function called with invalid hand value: {hand}");
                    return;
            }

            if (meshRenderer != null)
                meshRenderer.enabled = false;
        }
    }
}