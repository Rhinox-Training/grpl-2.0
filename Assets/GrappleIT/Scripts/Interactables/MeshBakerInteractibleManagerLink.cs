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
        [SerializeField] private GRPLInteractableManager _interactableManager;
        private MeshBaker _meshBaker;

        private List<GRPLInteractable> _pausedInteractables = new List<GRPLInteractable>();

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
            _pausedInteractables.Add(interactable);

            _meshBaker.BakeMesh(hand);
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