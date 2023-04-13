using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// Contains extensions for XR Hand Joints.
    /// </summary>
    public static class XRJointsExtensions
    {
        /// <summary>
        ///  Returns a boolean value indicating if the specified XR hand joint ID is a distal joint.
        /// </summary>
        /// <param name="joint">The XR hand joint ID to check.</param>
        /// <returns>True if the specified XR hand joint ID is a distal joint, otherwise false.</returns>
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

        /// <summary>
        /// Returns a boolean value indicating if the specified XR hand joint ID is a metacarpal joint.
        /// </summary>
        /// <param name="joint">The XR hand joint ID to check.</param>
        /// <returns>True if the specified XR hand joint ID is a metacarpal joint, otherwise false.</returns>
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

        /// <summary>
        /// Returns a boolean value indicating if the specified XR hand joint ID is a proximal joint.
        /// </summary>
        /// <param name="joint">The XR hand joint ID to check.</param>
        /// <returns>True if the specified XR hand joint ID is a proximal joint, otherwise false.</returns>
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

        /// <summary>
        /// Returns a boolean value indicating if the specified XR hand joint ID is a tip joint.
        /// </summary>
        /// <param name="joint">The XR hand joint ID to check.</param>
        /// <returns>True if the specified XR hand joint ID is a tip joint, otherwise false.</returns>
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