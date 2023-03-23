using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed.Collections;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class InteractableManager :  MonoBehaviour
    {
        [HideInInspector]
        public InteractableManager Instance;

        [Header("Proximate detection parameters")]
        [SerializeField] private int _maxAmountOfProximatesPerHand = 3;
        [SerializeField] private float _proximateRadius = 1f;
        
        private JointManager _jointManager;
        private List<GRPLInteractable> _interactables = null;

        private List<GRPLInteractable> _leftHandProximites;
        private List<GRPLInteractable> _rightHandProximites;

        private GRPLInteractable _currentLeftSelectedObject = null;
        private GRPLInteractable _currentRightSelectedObject = null;
        
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

            _leftHandProximites = new List<GRPLInteractable>();
            _rightHandProximites = new List<GRPLInteractable>();
        }

        private void OnJointManagerInitialised(JointManager obj) =>_jointManager = obj;

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
            
            if(!_jointManager.TryGetJointFromHandById(XRHandJointID.IndexTip,RhinoxHand.Right,out var joint))
                return;
            
            DetectProximates(RhinoxHand.Right,joint.JointPosition);
            
            var buttons = _interactables.OfType<GRPLButtonInteractable>().ToArray();

            foreach (var button in buttons)
            {
                if(!button.isActiveAndEnabled)
                    continue;
                
                float closestDistance = button.MaxPressedDistance;

                // Cache the button fields that get reused
                Transform buttonBaseTransform = button.ButtonBaseTransform;
                
                //Loop over the joints
                {
                    var back = -buttonBaseTransform.forward;

                    // Check if the joint pos is in front of the plane that is defined by the button
                    if(!IsPositionInFrontOfPlane(joint.JointPosition, buttonBaseTransform.position,back))
                        continue;
                    
                    // Check if the projected joint pos is within the button bounding box
                    if(!IsPlaneProjectedPointInBounds(joint.JointPosition,buttonBaseTransform.position,
                           Vector3.back, button.PressBounds))
                        continue;
                    
                    // Projects the joint pos onto the normal out of the button and gets the distance
                    float pokeDistance =
                        Vector3.Dot(joint.JointPosition - buttonBaseTransform.position,
                            back);
                    
                    pokeDistance -= joint.JointRadius;
                    if (pokeDistance < 0f)
                    {
                        pokeDistance = 0f;
                    }

                    closestDistance = Math.Min(pokeDistance, closestDistance);
                }

                button.ButtonSurface.transform.position = buttonBaseTransform.position +
                                                          -closestDistance * buttonBaseTransform.forward;

                float pressPercentage = 1 - (closestDistance / button.MaxPressedDistance);
                if (pressPercentage > button.SelectStartPercentage)
                {
                    if (!button.IsSelected)
                    {
                        button.InteractStarted();
                    }
                }
                else if (button.IsSelected)
                {
                    button.InteractStopped();
                }

            }
        }

        private void DetectProximates(RhinoxHand hand, Vector3 referenceJointPos)
        {
            List<GRPLInteractable> currentProximates;
            switch (hand)
            {
                case RhinoxHand.Left:
                    currentProximates = _leftHandProximites;
                    break;
                case RhinoxHand.Right:
                    currentProximates = _rightHandProximites;
                    break;
                default:
                    PLog.Error<GrappleItLogger>($"[{this.GetType()}:DetectProximates], function called with invalid hand {hand}");
                    return;
            }
            
            
            var newProximateInteractables =
                new Dictionary<float, GRPLInteractable>();
            float proximateRadiusSqr = _proximateRadius * _proximateRadius;
            foreach (var interactable in _interactables)
            {
                // Calculate the vector from th joint to the interactable
                Vector3 fromJointToInteractable = interactable.transform.position - referenceJointPos;
                
                // Calculate the distance from the interactable to the reference pos
                float sqrDistance = Vector3.SqrMagnitude(fromJointToInteractable);
                
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
                // If the new square distance is smaller than that distance
                // Replace it in the list
                var furthestPair = new KeyValuePair<float, GRPLInteractable>(float.MaxValue, null);
                foreach (var pair in newProximateInteractables)
                {
                    if (pair.Key < furthestPair.Key)
                        furthestPair = pair;
                }

                if (furthestPair.Key > sqrDistance)
                {
                    newProximateInteractables.Remove(furthestPair.Key);
                    newProximateInteractables.Add(sqrDistance,interactable);
                }

            }
            
            // Detect which proximates are new
            // Do this by selecting all the pairs that the currentProximates list does not contain
            var newProximates = newProximateInteractables.Where(x => currentProximates.Contains(x.Value) == false);
            foreach (var pair in newProximates)
                pair.Value.ProximityStarted();

            // Save a copy of the current proximates as the previousProximates
            var previousProximates = new List<GRPLInteractable>(currentProximates);
            
            // Set the new current proximates
            currentProximates.Clear();
            foreach (var pair in newProximateInteractables)
                currentProximates.Add(pair.Value);
            
            // Detect which proximates are not valid anymore
            // Do this by selecting all proximates in previousProximates that the currentProximates does not contain
            var stoppedProximates = previousProximates.Where(x => currentProximates.Contains(x) == false);
            foreach (var proximate in stoppedProximates)
                proximate.ProximityStopped();
            
        }
        
        /// <summary>
        /// Checks if the given position is in front of given plane. The plane is defined with a point and forward vector.
        /// </summary>
        /// <param name="pos">The position to check</param>
        /// <param name="planePosition">A point on the desired plane</param>
        /// <param name="planeNormal">The normal of the desired plane</param>
        /// <returns></returns>
        private bool IsPositionInFrontOfPlane(Vector3 pos, Vector3 planePosition, Vector3 planeNormal)
        {
            // Check if the finger pos is in front of the button
            // Check the dot product of the forward of the base and the vector from the base to the joint pos
            Vector3 toPos = pos - planePosition;
            float dotValue = Vector3.Dot(planeNormal, toPos);
            return dotValue >= 0;
        }

        /// <summary>
        /// Checks whether the projected position of the given point on the plane (defined by
        /// the planePosition and PlaneForward) is within the given bounds.
        /// </summary>
        /// <param name="point">The point to project.</param>
        /// <param name="planePosition">A point on the desired plane</param>
        /// <param name="planeNormal">The normal of the desired plane</param>
        /// <param name="bounds">The bounds to check</param>
        /// <returns></returns>
        private bool IsPlaneProjectedPointInBounds(Vector3 point, Vector3 planePosition, Vector3 planeNormal,
            Bounds bounds)
        {
            // Project the position on the plane defined by the given position and forward
            var projectedPos = Vector3.ProjectOnPlane(point, planeNormal) +
                               Vector3.Dot(planePosition, planeNormal) *
                               planeNormal;
            return bounds.Contains(projectedPos);
        }
        
        
        private void OnDestroy()
        {
            GRPLInteractable.InteractableCreated -= OnInteractableCreated;
            GRPLInteractable.InteractableDestroyed -= OnInteractableDestroyed;

        }

        private void OnInteractableCreated(GRPLInteractable interactable)
        {
            Debug.Log(((GRPLButtonInteractable)interactable).gameObject.name + " Added");
            _interactables.Add(interactable);
        }

        private void OnInteractableDestroyed(GRPLInteractable interactable)
        {
            Debug.Log(((GRPLButtonInteractable)interactable).gameObject.name + " Removed");
            _interactables.Remove(interactable);
            
        }
    }
}