using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;

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
        private List<RhinoxJointCapsule> _rightHandCapsules = new List<RhinoxJointCapsule>();

        private GameObject _leftHandCollidersParent;
        private GameObject _rightHandCollidersParent;
        public bool IsInitialised { get; private set; } = false;

        public bool IsLeftHandTracked { get; private set; } = false;
        public bool IsRightHandTracked { get; private set; } = false;

        public Action<Hand> TrackingAcquired;
        public Action<Hand> TrackingLost;

        public JointManager()
        {
            InitializeHandJoints();
        }

        private void TryEnsureInitialized()
        {
            //Load the subsystem if possible
            _subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();

            if (_subsystem == null)
                return;

            _subsystem.updatedHands += OnUpdatedHands;
            _subsystem.trackingAcquired += OnTrackingAcquired;
            _subsystem.trackingLost += OnTrackingLost;
        }

        private void OnTrackingAcquired(XRHand hand)
        {
            InitializeJointCapsules(hand.handedness.ToRhinoxHand());

            switch (hand.handedness)
            {
                case Handedness.Left:
                    IsLeftHandTracked = true;
                    break;
                case Handedness.Right:
                    IsRightHandTracked = true;
                    break;
                default:
                    Debug.LogError($"{nameof(JointManager)} - {nameof(OnTrackingAcquired)}, function called with incorrect hand {hand}. Only left or right supported!");
                    break;
            }

            TrackingAcquired?.Invoke(hand.handedness.ToRhinoxHand());
        }

        private void OnTrackingLost(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Invalid:
                    break;
                case Handedness.Left:
                    IsLeftHandTracked = false;
                    break;
                case Handedness.Right:
                    IsRightHandTracked = false;
                    break;
                default:
                    Debug.LogError($"{nameof(JointManager)} - {nameof(OnTrackingLost)}, function called with incorrect hand {hand}. Only left or right supported!");
                    break;
            }

            TrackingLost?.Invoke(hand.handedness.ToRhinoxHand());
        }

        #region Update Logic
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
            if (_subsystem == null)
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
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(UpdateRootPose)}, function called with incorrect hand {hand}. Only left or right supported!");
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
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(UpdateJoints)}, function called with incorrect hand {hand}. Only left or right supported!");
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
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(InitializeJointCapsules)}, function called with incorrect hand {handedness}. Only left or right supported!");
                    break;
            }

            //Initialize capsule list
            List<RhinoxJointCapsule> currentList = null; 
            GameObject parent;
            List<RhinoxJoint> joints;
            switch (handedness)
            {
                case Hand.Left:
                    if (_leftHandCapsules == null || _leftHandCapsules.Count != _leftHandJoints.Count)
                        _leftHandCapsules = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_leftHandJoints.Count]);
                    currentList = _leftHandCapsules;
                    parent = _leftHandCollidersParent;
                    joints = _leftHandJoints;
                    break;
                case Hand.Right:
                    if (_rightHandCapsules == null || _rightHandCapsules.Count != _rightHandJoints.Count)
                        _rightHandCapsules = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_rightHandJoints.Count]);
                    currentList = _rightHandCapsules;
                    parent = _rightHandCollidersParent;
                    joints = _rightHandJoints;
                    break;
                default:
                    return;
            }

            //Loop over the capsules and process them
            for (var index = currentList.Count - 1; index > 0; index--)
            {
                TryGetJointFromHandById((XRHandJointID)index + 1, handedness, out var joint);
                var jointCapsule = currentList[index] ?? (currentList[index] = new RhinoxJointCapsule());
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
                rbGo.transform.SetParent(parent.transform, false);
                rbGo.transform.position = joint.JointPosition;
                rbGo.transform.rotation = joint.JointRotation;

                if (jointCapsule.JointCollider == null)
                {
                    jointCapsule.JointCollider = new GameObject(joint.JointID.ToString() + "_CapsuleCollider")
                            .AddComponent<CapsuleCollider>();
                    jointCapsule.JointCollider.isTrigger = true;
                }

                //Get the transform of the connected joints
                var p0 = joints[index].JointPosition;
                var endJoint = !joint.JointID.ToString().Contains("Metacarpal") ? joints[index-1] : joints[0];
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
            FixedUpdateCapsules(Hand.Left);
            FixedUpdateCapsules(Hand.Right);
        }

        private void FixedUpdateCapsules(Hand hand)
        {
            GameObject parent;
            switch (hand)
            {
                case Hand.Left:
                    parent = _leftHandCollidersParent;
                    break;
                case Hand.Right:
                    parent = _rightHandCollidersParent;
                    break;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(FixedUpdateCapsules)}, function called with incorrect hand {hand}. Only left or right supported!");
                    return;
            }
            
            if(!parent)
                return;

            List<RhinoxJointCapsule> list = new List<RhinoxJointCapsule>();
            var joints = new List<RhinoxJoint>();
            switch (hand)
            {
                case Hand.Left:
                    list = _leftHandCapsules;
                    joints = _leftHandJoints;
                    break;
                case Hand.Right:
                    list = _rightHandCapsules;
                    joints = _rightHandJoints;
                    break;
            }

            if (parent.activeSelf)
            {
                for (var i = 1; i < list.Count; i++)
                {
                    var capsule = list[i];
                    var joint = joints[i];
                    var rigidBodyGo = capsule.JointRigidbody.gameObject;
                    capsule.JointCollider.isTrigger = false;
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
            List<RhinoxJointCapsule> capsules;
            switch (handedness)
            {
                case Hand.Left:
                    capsules = _leftHandCapsules;
                    break;
                case Hand.Right:
                    capsules = _rightHandCapsules;
                    break;
                default:
                    return;
            }
            
            foreach (var capsule in capsules)
            {
                if(capsule == null)
                    continue;
                //Get the transform of the connected joints
                var p0 = capsule.StartJoint.JointPosition;
                var p1 = capsule.EndJoint.JointPosition;
                var delta = p1 - p0;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                capsule.JointCollider.gameObject.transform.rotation = rot;
            }
        }
        public bool TryGetJointsFromHand(Hand hand, out List<RhinoxJoint> jointList)
        {
            switch (hand)
            {
                case Hand.Left:
                    jointList = _leftHandJoints;
                    return IsLeftHandTracked;
                case Hand.Right:
                    jointList = _rightHandJoints;
                    return IsRightHandTracked;
                default:
                    jointList = new List<RhinoxJoint>();
                    return false;
            }

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
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(GetJointsFromHand)}, function called with incorrect hand {hand}. Only left or right supported!");
                    return new List<RhinoxJoint>();
            }
        }
        
        public bool TryGetJointFromHandById(XRHandJointID jointID, Hand hand, out RhinoxJoint joint)
        {
            switch (hand)
            {
                case Hand.Left:
                    joint = _leftHandJoints.First(rhinoxJoint => rhinoxJoint.JointID == jointID);
                    return IsLeftHandTracked && joint != null;
                case Hand.Right:
                    joint = _rightHandJoints.First(rhinoxJoint => rhinoxJoint.JointID == jointID);
                    return IsRightHandTracked && joint != null;
                default:
                    Debug.LogError($"{nameof(JointManager)} - {nameof(TryGetJointFromHandById)}, function called with incorrect hand {hand}. Only left or right supported!");
                    joint = null;
                    return false;
            }
        }

        #endregion
    }
}