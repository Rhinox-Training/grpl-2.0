using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class InteractableManager :  MonoBehaviour
    {
        public InteractableManager Instance;

        private JointManager _jointManager;
        private List<GRPLInteractable> _interactables = null;

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

        private Vector3 _projectedPos;
        
        private void Update()
        {
            if(_jointManager == null)
                return;
            
            if(!_jointManager.TryGetJointFromHandById(XRHandJointID.IndexTip,RhinoxHand.Right,out var joint))
                return;
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
                    
                    // Create the bounds of the button as if it was not pressed
                    Vector3 boundsCenter = button.gameObject.GetObjectBounds().center + (button.MaxPressedDistance / 2f) * back;
                    
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
            _projectedPos = projectedPos;
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