using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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

    public class RhinoxJoint
    {
        public XRHandJointID JointID = XRHandJointID.Invalid;

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
        public XRHandJointID JointID;
        public Rigidbody JointRigidbody;
        public CapsuleCollider JointCollider;
    }


    public class JointManager : MonoBehaviour
    {
        #region XRHands fields

        private XRHandSubsystem _subsystem;

        #endregion


        private List<RhinoxJoint> _leftHandJoints = new List<RhinoxJoint>();
        private List<RhinoxJoint> _rightHandJoints = new List<RhinoxJoint>();

        private List<RhinoxJointCapsule> _leftHandColliders = new List<RhinoxJointCapsule>();
        private List<RhinoxJointCapsule> _rightHandColliders = new List<RhinoxJointCapsule>();

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
            if (_subsystem != null)
                return;

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
            InitializeJointColliders(hand.handedness.ToRhinoxHand());

            switch (hand.handedness)
            {
                case Handedness.Invalid:
                    break;
                case Handedness.Left:
                    IsLeftHandTracked = true;
                    break;
                case Handedness.Right:
                    IsRightHandTracked = true;
                    break;
                default:
                    break;
            }

            TrackingAcquired?.Invoke(XrHandednessToRhinoxHand(hand.handedness));
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
                    break;
            }

            TrackingLost?.Invoke(XrHandednessToRhinoxHand(hand.handedness));
        }

        private Hand XrHandednessToRhinoxHand(Handedness hand)
        {
            switch (hand)
            {
                case Handedness.Invalid:
                    return Hand.Invalid;
                case Handedness.Left:
                    return Hand.Left;
                case Handedness.Right:
                    return Hand.Right;
                default:
                    return Hand.Invalid;
            }
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

            InitializeJointColliders(hand.ToRhinoxHand());
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

        private void InitializeJointColliders(Hand handedness)
        {
            //Check the parent object. If it doesn't exist, create it.
            GameObject parent;
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
            List<RhinoxJointCapsule> currentList;
            switch (handedness)
            {
                case Hand.Left:
                    if (_leftHandColliders == null || _leftHandColliders.Count != _leftHandJoints.Count)
                        _leftHandColliders = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_leftHandJoints.Count]);
                    currentList = _leftHandColliders;
                    break;
                case Hand.Right:
                    return;
                    if (_rightHandColliders == null || _rightHandColliders.Count != _rightHandJoints.Count - 1)
                        _rightHandColliders = new List<RhinoxJointCapsule>(new RhinoxJointCapsule[_rightHandJoints.Count]);
                    currentList = _rightHandColliders;
                    break;
                default:
                    return;
            }


            //------------------------------------
            //    !! LEFT HAND FOR NOW!!
            //------------------------------------

            //Loop over the capsules and process them
            for (var index = currentList.Count - 1; index > 0; index--)
            {
                TryGetJointFromHandById((XRHandJointID)index + 1, handedness, out var joint);
                var jointCapsule = _leftHandColliders[index] ?? (_leftHandColliders[index] = new RhinoxJointCapsule());
                jointCapsule.JointID = joint.JointID;

                // if (jointCapsule.JointRigidbody == null)
                // {
                //     jointCapsule.JointRigidbody = new GameObject(joint.JointID.ToString() + "_CapsuleRigidbody").AddComponent<Rigidbody>();
                //     jointCapsule.JointRigidbody.mass = 1.0f;
                //     jointCapsule.JointRigidbody.isKinematic = true;
                //     jointCapsule.JointRigidbody.useGravity = false;
                //     jointCapsule.JointRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                // }
                //
                // var rbGo = jointCapsule.JointRigidbody.gameObject;
                // rbGo.transform.SetParent(_leftHandCollidersParent.transform, false);
                // rbGo.transform.position = joint.JointPosition;
                // rbGo.transform.rotation = joint.JointRotation;

                if (jointCapsule.JointCollider == null)
                {
                    jointCapsule.JointCollider =
                        new GameObject(joint.JointID.ToString() + "_CapsuleCollider")
                            .AddComponent<CapsuleCollider>();
                    jointCapsule.JointCollider.isTrigger = true;
                }

                //Get the transform of the connected joints
                var p0 = _leftHandJoints[index].JointPosition;
                var p1 = !joint.JointID.ToString().Contains("Metacarpal") ? _leftHandJoints[index - 1].JointPosition : _leftHandJoints[0].JointPosition;

                var delta = p1 - p0;
                var colliderLength = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);

                jointCapsule.JointCollider.radius = joint.JointRadius;
                jointCapsule.JointCollider.height = colliderLength;
                jointCapsule.JointCollider.direction = 0;
                jointCapsule.JointCollider.center = Vector3.right * colliderLength * 0.5f;


                var colliderGO = jointCapsule.JointCollider.gameObject;
                colliderGO.transform.SetParent(_leftHandCollidersParent.transform, false);
                colliderGO.transform.localPosition = p0;
                colliderGO.transform.localRotation = rot;
            }




        }

        #endregion

        #region Public Functions
        public bool TryGetJointsFromHand(Hand hand, out List<RhinoxJoint> boneList)
        {
            switch (hand)
            {
                case Hand.Left:
                    boneList = _leftHandJoints;
                    return IsLeftHandTracked;
                case Hand.Right:
                    boneList = _rightHandJoints;
                    return IsRightHandTracked;
                default:
                    boneList = new List<RhinoxJoint>();
                    return false;
            }

        }

        //public List<RhinoxJoint> TryGetJointsFromHand(Handedness hand)
        //{
        //    switch (hand)
        //    {

        //        case Handedness.Left:
        //            return _leftHandJoints;
        //        case Handedness.Right:
        //            return _rightHandJoints;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(hand), hand, null);
        //    }
        //}

        //public RhinoxJoint GetJoint(XRHandJointID jointID, Handedness handedness)
        //{
        //    if (handedness == Handedness.Left)
        //        return _leftHandJoints.Find(x => x.JointID == jointID);
        //    if (handedness == Handedness.Right)
        //        return _rightHandJoints.Find(x => x.JointID == jointID);

        //    return null;
        //}

        //public RhinoxJoint GetJoint(XRHandJointID jointID, Hand handedness)
        //{
        //    if (handedness == Hand.Left)
        //        return _leftHandJoints.Find(x => x.JointID == jointID);
        //    if (handedness == Hand.Right)
        //        return _rightHandJoints.Find(x => x.JointID == jointID);

        //    return null;
        //}

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
            }

            joint = null;
            return false;
        }

        #endregion
    }
}