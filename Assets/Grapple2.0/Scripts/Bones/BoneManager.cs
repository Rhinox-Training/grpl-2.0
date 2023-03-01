using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public string BoneName { get; private set; } = null;
        public Transform BoneTransform { get; private set; } = null;
        public List<CapsuleCollider> BoneCollisionCapsules { get; private set; } = null;

        public RhinoxBone(string boneName, Transform boneTransform, List<CapsuleCollider> boneCollisionCapsules)
        {
            BoneName = boneName;
            BoneTransform = boneTransform;
            BoneCollisionCapsules = boneCollisionCapsules;
        }
    }

    public class BoneManager : MonoBehaviour
    {
        private IBaseBoneService _boneConvertorService = new NullBoneService();

        private List<RhinoxBone> _leftHandBones = new List<RhinoxBone>();
        private List<RhinoxBone> _rightHandBones = new List<RhinoxBone>();

        public bool IsInitialised { get; private set; } = false;

        private void Start()
        {
        }


        // Update is called once per frame
        void Update()
        {
            if (_boneConvertorService.GetIsInitialised() && !_boneConvertorService.GetAreBonesLoaded())
                _boneConvertorService.TryLoadSkeletons();
            else if (_boneConvertorService.GetIsInitialised() && !IsInitialised)
            {
                GetBonesFromCouplerService();
                IsInitialised = true;
                //onIsInitialised.Invoke();
            }
            else
            {
                GetBonesFromCouplerService();
            }
            //if (_boneConvertorService.GetIsInitialised() && _boneConvertorService.GetAreBonesLoaded())
        }
        
        private void GetBonesFromCouplerService(/*bool refreshBoneList = true*/)
        {
            //if (refreshBoneList)
            //{
            //    _leftHandBones.Clear();
            //    _rightHandBones.Clear();
            //}

            _leftHandBones = _boneConvertorService.GetBones(Hand.Left);
            _rightHandBones = _boneConvertorService.GetBones(Hand.Right);
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