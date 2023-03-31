using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class MeshBaker : MonoBehaviour
    {
        [SerializeField] private GRPLHandVisualizer _handVisualizer;
        [SerializeField] private Material _bakedMeshMaterial;
        [SerializeField] private Material _handAfterBakeMaterial;

        private GRPLJointManager _jointManager;

        private SkinnedMeshRenderer _leftHandRenderer;
        private SkinnedMeshRenderer _rightHandRenderer;

        private GameObject _leftBakedMeshObject;
        private GameObject _rightBakedMeshObject;

        private void Awake()
        {
            GRPLJointManager.GlobalInitialized += OnJointManagerInitialized;
        }

        private void OnJointManagerInitialized(GRPLJointManager obj)
        {
            _jointManager = obj;
            if (_handVisualizer == null)
            {
                _handVisualizer = _jointManager.GetComponent<GRPLHandVisualizer>();
                if (_handVisualizer == null)
                {
                    PLog.Warn<GRPLItLogger>("[MeshBaker:OnJointManagerInitialized] Could not find GRPLHandVisualizer component, disabling mesh baker");
                    enabled = false;
                }
                else
                {
                    _leftHandRenderer = _handVisualizer.LeftHandRenderer;
                    _rightHandRenderer = _handVisualizer.RightHandRenderer;
                }
            }
        }

        /// <summary>
        /// Bakes the meshes for the given hand.
        /// </summary>
        /// <param name="hand">The hand to bake.</param>
        /// <param name="disableRenderer">Optional parameter to specify if the original hand should continue rendering.</param>
        public void BakeMesh(RhinoxHand hand, bool disableRenderer = false)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    if (!_jointManager.IsLeftHandTracked)
                        return;
                    if (_leftBakedMeshObject == null)
                        _leftBakedMeshObject = new GameObject("Left Baked meshes");
                    _leftHandRenderer.enabled = !disableRenderer;
                    BakeMeshDelegate(_leftHandRenderer, _leftBakedMeshObject);
                    break;
                case RhinoxHand.Right:
                    if (!_jointManager.IsRightHandTracked)
                        return;
                    if (_rightBakedMeshObject == null)
                        _rightBakedMeshObject = new GameObject("Right Baked meshes");
                    _rightHandRenderer.enabled = !disableRenderer;
                    BakeMeshDelegate(_rightHandRenderer, _rightBakedMeshObject);
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLItLogger>($"[MeshBaker:BakeMesh], function called with invalid RhinoxHand: {hand}");
                    return;
            }
        }

        private void BakeMeshDelegate(SkinnedMeshRenderer meshRenderer, GameObject parent)
        {
            MeshRenderer bakeRenderer = null;
            MeshFilter bakeFilter = null;

            var bakeChild = new GameObject("Baked mesh");
            bakeRenderer = bakeChild.AddComponent<MeshRenderer>();
            bakeFilter = bakeChild.AddComponent<MeshFilter>();

            bakeRenderer.material = _bakedMeshMaterial;
            meshRenderer.BakeMesh(bakeFilter.mesh);

            meshRenderer.material = _handAfterBakeMaterial;
            var transform1 = meshRenderer.transform;
            bakeChild.transform.position = transform1.position;
            bakeChild.transform.rotation = transform1.rotation;
            bakeChild.transform.SetParent(parent.transform, true);
        }

        /// <summary>
        /// Destroys all baked meshes for the given hand on this mesh baker.
        /// </summary>
        /// <param name="hand"></param>
        public void DestroyBakedObjects(RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    Destroy(_leftBakedMeshObject);
                    _leftHandRenderer.material = _handVisualizer.HandMaterial;
                    _leftHandRenderer.enabled = true;
                    break;
                case RhinoxHand.Right:
                    Destroy(_rightBakedMeshObject);
                    _rightHandRenderer.material = _handVisualizer.HandMaterial;
                    _rightHandRenderer.enabled = true;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLItLogger>("[MeshBaker:DestroyBakedObjects], function called with invalid hand value: {hand}");
                    return;
            }
        }
    }
}