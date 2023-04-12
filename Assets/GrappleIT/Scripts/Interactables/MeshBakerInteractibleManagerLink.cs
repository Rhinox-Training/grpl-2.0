using System;
using System.Collections.Generic;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This component links the interactible manager to the mesh baker. <br />
    /// When the interactible manager freezer an interaction check, it bakes the current mesh.
    /// </summary>
    /// <dependencies>
    /// <see cref="GRPLInteractableManager"/> <see cref="MeshBaker"/> </dependencies>
    public class MeshBakerInteractibleManagerLink : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GRPLInteractableManager _interactableManager;

        [Header("Bake parameters")]
        [Tooltip("Parameter to specify what bake should be used, if the GRPLInteractableBakeSettings component is not found.")]
        [SerializeField] private GRPLBakeOptions _defaultBakeOptions = GRPLBakeOptions.NoBake;
        
        private MeshBaker _meshBaker;
        private HashSet<GRPLInteractable> _pausedInteractables = new HashSet<GRPLInteractable>();

        private void Awake()
        {
            GRPLJointManager.GlobalInitialized += OnJointManagerGlobalInitialized;
        }

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


        private void OnInteractibleInteractionCheckPaused(RhinoxHand hand, GRPLInteractable interactable)
        {
            if(_pausedInteractables.Add(interactable))
                PLog.Warn<GRPLITLogger>("[MeshBakerInteractibleManagerLink:OnInteractibleInteractionCheckPaused] Interactable is already paused");

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
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        private void OnInteractibleInteractionCheckResumed(RhinoxHand hand, GRPLInteractable interactable)
        {
            _meshBaker.DestroyBakedObjects(hand);
        }

        private void OnInteractibleLeftProximity(RhinoxHand hand, GRPLInteractable interactable)
        {
            if (!_pausedInteractables.Contains(interactable))
                return;

            _meshBaker.DestroyBakedObjects(hand);
            _pausedInteractables.Remove(interactable);
        }
    }
}