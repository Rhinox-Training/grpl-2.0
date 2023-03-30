using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class InteractableManager : MonoBehaviour
    {
        [HideInInspector] public InteractableManager Instance;

        [Header("Proximate detection parameters")] [SerializeField]
        private int _maxAmountOfProximatesPerHand = 3;

        [SerializeField] private float _proximateRadius = 1f;
        [SerializeField] private XRHandJointID _proximateJointID = XRHandJointID.MiddleMetacarpal;

        [Header("Interactible Groups")] [SerializeField]
        private List<InteractibleGroup> _interactibleGroups;

        private JointManager _jointManager;
        private List<GRPLInteractable> _interactables = null;

        private List<GRPLInteractable> _leftHandProximates;
        private List<GRPLInteractable> _rightHandProximates;

        [Header("Mesh Baking")] [SerializeField]
        private bool _checkForMeshBaking = true;

        [SerializeField] private MeshBaker _meshBaker;


        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Init();
            }
            else
            {
                Destroy(this);
            }

            JointManager.GlobalInitialized += OnJointManagerInitialised;

            _leftHandProximates = new List<GRPLInteractable>();
            _rightHandProximates = new List<GRPLInteractable>();
        }

        private void OnJointManagerInitialised(JointManager obj)
        {
            _jointManager = obj;
            _jointManager.TrackingLost += OnTrackingLost;
            if (_meshBaker == null)
            {
                _meshBaker = _jointManager.GetComponent<MeshBaker>();
                if (_meshBaker == null && _checkForMeshBaking)
                {
                    PLog.Error<GrappleItLogger>(
                        "[MeshBaker:OnJointManagerInitialized] Could not find MeshBaker component, disabling mesh baking");
                    _checkForMeshBaking = false;
                }
            }
        }

        private void Init()
        {
            GRPLInteractable.InteractableCreated -= OnInteractableCreated;
            GRPLInteractable.InteractableCreated += OnInteractableCreated;

            GRPLInteractable.InteractableDestroyed -= OnInteractableDestroyed;
            GRPLInteractable.InteractableDestroyed += OnInteractableDestroyed;

            _interactables = new List<GRPLInteractable>();
        }

        private void Update()
        {
            if (_jointManager == null)
                return;

            if (_jointManager.IsLeftHandTracked)
                HandUpdate(RhinoxHand.Left);

            if (_jointManager.IsRightHandTracked)
                HandUpdate(RhinoxHand.Right);
        }

        /// <summary>
        /// This methods handles all interactable updates for the given hand.  <br />
        /// It first detects the proximates and then checks those proximates for interactions.
        /// </summary>
        /// <param name="hand">Hand to process</param>
        private void HandUpdate(RhinoxHand hand)
        {
            // Get the proximate joint
            // If it failed to get the joint, return
            if (!_jointManager.TryGetJointFromHandById(_proximateJointID, hand, out var proximateJoint))
                return;

            // Detect all the current proximates for this hand and invoke their events (if necessary)
            var proximates = DetectProximates(hand, proximateJoint.JointPosition);

            // Get all the joints of the given hand
            // If it failed to get the joints, return
            if (!_jointManager.TryGetJointsFromHand(hand, out var joints))
                return;

            // For every proximate
            foreach (var proximate in proximates)
            {
                if (ControlInteractionCheckStop(hand, proximate))
                    continue;

                // Try to get a valid interactJoint
                if (!proximate.TryGetCurrentInteractJoint(joints, out var interactJoint))
                {
                    if (proximate.State == GRPLInteractionState.Interacted)
                        proximate.SetState(GRPLInteractionState.Proximate);
                    continue;
                }

                // Check if an interaction is happening
                bool isInteracted = proximate.CheckForInteraction(interactJoint);

                if (isInteracted)
                {
                    if (proximate.State != GRPLInteractionState.Interacted)
                        proximate.SetState(GRPLInteractionState.Interacted);
                }
                else if (proximate.State == GRPLInteractionState.Interacted)
                    proximate.SetState(GRPLInteractionState.Proximate);
            }
        }

        /// <summary>
        /// Checks if interaction checks should happen for this proximate. Also bakes the mesh if that is enabled.
        /// </summary>
        /// <param name="hand">The hand that is being checked</param>
        /// <param name="proximate">The current proximate</param>
        /// <returns>Whether to skip the interact calculations</returns>
        private bool ControlInteractionCheckStop(RhinoxHand hand, GRPLInteractable proximate)
        {
            // Cache the previous CanInteract value
            bool couldInteract = proximate.CanInteractCheck;

            // Check if interaction checks should happen for this proximate
            bool shouldInteractionCheckStop =
                proximate.ShouldInteractionCheckStop(new RhinoxJoint(XRHandJointID.Invalid));

            // If interaction should not get checked
            if (shouldInteractionCheckStop)
            {
                if (couldInteract && _checkForMeshBaking)
                    _meshBaker.BakeMesh(hand);
                return true;
            }
            else if (couldInteract == false)
            {
                if(_checkForMeshBaking)
                    _meshBaker.DestroyBakedObjects(hand);
            }

            return false;
        }
        

        /// <summary>
        /// Detects all proximate interactables of the given hand in a range of "_proximateRadius".<br />
        /// This function also calls the appropriate Proximity events of the interactables.
        /// </summary>
        /// <param name="hand">The desired hand.</param>
        /// <param name="referenceJointPos"> A reference point of the given hand. F.e. Wrist joint position.</param>
        /// <returns> An ICollection holding all the proximates</returns>
        /// <remarks>Hand should be RhinoxHand.Left or RhinoxHand.Right!</remarks>
        private IEnumerable<GRPLInteractable> DetectProximates(RhinoxHand hand, Vector3 referenceJointPos)
        {
            List<GRPLInteractable> currentProximates;
            switch (hand)
            {
                case RhinoxHand.Left:
                    currentProximates = _leftHandProximates;
                    break;
                case RhinoxHand.Right:
                    currentProximates = _rightHandProximates;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GrappleItLogger>(
                        $"[{this.GetType()}:DetectProximates], function called with invalid hand {hand}");
                    return Array.Empty<GRPLInteractable>();
            }

            var newProximateInteractables =
                new SortedDictionary<float, GRPLInteractable>();
            float proximateRadiusSqr = _proximateRadius * _proximateRadius;
            foreach (var interactable in _interactables)
            {
                if(interactable.State == GRPLInteractionState.Disabled)
                    continue;


                // Calculate the vector from th joint to the interactable
                Vector3 fromJointToInteractable = interactable.transform.position - referenceJointPos;

                // Calculate the distance from the interactable to the reference pos
                float sqrDistance = fromJointToInteractable.sqrMagnitude;

                // If the calculated distance is larger than the squared proximate radius
                // continue
                if (sqrDistance > proximateRadiusSqr)
                    continue;

                // If there are les than "_maxAmountOfProximatesPerHand" objects in the dictionary
                // Add it to the list and continue
                if (newProximateInteractables.Count < _maxAmountOfProximatesPerHand)
                {
                    newProximateInteractables.Add(sqrDistance, interactable);
                    continue;
                }

                // Find the new proximate at the furthest distance
                var furthestKey = newProximateInteractables.Keys.Max();

                // If the new square distance is smaller than that distance
                if (furthestKey > sqrDistance)
                {
                    // Replace it in the list
                    newProximateInteractables.Remove(furthestKey);
                    newProximateInteractables.Add(sqrDistance, interactable);
                }
            }

            // Detect which proximates are new
            // Do this by selecting all the pairs that the currentProximates list does not contain
            var newProximates = newProximateInteractables.Where(x => currentProximates.Contains(x.Value) == false);
            foreach (var pair in newProximates)
                pair.Value.SetState(GRPLInteractionState.Proximate);

            // foreach (var pair in newProximateInteractables)
            //         pair.Value.SetState(GRPLInteractionState.Proximate);

            // Save a copy of the current proximates as the previousProximates
            var previousProximates = new List<GRPLInteractable>(currentProximates);

            // Set the new current proximates
            currentProximates.Clear();
            foreach (var pair in newProximateInteractables)
                currentProximates.Add(pair.Value);

            // Filter out proximates that can not be used because of their group
            foreach (var group in _interactibleGroups)
            {
                group.FilterImpossibleInteractables(ref currentProximates);
            }

            // Detect which proximates are not valid anymore
            // Do this by selecting all proximates in previousProximates that the currentProximates does not contain
            var stoppedProximates = previousProximates.Where(x => currentProximates.Contains(x) == false);
            foreach (var proximate in stoppedProximates)
            {
                // If the proximate is still in proximity for the other hand
                // Ignore it
                
                if (!proximate.CanInteractCheck)
                {
                    proximate.CanInteractCheck = true;
                    if(_checkForMeshBaking)
                        _meshBaker.DestroyBakedObjects(hand);
                }
                proximate.SetState(GRPLInteractionState.Active);
            }

            return currentProximates;
        }

        //-----------------------
        // EVENT METHODS
        //-----------------------
        private void OnDestroy()
        {
            GRPLInteractable.InteractableCreated -= OnInteractableCreated;
            GRPLInteractable.InteractableDestroyed -= OnInteractableDestroyed;
        }

        private void OnInteractableCreated(GRPLInteractable interactable) => _interactables.Add(interactable);

        private void OnInteractableDestroyed(GRPLInteractable interactable) => _interactables.Remove(interactable);

        private void OnTrackingLost(RhinoxHand hand)
        {
            List<GRPLInteractable> proximates;
            switch (hand)
            {
                case RhinoxHand.Left:
                    proximates = _leftHandProximates;
                    break;
                case RhinoxHand.Right:
                    proximates = _rightHandProximates;
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GrappleItLogger>(
                        $"[{GetType()}:OnTrackingLost], function called with invalid hand {hand}");
                    return;
            }

            foreach (var proximate in proximates)
            {
                proximate.SetState(GRPLInteractionState.Active);
            }

            proximates.Clear();
        }
    }
}