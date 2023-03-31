﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLInteractableManager :  MonoBehaviour
    {
        [HideInInspector]
        public GRPLInteractableManager Instance;

        [Header("Proximate detection parameters")]
        [SerializeField] private int _maxAmountOfProximatesPerHand = 3;
        [SerializeField] private float _proximateRadius = 1f;
        [SerializeField] private XRHandJointID _proximateJointID = XRHandJointID.MiddleMetacarpal;
        
        [Header("Interactible Groups")]
        [SerializeField] private List<GRPLInteractibleGroup> _interactibleGroups;
        
        private GRPLJointManager _jointManager;
        private List<GRPLInteractable> _interactables = null;

        private List<GRPLInteractable> _leftHandProximates;
        private List<GRPLInteractable> _rightHandProximates;
        
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

            GRPLJointManager.GlobalInitialized += OnJointManagerInitialised;

            _leftHandProximates = new List<GRPLInteractable>();
            _rightHandProximates = new List<GRPLInteractable>();
        }

        private void OnJointManagerInitialised(GRPLJointManager obj)
        {
            _jointManager = obj;
            _jointManager.TrackingLost += OnTrackingLost;
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
            if(_jointManager == null)
                return;
            
            if(_jointManager.IsLeftHandTracked)
                HandUpdate(RhinoxHand.Left);         
            
            if(_jointManager.IsRightHandTracked)
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
            if(!_jointManager.TryGetJointFromHandById(_proximateJointID,hand,out var proximateJoint))
                return;
            
            // Detect all the current proximates for this hand and invoke their events (if necessary)
            var proximates = DetectProximates(hand, proximateJoint.JointPosition);

            // Get all the joints of the given hand
            // If it failed to get the joints, return
            if(!_jointManager.TryGetJointsFromHand(hand, out var joints))
                return;

            // For every proximate
            foreach (var proximate in proximates)
            {
                // Try to get a valid interactJoint
                if (!proximate.TryGetCurrentInteractJoint(joints, out var interactJoint))
                {
                    if(proximate.State == GRPLInteractionState.Interacted)
                        proximate.SetState(GRPLInteractionState.Proximate);
                    continue;
                }
                
                // Check if an interaction is happening
                var isInteracted = proximate.CheckForInteraction(interactJoint);
                
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
                    PLog.Error<GRPLItLogger>($"[{this.GetType()}:DetectProximates], function called with invalid hand {hand}");
                    return Array.Empty<GRPLInteractable>();
            }
            
            var newProximateInteractables =
                new SortedDictionary<float, GRPLInteractable>();
            float proximateRadiusSqr = _proximateRadius * _proximateRadius;
            foreach (var interactable in _interactables)
            {
                // Ask senior
                // Is it worth to do a raycast to determine if nothing is blocking the object?
                // => would add a lot of calculations if there are many buttons
                
                
                // Calculate the vector from th joint to the interactable
                Vector3 fromJointToInteractable = interactable.transform.position - referenceJointPos;
                
                // Calculate the distance from the interactable to the reference pos
                float sqrDistance = fromJointToInteractable.sqrMagnitude;
                
                // If the calculated distance is larger than the squared proximate radius
                // continue
                if(sqrDistance > proximateRadiusSqr)
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
                    newProximateInteractables.Add(sqrDistance,interactable);
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
                proximate.SetState(GRPLInteractionState.Active);

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

        private void OnInteractableCreated(GRPLInteractable interactable)=> _interactables.Add(interactable);

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
                    PLog.Error<GRPLItLogger>($"[{GetType()}:OnTrackingLost], function called with invalid hand {hand}");
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