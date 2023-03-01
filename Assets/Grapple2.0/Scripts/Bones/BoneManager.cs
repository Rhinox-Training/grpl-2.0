using System;
using System.Collections;
using System.Collections.Generic;
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
        public XRHandJointID BoneId { get; private set; }

        public Vector3 BonePosition;
        public Quaternion BoneRotation;
        
        public RhinoxBone(XRHandJointID boneId)
        {
            BoneId = boneId;
        }
    }

    public class BoneManager : MonoBehaviour
    {
        #region XRHands fields

        XRHandSubsystem _subsystem;

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
            
            if(_subsystem == null)
                return;

            _subsystem.updatedHands += OnUpdatedHands;
        }

        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            //This update types timing is similar to that of the Update
            //Use this for game logic
            if(updateType == XRHandSubsystem.UpdateType.Dynamic)
                return;
        }

        private void Update() => TryEnsureInitialized();

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
                if(jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker or XRHandJointID.EndMarker)
                    continue;
                
                var leftBone = new RhinoxBone(jointId);
                _leftHandBones.Add(leftBone);
                var rightBone = new RhinoxBone(jointId);
                _rightHandBones.Add(rightBone);
            }
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
    }

}