using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Rhinox.XR.Grapple
{
    public enum Hand
    {
        Left,
        Right,
        Both,
        Invalid
    }

    public sealed class RhinoxJoint
    {
        public readonly XRHandJointID JointID;

        public Vector3 JointPosition = Vector3.zero;
        public Quaternion JointRotation = Quaternion.identity;

        public Vector3 Forward;
        public float JointRadius;
        
        public RhinoxJoint(XRHandJointID jointID)
        {
            JointID = jointID;
        }
    }

    public class RhinoxJointCapsule
    {
        public Rigidbody JointRigidbody;
        public CapsuleCollider JointCollider;

        public RhinoxJoint StartJoint;
        public RhinoxJoint EndJoint;
    }
    
    
    public sealed class JointManager : MonoBehaviour
    {
        #region XRHands fields

        private XRHandSubsystem _subsystem;

        #endregion
        

        private List<RhinoxJoint> _leftHandJoints = new List<RhinoxJoint>();
        private List<RhinoxJoint> _rightHandJoints = new List<RhinoxJoint>();

        private List<RhinoxJointCapsule> _leftHandCapsules = new List<RhinoxJointCapsule>();
        private List<RhinoxJointCapsule> _rightHandColliders = new List<RhinoxJointCapsule>();

        private GameObject _leftHandCollidersParent;
        private GameObject _rightHandCollidersParent;
        public bool IsInitialised { get; private set; } = false;

        public JointManager()
        {
            InitializeHandJoints();

        }

        private void TryEnsureInitialized()
        {
            if (_subsystem != null)
                return;
            
            //Load the subsystem if possible
            _subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();

            if (_subsystem == null)
                return;

            _subsystem.updatedHands += OnUpdatedHands;
            _subsystem.trackingAcquired += OnTrackingAcquired;
        }

        private void OnTrackingAcquired(XRHand hand)
        {
            InitializeJointCapsules(hand.handedness.ToRhinoxHand());
        }

        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            //This update types timing is similar to that of the Update
            //Use this for game logic
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
                return;

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != XRHandSubsystem.UpdateSuccessFlags.None)
                UpdateRootPose(Handedness.Left);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != XRHandSubsystem.UpdateSuccessFlags.None)
                UpdateJoints(Handedness.Left);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != XRHandSubsystem.UpdateSuccessFlags.None)
                UpdateRootPose(Handedness.Right);

            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != XRHandSubsystem.UpdateSuccessFlags.None)
                UpdateJoints(Handedness.Right);

        }

        private void Update()
        {
            TryEnsureInitialized();
        }

        private void UpdateRootPose(Handedness hand)
        {
            switch (hand)
            {
                case Handedness.Left:
                {
                        var rootPose = _subsystem.leftHand.rootPose;
                        _leftHandJoints[0].JointPosition = rootPose.position;
                        _leftHandJoints[0].JointRotation = rootPose.rotation;
                        _leftHandJoints[0].Forward = rootPose.forward;

                        if (_subsystem.leftHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                            _leftHandJoints[0].JointRadius = radius;
                        break;
                    }
                case Handedness.Right:
                    {
                        var rootPose = _subsystem.rightHand.rootPose;
                        _rightHandJoints[0].JointPosition = rootPose.position;
                        _rightHandJoints[0].JointRotation = rootPose.rotation;
                        _rightHandJoints[0].Forward = rootPose.forward;

                        if (_subsystem.rightHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                            _leftHandJoints[0].JointRadius = radius;
                        break;

                    }
                case Handedness.Invalid:
                    break;
            }
        }

        private void UpdateJoints(Handedness hand)
        {
            switch (hand)
            {
                case Handedness.Left:
                    {
                        foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
                        {
                            if (jointId is XRHandJointID.Invalid or XRHandJointID.EndMarker)
                                continue;

                            var currentJoint = _leftHandJoints[(int)jointId - 1];
                            
                            var subsystemJoint = _subsystem.leftHand.GetJoint(jointId);
                            subsystemJoint.TryGetPose(out var pose);
                            
                            currentJoint.JointPosition = pose.position;
                            currentJoint.JointRotation = pose.rotation;
                            currentJoint.Forward = pose.forward;
                            
                            if (subsystemJoint.TryGetRadius(out var radius))
                                currentJoint.JointRadius = radius;

                        }
                        break;
                    }
                case Handedness.Right:
                    {
                        foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
                        {
                            if (jointId is XRHandJointID.Invalid or XRHandJointID.EndMarker)
                                continue;

                            var currentJoint = _rightHandJoints[(int)jointId - 1];

                            var subsystemJoint = _subsystem.rightHand.GetJoint(jointId);
                            subsystemJoint.TryGetPose(out var pose);
                            
                            currentJoint.JointPosition = pose.position;
                            currentJoint.JointRotation = pose.rotation;
                            currentJoint.Forward = pose.forward;
                            
                            if (subsystemJoint.TryGetRadius(out var radius))
                                currentJoint.JointRadius = radius;
                        }
                        break;
                    }
                case Handedness.Invalid:
                    break;
                
            }
            
            UpdateCapsuleColliders(hand.ToRhinoxHand());
        }

        private void InitializeHandJoints()
        {
            //Initialize wrist
            {
                var leftWristJoint = new RhinoxJoint(XRHandJointID.Wrist);
                _leftHandJoints.Add(leftWristJoint);

                var rightWristJoint = new RhinoxJoint(XRHandJointID.Wrist);
                _rightHandJoints.Add(rightWristJoint);
            }

            //Initialize finger joints
            foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
            {
                if (jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker or XRHandJointID.EndMarker)
                    continue;

                var leftJoint = new RhinoxJoint(jointId);
                _leftHandJoints.Add(leftJoint);
                var rightJoint = new RhinoxJoint(jointId);
                _rightHandJoints.Add(rightJoint);
            }

            IsInitialised = true;
        }

        private void InitializeJointCapsules(Hand handedness)
        {
            //Check the parent object. If it doesn't exist, create it.
            switch (handedness)
            {
                case Hand.Left:
                {
                    if (_leftHandCollidersParent)
                        break;
                    _leftHandCollidersParent = new GameObject($"{handedness}_Capsules");
                    _leftHandCollidersParent.transform.SetParent(transform, false);
                    _leftHandCollidersParent.transform.localPosition = Vector3.zero;
                    _leftHandCollidersParent.transform.localRotation = Quaternion.identity;
                    break;
                }
                case Hand.Right:
                {
                    if (_rightHandCollidersParent)
                        break;
                    _rightHandCollidersParent = new GameObject($"{handedness}_Capsules");
                    _rightHandCollidersParent.transform.SetParent(transform, false);
                    _rightHandCollidersParent.transform.localPosition = Vector3.zero;
                    _rightHandCollidersParent.transform.localRotation = Quaternion.identity;
                    break;
                }
                case Hand.Both:
                    return;
                default:
                    return;
            }
            
            //Initialize capsule list
            List<RhinoxJointCapsule> currentList = null;
            switch (handedness)
            {
                case Hand.Left:
                    if (_leftHandCapsules == null || _leftHandCapsules.Count != _leftHandJoints.Count)
                        _leftHandCapsules = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_leftHandJoints.Count]);
                    currentList = _leftHandCapsules;
                    break;
                case Hand.Right:
                    return;
                    // if (_rightHandColliders == null || _rightHandColliders.Count != _rightHandJoints.Count - 1)
                    //     _rightHandColliders = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_rightHandJoints.Count]);
                    // currentList = _rightHandColliders;
                    // break;
            }
            
            
            //------------------------------------
            //    !! LEFT HAND FOR NOW!!
            //------------------------------------
            if(currentList != null)
             return;
            
            //Loop over the capsules and process them
            for (var index = currentList.Count - 1; index > 0; index--)
            {
                var joint = GetJoint((XRHandJointID)index + 1, handedness);
                var jointCapsule = _leftHandCapsules[index] ?? (_leftHandCapsules[index] = new RhinoxJointCapsule());
                jointCapsule.StartJoint = joint;

                if (jointCapsule.JointRigidbody == null)
                {
                    jointCapsule.JointRigidbody = new GameObject(joint.JointID.ToString()+ "_Rigidbody").AddComponent<Rigidbody>();
                    jointCapsule.JointRigidbody.mass = 1.0f;
                    jointCapsule.JointRigidbody.isKinematic = true;
                    jointCapsule.JointRigidbody.useGravity = false;
                    jointCapsule.JointRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
                
                var rbGo = jointCapsule.JointRigidbody.gameObject;
                rbGo.transform.SetParent(_leftHandCollidersParent.transform, false);
                rbGo.transform.position = joint.JointPosition;
                rbGo.transform.rotation = joint.JointRotation;

                if (jointCapsule.JointCollider == null)
                {
                    jointCapsule.JointCollider = new GameObject(joint.JointID.ToString() + "_CapsuleCollider")
                            .AddComponent<CapsuleCollider>();
                    jointCapsule.JointCollider.isTrigger = false;
                }

                //Get the transform of the connected joints
                var p0 = _leftHandJoints[index].JointPosition;
                var endJoint = !joint.JointID.ToString().Contains("Metacarpal") ? _leftHandJoints[index-1] : _leftHandJoints[0];
                var p1 = endJoint.JointPosition;
                var delta = p1 - p0;
                var colliderLength = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                jointCapsule.JointCollider.radius = 0.01f;
                jointCapsule.JointCollider.height = colliderLength;
                jointCapsule.JointCollider.direction = 0;
                jointCapsule.JointCollider.center = Vector3.right * colliderLength * 0.5f;
                
                
                var colliderGo = jointCapsule.JointCollider.gameObject;
                colliderGo.transform.SetParent(rbGo.transform,false);
                colliderGo.transform.rotation = rot;

                jointCapsule.EndJoint = endJoint;
            }
            
        }

        private void FixedUpdate()
        {
            if (_leftHandCollidersParent.activeSelf)
            {
                for (var i = 1; i < _leftHandCapsules.Count; i++)
                {
                    var capsule = _leftHandCapsules[i];
                    var joint = _leftHandJoints[i];
                    var rigidBodyGo = capsule.JointRigidbody.gameObject;
                    if (rigidBodyGo.activeSelf)
                    {
                        capsule.JointRigidbody.MovePosition(joint.JointPosition);
                        capsule.JointRigidbody.MoveRotation(joint.JointRotation);
                    }
                    else
                    {
                        rigidBodyGo.SetActive(true);
                        capsule.JointRigidbody.position = joint.JointPosition;
                        capsule.JointRigidbody.rotation = joint.JointRotation;
                    }
                }
            }
        }

        private void UpdateCapsuleColliders(Hand handedness)
        {
            switch (handedness)
            {
                case Hand.Left:
                    break;
                case Hand.Right:
                    break;
                default:
                    return;
            }

            foreach (var capsule in _leftHandCapsules)
            {
                if(capsule == null)
                    continue;
                //Get the transform of the connected joints
                var p0 = capsule.StartJoint.JointPosition;
                var p1 = capsule.EndJoint.JointPosition;
                var delta = p1 - p0;
                var colliderLength = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                capsule.JointCollider.gameObject.transform.rotation = rot;
            }
        }
        public List<RhinoxJoint> GetJointsFromHand(Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    return _leftHandJoints;
                case Hand.Right:
                    return _rightHandJoints;
            }

            return new List<RhinoxJoint>();
        }

        public List<RhinoxJoint> GetJointsFromHand(Handedness hand)
        {
            switch (hand)
            {

                case Handedness.Left:
                    return _leftHandJoints;
                case Handedness.Right:
                    return _rightHandJoints;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hand), hand, null);
            }
        }

        public RhinoxJoint GetJoint(XRHandJointID jointID,Handedness handedness)
        {
            if (handedness == Handedness.Left)
                return _leftHandJoints.Find(x => x.JointID == jointID);
            if (handedness == Handedness.Right)
                return _rightHandJoints.Find(x => x.JointID == jointID);

            return null;
        }

        public RhinoxJoint GetJoint(XRHandJointID jointID, Hand handedness)
        {
            if (handedness == Hand.Left)
                return _leftHandJoints.Find(x => x.JointID == jointID);
            if (handedness == Hand.Right)
                return _rightHandJoints.Find(x => x.JointID == jointID);

            return null;
        }

        public RhinoxJoint GetJointFromHandById(Hand hand, XRHandJointID jointID)
        {
            switch (hand)
            {
                case Hand.Left:
                    return _leftHandJoints.First(rhinoxJoint => rhinoxJoint.JointID == jointID);
                case Hand.Right:
                    return _rightHandJoints.First(rhinoxJoint => rhinoxJoint.JointID == jointID);
            }

            return null;
        }
        
    }
}