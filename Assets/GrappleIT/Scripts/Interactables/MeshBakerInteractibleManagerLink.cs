using System.Collections.Generic;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public class MeshBakerInteractibleManagerLink : MonoBehaviour
    {
        [SerializeField] private InteractableManager _interactableManager;
        [SerializeField] private MeshBaker _meshBaker;

        private List<GRPLInteractable> _pausedInteractables = new List<GRPLInteractable>();

        private void Awake()
        {
            JointManager.GlobalInitialized += OnJointManagerGlobalInitialized;
        }

        private void OnJointManagerGlobalInitialized(JointManager obj)
        {
            if (_meshBaker == null)
            {
                _meshBaker = obj.GetComponent<MeshBaker>();
                if (_meshBaker == null)
                {
                    PLog.Error<GrappleItLogger>(
                        "[MeshBakerInteractibleManagerLink:OnJointManagerGlobalInitialized] Could not find MeshBaker component, disabling component");
                }
            }

            if (_interactableManager == null)
            {
                _interactableManager = obj.GetComponent<InteractableManager>();
                if (_interactableManager == null)
                {
                    PLog.Error<GrappleItLogger>(
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