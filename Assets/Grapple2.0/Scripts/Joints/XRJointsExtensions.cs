using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    public static class XRJointsExtensions
    {
        public static bool IsMetacarpal(this XRHandJointID joint)
        {
            switch (joint)
            {
                case XRHandJointID.ThumbMetacarpal:
                case XRHandJointID.IndexMetacarpal:
                case XRHandJointID.MiddleMetacarpal:
                case XRHandJointID.RingMetacarpal:
                case XRHandJointID.LittleMetacarpal:
                    return true;
                default:
                    return false;
            }
        }
    }
}