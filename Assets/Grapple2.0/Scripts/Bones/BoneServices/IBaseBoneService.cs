using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public interface IBaseBoneService
    {
        void Initialize(GameObject controllerParent);
        bool GetIsInitialised();
        bool TryLoadSkeletons();
        bool GetAreBonesLoaded();
        List<RhinoxBone> GetBones(Hand hand);
    }

    public class NullBoneService : IBaseBoneService
    {
        public void Initialize(GameObject controllerParent)
        {
        }

        public bool GetIsInitialised()
        {
            return false;
        }

        public bool TryLoadSkeletons()
        {
            return false;
        }

        public bool GetAreBonesLoaded()
        {
            return false;
        }

        public List<RhinoxBone> GetBones(Hand hand)
        {
            return new List<RhinoxBone>();
        }
    }
}
