using System;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This component contains functionality to bake a mesh.
    /// </summary>
    public class MeshBaker : MonoBehaviour
    {
        /// <summary>
        /// The material to apply to the baked mesh.
        /// </summary>
        [SerializeField] private Material _bakedMeshMaterial;

        /// <summary>
        /// The material to apply to the hand after the mesh is baked.
        /// </summary>
        [SerializeField] private Material _handAfterBakeMaterial;

        /// <summary>
        /// The GRPLJointManager object used to access the hand renderers.
        /// </summary>
        private GRPLJointManager _jointManager;

        /// <summary>
        /// The SkinnedMeshRenderer object representing the left hand.
        /// </summary>
        private SkinnedMeshRenderer _leftHandRenderer;

        /// <summary>
        /// The SkinnedMeshRenderer object representing the right hand.
        /// </summary>
        private SkinnedMeshRenderer _rightHandRenderer;

        /// <summary>
        /// The GameObject to store the left baked meshes.
        /// </summary>
        private GameObject _leftBakedMeshObject;

        /// <summary>
        /// The GameObject to store the right baked meshes.
        /// </summary>
        private GameObject _rightBakedMeshObject;

        /// <summary>
        /// The GRPLHandVisualizer component.
        /// </summary>
        private GRPLHandVisualizer _handVisualizer = null;

        /// <summary>
        /// Registers the <see cref="OnJointManagerInitialized"/> method to the <see cref="GRPLJointManager.GlobalInitialized"/> event.
        /// </summary>
        private void Awake()
        {
            GRPLJointManager.GlobalInitialized += OnJointManagerInitialized;
        }

        /// <summary>
        ///  Method called when the <see cref="GRPLJointManager"/> is initialized. <br/>
        /// Sets _jointManager to obj and retrieves the _handVisualizer component.<br/>
        /// Disables the <see cref="MeshBaker"/> component if _handVisualizer is null.
        /// </summary>
        /// <param name="obj"></param>
        private void OnJointManagerInitialized(GRPLJointManager obj)
        {
            _jointManager = obj;
            if (_handVisualizer == null)
            {
                _handVisualizer = _jointManager.GetComponent<GRPLHandVisualizer>();
                if (_handVisualizer == null)
                {
                    PLog.Warn<GRPLITLogger>(
                        "[MeshBaker:OnJointManagerInitialized] Could not find GRPLHandVisualizer component, disabling mesh baker");
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
        /// Bakes the meshes for the given hand. If disableRenderer is true, the original hand will not be rendered.
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
                    PLog.Error<GRPLITLogger>($"[MeshBaker:BakeMesh], function called with invalid RhinoxHand: {hand}");
                    return;
            }
        }

        /// <summary>
        /// Bakes the meshes for the given hand and parents them to the specified Transform.
        /// If disableRenderer is true, the original hand will not be rendered.
        /// </summary>
        /// <param name="hand">The hand to bake.</param>
        /// <param name="parent">The transform to parent the baked mesh to.</param>
        /// <param name="disableRenderer">Optional parameter to specify if the original hand should continue rendering.</param>
        public void BakeMeshAndParentToTransform(RhinoxHand hand, Transform parent, bool disableRenderer = false)
        {
            BakeMesh(hand, disableRenderer);
            GameObject bakeParent;
            switch (hand)
            {
                case RhinoxHand.Left:
                    bakeParent = _leftBakedMeshObject;
                    break;
                case RhinoxHand.Right:
                    bakeParent = _rightBakedMeshObject;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLITLogger>(
                        "[MeshBaker:BakeMeshAndParentToTransform], function called with invalid hand value: {hand}");
                    return;
            }

            bakeParent.transform.SetParent(parent, true);
        }

        /// <summary>
        /// Method called to bake the mesh. Creates a new GameObject to store the baked mesh, applies the
        /// _bakedMeshMaterial, bakes the mesh, applies the _handAfterBakeMaterial,
        /// and parents the new GameObject to parent.
        /// </summary>
        /// <param name="meshRenderer">The mesh renderer to bake.</param>
        /// <param name="parent">The object to parent to.</param>
        private void BakeMeshDelegate(SkinnedMeshRenderer meshRenderer, GameObject parent)
        {
            MeshRenderer bakeRenderer = null;
            MeshFilter bakeFilter = null;

            var bakeChild = new GameObject("Baked mesh");
            bakeRenderer = bakeChild.AddComponent<MeshRenderer>();
            bakeFilter = bakeChild.AddComponent<MeshFilter>();

            bakeRenderer.material = _bakedMeshMaterial;
            meshRenderer.BakeMesh(bakeFilter.mesh, true);

            bakeFilter.mesh.RecalculateBounds();
            meshRenderer.material = _handAfterBakeMaterial;
            Transform transform1 = meshRenderer.transform;
            bakeChild.transform.position = transform1.position;
            bakeChild.transform.rotation = transform1.rotation;
            bakeChild.transform.SetParent(parent.transform, true);
        }

        /// <summary>
        /// Destroys all baked meshes for the given hand on this MeshBaker.
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
                    PLog.Error<GRPLITLogger>(
                        "[MeshBaker:DestroyBakedObjects], function called with invalid hand value: {hand}");
                    return;
            }
        }
    }
}