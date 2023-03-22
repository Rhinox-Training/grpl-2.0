using System;
using System.Collections.Generic;
using System.Linq;
using PlasticPipe.PlasticProtocol.Client.Proxies;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class InteractableManager :  MonoBehaviour
    {
        public InteractableManager Instance;

        private JointManager _jointManager;
        private List<GRPLInteractable> _interactables = null;
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
            var interactJoints = _jointManager.GetJointsFromBothHand(XRHandJointID.IndexTip);
            var buttons = _interactables.OfType<GRPLButtonInteractable>().ToArray();

            foreach (var button in buttons)
            {
                if(!button.isActiveAndEnabled)
                    continue;
                
                float closestDistance = button.MaxPressedDistance;

                // Cache the button fields that get reused
                Transform buttonBaseTransform = button.ButtonBaseTransform;
                
                //Loop over the joints
                foreach (RhinoxJoint joint in interactJoints)
                {
                    var back = -buttonBaseTransform.forward;

                    // Check if the finger pos is in front of the button
                    // Check the dot product of the forward of the base and the vector from the base to the joint pos
                    Vector3 toJoint = joint.JointPosition - buttonBaseTransform.position;
                    float dotValue = Vector3.Dot(back, toJoint);
                    if (dotValue < 0)
                        continue;

                    // Project the joints position on the plane of the button base 
                    _projectedPos = Vector3.ProjectOnPlane(joint.JointPosition, back) +
                                    Vector3.Dot(buttonBaseTransform.position, back) *
                                    back;

                    // If the projected pos is not within the bounding box of the button base
                    // continue;
                    if (!button.gameObject.GetObjectBounds().Contains(_projectedPos))
                        continue;
                    
                    // Check if the distance is correct
                    float pokeDistance = Vector3.Distance(joint.JointPosition, buttonBaseTransform.position);
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

            }
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

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_projectedPos,.05f);
        }
    }
}