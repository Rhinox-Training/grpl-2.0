using System;
using System.Collections.Generic;
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
        Both
    }

    public sealed class RhinoxBone
    {
        public XRHandJointID BoneId { get; private set; } = XRHandJointID.Invalid;

        public Vector3 BonePosition = Vector3.zero;
        public Quaternion BoneRotation = Quaternion.identity;

        public Vector3 Forward;
        
        public RhinoxBone(XRHandJointID boneId)
        {
            BoneId = boneId;
        }
    }

    public sealed class BoneManager : MonoBehaviour
    {
        #region XRHands fields

        private XRHandSubsystem _subsystem;

        #endregion
        

        private List<RhinoxBone> _leftHandBones = new List<RhinoxBone>();
        private List<RhinoxBone> _rightHandBones = new List<RhinoxBone>();

        public bool IsInitialised { get; private set; } = false;


        public BoneManager()
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
                        var rootPose = _subsystem.leftHand.rootPose;// GetJoint(XRHandJointID.Wrist).TryGetPose();
                        _leftHandBones[0].BonePosition = rootPose.position;
                        _leftHandBones[0].BoneRotation = rootPose.rotation;
                        _leftHandBones[0].Forward = rootPose.forward;
                        break;
                    }
                case Handedness.Right:
                    {
                        var rootPose = _subsystem.rightHand.rootPose;// GetJoint(XRHandJointID.Wrist).TryGetPose();
                        _rightHandBones[0].BonePosition = rootPose.position;
                        _rightHandBones[0].BoneRotation = rootPose.rotation;
                        _rightHandBones[0].Forward = rootPose.forward;
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
                            if (jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker or XRHandJointID.EndMarker)
                                continue;

                            var currentBone = _leftHandBones[(int)jointId - 1];

                            _subsystem.leftHand.GetJoint(jointId).TryGetPose(out var pose);

                            currentBone.BonePosition = pose.position;
                            currentBone.BoneRotation = pose.rotation;
                            currentBone.Forward = pose.forward;
                        }
                        break;
                    }
                case Handedness.Right:
                    {
                        foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
                        {
                            if (jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker or XRHandJointID.EndMarker)
                                continue;

                            var currentBone = _rightHandBones[(int)jointId - 1];

                            _subsystem.rightHand.GetJoint(jointId).TryGetPose(out var pose);
                            
                            currentBone.BonePosition = pose.position;
                            currentBone.BoneRotation = pose.rotation;
                            currentBone.Forward = pose.forward;

                        }
                        break;
                    }
                case Handedness.Invalid:
                    break;
            }
        }

        private void InitializeHandJoints()
        {
            //Initialize wrist
            {
                var leftWristJoint = new RhinoxBone(XRHandJointID.Wrist);
                _leftHandBones.Add(leftWristJoint);

                var rightWristJoint = new RhinoxBone(XRHandJointID.Wrist);
                _rightHandBones.Add(rightWristJoint);
            }

            //Initialize finger joints
            foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
            {
                if (jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker or XRHandJointID.EndMarker)
                    continue;

                var leftBone = new RhinoxBone(jointId);
                _leftHandBones.Add(leftBone);
                var rightBone = new RhinoxBone(jointId);
                _rightHandBones.Add(rightBone);
            }

            IsInitialised = true;
        }

        public List<RhinoxBone> GetBonesFromHand(Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    return _leftHandBones;
                case Hand.Right:
                    return _rightHandBones;
            }

            return new List<RhinoxBone>();
        }

        public List<RhinoxBone> GetBonesFromHand(Handedness hand)
        {
            switch (hand)
            {

                case Handedness.Left:
                    return _leftHandBones;
                case Handedness.Right:
                    return _rightHandBones;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hand), hand, null);
            }
        }

        [CanBeNull]
        public RhinoxBone GetBone(XRHandJointID jointID,Handedness handedness)
        {
            if (handedness == Handedness.Left)
                return _leftHandBones.Find(x => x.BoneId == jointID);
            if (handedness == Handedness.Right)
                return _rightHandBones.Find(x => x.BoneId == jointID);

            return null;
        }

        public RhinoxBone GetBoneFromHandById(Hand hand, XRHandJointID boneId)
        {
            switch (hand)
            {
                case Hand.Left:
                    return _leftHandBones.First(rhinoxBone => rhinoxBone.BoneId == boneId);
                case Hand.Right:
                    return _rightHandBones.First(rhinoxBone => rhinoxBone.BoneId == boneId);
            }

            return null;
        }
    }
}