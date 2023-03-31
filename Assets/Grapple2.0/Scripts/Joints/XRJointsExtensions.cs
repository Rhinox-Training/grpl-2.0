using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    public static class XRJointsExtensions
    {
        public static bool IsDistal(this XRHandJointID joint)
        {
            switch (joint)
            {
                case XRHandJointID.ThumbDistal:
                case XRHandJointID.IndexDistal:
                case XRHandJointID.MiddleDistal:
                case XRHandJointID.RingDistal:
                case XRHandJointID.LittleDistal:
                    return true;
                default:
                    return false;
            }
        }

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

        public static bool IsProximal(this XRHandJointID joint)
        {
            switch (joint)
            {
                case XRHandJointID.ThumbProximal:
                case XRHandJointID.IndexProximal:
                case XRHandJointID.MiddleProximal:
                case XRHandJointID.RingProximal:
                case XRHandJointID.LittleProximal:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTip(this XRHandJointID joint)
        {
            switch (joint)
            {
                case XRHandJointID.ThumbTip:
                case XRHandJointID.IndexTip:
                case XRHandJointID.MiddleTip:
                case XRHandJointID.RingTip:
                case XRHandJointID.LittleTip:
                    return true;
                default:
                    return false;
            }
        }
    }
}