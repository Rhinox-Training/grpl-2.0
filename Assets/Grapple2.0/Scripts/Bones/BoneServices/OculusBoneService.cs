using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public class OculusBoneService : IBaseBoneService
    {
        private OVRSkeleton _skeletonRefLeftHand = null;
        private OVRSkeleton _skeletonRefRightHand = null;

        private GameObject _controllerParent = null;

        private bool _isInitialised;
        private bool _areBonesLoaded = false;

        public void Initialize(GameObject controllerParent)
        {
            if (_isInitialised)
                return;

            if (controllerParent == null)
            {
                Debug.LogError($"{typeof(OculusBoneService).Namespace + "." + nameof(OculusBoneService)}.Initialize(): Controller parent cannot be null");
                return;
            }
            _controllerParent = controllerParent;

            _isInitialised = true;
        }

        public bool GetIsInitialised()
        {
            return _isInitialised;
        }
        public bool GetAreBonesLoaded()
        {
            return _areBonesLoaded;
        }

        public bool TryLoadBones()
        {
            _areBonesLoaded = false;

            var skeletons = _controllerParent.GetComponentsInChildren<OVRSkeleton>();
            foreach (var skeleton in skeletons)
            {
                if (!skeleton.IsInitialized)
                    return false;
            }
            foreach (var skeleton in skeletons)
            {
                var type = skeleton.GetSkeletonType();
                switch (type)
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                        _skeletonRefLeftHand = skeleton;
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                        _skeletonRefRightHand = skeleton;
                        break;
                    default:
                        Debug.LogError($"{typeof(OculusBoneService).Namespace + "." + nameof(OculusBoneService)}.TryLoadBones() : Cannot determine hand type");
                        return false;
                }
            }
            Debug.Log($"{typeof(OculusBoneService).Namespace + "." + nameof(OculusBoneService)}.TryLoadBones() : OVRSkeletons loaded in");

            _areBonesLoaded = true;
            return true;
        }


        public List<RhinoxBone> GetBones(Hand hand)
        {
            List<RhinoxBone> retVal = new List<RhinoxBone>();
            switch (hand)
            {
                case Hand.Left:
                    for (int i = 0; i < _skeletonRefLeftHand.Bones.Count; i++)
                    {
                        retVal.Add(new RhinoxBone(
                            _skeletonRefLeftHand.Bones[i].Id.ToString(),
                            _skeletonRefLeftHand.Bones[i].Transform,
                            FindCorrespondingColliderCapsules(_skeletonRefLeftHand, i)));
                    }
                    break;
                case Hand.Right:
                    for (int i = 0; i < _skeletonRefRightHand.Bones.Count; i++)
                    {
                        retVal.Add(new RhinoxBone(
                            _skeletonRefRightHand.Bones[i].Id.ToString(),
                            _skeletonRefRightHand.Bones[i].Transform,
                            FindCorrespondingColliderCapsules(_skeletonRefRightHand, i)));
                    }
                    break;
                case Hand.Both:
                    for (int i = 0; i < _skeletonRefLeftHand.Bones.Count; i++)
                    {
                        retVal.Add(new RhinoxBone(
                            _skeletonRefLeftHand.Bones[i].Id.ToString(),
                            _skeletonRefLeftHand.Bones[i].Transform,
                            FindCorrespondingColliderCapsules(_skeletonRefLeftHand, i)));
                    }
                    for (int i = 0; i < _skeletonRefRightHand.Bones.Count; i++)
                    {
                        retVal.Add(new RhinoxBone(
                            _skeletonRefRightHand.Bones[i].Id.ToString(),
                            _skeletonRefRightHand.Bones[i].Transform,
                            FindCorrespondingColliderCapsules(_skeletonRefRightHand, i)));
                    }
                    break;
            }
            return retVal;
        }

        public List<OVRBone> GetOculusBones(Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    return new List<OVRBone>(_skeletonRefLeftHand.Bones);
                case Hand.Right:
                    return new List<OVRBone>(_skeletonRefRightHand.Bones);
                case Hand.Both:
                    return new List<OVRBone>((_skeletonRefLeftHand.Bones).Concat(_skeletonRefRightHand.Bones));
            }
            return null;
        }

        public OVRSkeleton GetOculusSkeleton(Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    return _skeletonRefLeftHand;
                case Hand.Right:
                    return _skeletonRefRightHand;
                case Hand.Both:
                    return null;
            }
            return null;
        }

        private List<CapsuleCollider> FindCorrespondingColliderCapsules(OVRSkeleton skeleton, int i)
        {
            List<CapsuleCollider> retVal = new List<CapsuleCollider>();

            foreach (var capsule in skeleton.Capsules)
            {
                if (capsule.BoneIndex == i)
                {
                    retVal.Add(capsule.CapsuleCollider);
                }
            }
            return retVal;
        }
    }
}