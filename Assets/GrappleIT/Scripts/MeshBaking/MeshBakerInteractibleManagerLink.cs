using System;
using System.Collections.Generic;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// Links the <see cref="GRPLInteractableManager"/> to the <see cref="MeshBaker"/> component. <br /> This script is used to bake the current
    /// mesh when the <see cref="GRPLInteractableManager"/> freezes an interaction check. <br /> <br /> It subscribes to events from the
    /// <see cref="GRPLInteractableManager"/> to pause and resume interactables, and to track interactables that are currently paused.
    ///  <br /> <br /> The class also provides a default bake option for cases where the <see cref="GRPLInteractableBakeSettings"/> component is not
    /// found. When an interactible is paused, the script uses the bake option specified in the interactables
    /// <see cref="GRPLInteractableBakeSettings"/> component to bake the mesh using the <see cref="MeshBaker"/> component. When the interactible is
    /// resumed or left proximity, the baked mesh is destroyed.
    /// </summary>
    /// <dependencies>
    /// <see cref="GRPLInteractableManager"/> <see cref="MeshBaker"/> </dependencies>
    public class MeshBakerInteractibleManagerLink : MonoBehaviour
    {
        /// <summary>
        /// Stores a reference to the GRPLInteractableManager component.
        /// </summary>
        [Header("Dependencies")] [SerializeField]
        private GRPLInteractableManager _interactableManager;

        /// <summary>
        /// Stores the default bake option to use when the GRPLInteractableBakeSettings component is not found.
        /// </summary>
        [Header("Bake parameters")]
        [Tooltip(
            "Parameter to specify what bake should be used, if the GRPLInteractableBakeSettings component is not found.")]
        [SerializeField]
        private GRPLBakeOptions _defaultBakeOptions = GRPLBakeOptions.NoBake;

        /// <summary>
        /// Stores a reference to the MeshBaker component.
        /// </summary>
        private MeshBaker _meshBaker;

        /// <summary>
        /// Stores interactables that are currently paused.
        /// </summary>
        private HashSet<GRPLInteractable> _pausedInteractables = new HashSet<GRPLInteractable>();

        /// <summary>
        /// Subscribes to the GlobalInitialized event of the GRPLJointManager.
        /// </summary>
        private void Awake()
        {
            GRPLJointManager.GlobalInitialized += OnJointManagerGlobalInitialized;
        }

        /// <summary>
        /// Gets references to the MeshBaker and GRPLInteractableManager components, subscribes to events of the
        /// GRPLInteractableManager, and logs errors if any component is missing.
        /// </summary>
        /// <param name="obj"></param>
        private void OnJointManagerGlobalInitialized(GRPLJointManager obj)
        {
            if (_meshBaker == null)
            {
                _meshBaker = obj.GetComponent<MeshBaker>();
                if (_meshBaker == null)
                {
                    PLog.Error<GRPLITLogger>(
                        "[MeshBakerInteractibleManagerLink:OnJointManagerGlobalInitialized] Could not find MeshBaker component, disabling component");
                }
            }

            if (_interactableManager == null)
            {
                _interactableManager = obj.GetComponent<GRPLInteractableManager>();
                if (_interactableManager == null)
                {
                    PLog.Error<GRPLITLogger>(
                        "[MeshBakerInteractibleManagerLink:OnJointManagerGlobalInitialized] Could not find _interactableManager component, disabling component");
                }
            }

            _interactableManager.InteractibleInteractionCheckPaused += OnInteractibleInteractionCheckPaused;
            _interactableManager.InteractibleInteractionCheckResumed += OnInteractibleInteractionCheckResumed;
            _interactableManager.InteractibleLeftProximity += OnInteractibleLeftProximity;
        }

        /// <summary>
        /// Called when an interactible is paused. It checks if the interactible is already paused,
        /// gets the bake option specified in the interactables <See cref="GRPLInteractableBakeSettings" /> component or
        /// the default bake option, and bakes the mesh using the <see cref="MeshBaker"/> component accordingly.
        /// </summary>
        /// <param name="hand">The current hand.</param>
        /// <param name="interactable">The current interactable.</param>
        private void OnInteractibleInteractionCheckPaused(RhinoxHand hand, GRPLInteractable interactable)
        {
            if (_pausedInteractables.Add(interactable))
                PLog.Warn<GRPLITLogger>(
                    "[MeshBakerInteractibleManagerLink:OnInteractibleInteractionCheckPaused] Interactable is already paused");

            var bakeSettings = interactable.GetComponent<GRPLInteractableBakeSettings>();
            GRPLBakeOptions bakeOption = bakeSettings == null ? _defaultBakeOptions : bakeSettings.BakeOptions;

            switch (bakeOption)
            {
                case GRPLBakeOptions.NoBake:
                    return;
                case GRPLBakeOptions.StandardBake:
                    _meshBaker.BakeMesh(hand);
                    break;
                case GRPLBakeOptions.BakeAndParent:
                    _meshBaker.BakeMeshAndParentToTransform(hand, interactable.GetReferenceTransform());
                    break;
                default:
                    PLog.Error<GRPLITLogger>(
                        $"[MeshBakerInteractibleManagerLink,OnInteractibleInteractionCheckPaused], " +
                        $"Invalid bake option {bakeOption} ");
                    break;
            }
        }

        /// <summary>
        /// Called when an interactible is resumed. It destroys the baked mesh using the <see cref="MeshBaker"/> component.
        /// </summary>
        /// <param name="hand">The current hand.</param>
        /// <param name="interactable">The current interactable.</param>
        private void OnInteractibleInteractionCheckResumed(RhinoxHand hand, GRPLInteractable interactable)
        {
            _meshBaker.DestroyBakedObjects(hand);
        }

        /// <summary>
        /// Called when an interactible leaves proximity. It destroys the baked mesh using the <see cref="MeshBaker"/>
        /// component and removes the interactible from the set of paused interactables.
        /// </summary>
        /// <param name="hand">The current hand.</param>
        /// <param name="interactable">The current interactable.</param>
        private void OnInteractibleLeftProximity(RhinoxHand hand, GRPLInteractable interactable)
        {
            if (!_pausedInteractables.Contains(interactable))
                return;

            _meshBaker.DestroyBakedObjects(hand);
            _pausedInteractables.Remove(interactable);
        }
    }
}