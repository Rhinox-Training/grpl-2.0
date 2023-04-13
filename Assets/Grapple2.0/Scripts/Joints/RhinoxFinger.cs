using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// Represents the fingers on a hand, including the thumb and four fingers.
    /// </summary>
    public enum RhinoxFinger
    {
        /// <summary>
        /// The thumb finger.
        /// </summary>
        Thumb = 0,

        /// <summary>
        /// The index finger.
        /// </summary>
        Index = 1,

        /// <summary>
        /// The middle finger.
        /// </summary>
        Middle = 2,

        /// <summary>
        /// The ring finger.
        /// </summary>
        Ring = 3,

        /// <summary>
        /// The little finger (also known as the pinky finger).
        /// </summary>
        Little = 4
    }

    public static class RhinoxFingerExtensions
    {
        /// <summary>
        /// This extension method retrieves all the XRHandJointIds that correspond to the joints of a specified finger.
        /// </summary>
        /// <param name="finger">The finger for which the joint ids are being retrieved.</param>
        /// <returns>A collection of XRHandJointID values that correspond to the joints of the specified finger.</returns>
        /// <remarks>If an invalid value is passed for the RhinoxFinger parameter, an empty collection is returned.</remarks>
        public static ICollection<XRHandJointID> GetJointIdsFromFinger(this RhinoxFinger finger)
        {
            var returnValue = new Collection<XRHandJointID>();
            switch (finger)
            {
                case RhinoxFinger.Thumb:
                    returnValue.Add(XRHandJointID.ThumbMetacarpal);
                    returnValue.Add(XRHandJointID.ThumbProximal);
                    returnValue.Add(XRHandJointID.ThumbDistal);
                    returnValue.Add(XRHandJointID.ThumbTip);
                    break;
                case RhinoxFinger.Index:
                    returnValue.Add(XRHandJointID.IndexMetacarpal);
                    returnValue.Add(XRHandJointID.IndexProximal);
                    returnValue.Add(XRHandJointID.IndexIntermediate);
                    returnValue.Add(XRHandJointID.IndexDistal);
                    returnValue.Add(XRHandJointID.IndexTip);
                    break;
                case RhinoxFinger.Middle:
                    returnValue.Add(XRHandJointID.MiddleMetacarpal);
                    returnValue.Add(XRHandJointID.MiddleProximal);
                    returnValue.Add(XRHandJointID.MiddleIntermediate);
                    returnValue.Add(XRHandJointID.MiddleDistal);
                    returnValue.Add(XRHandJointID.MiddleTip);
                    break;
                case RhinoxFinger.Ring:
                    returnValue.Add(XRHandJointID.RingMetacarpal);
                    returnValue.Add(XRHandJointID.RingProximal);
                    returnValue.Add(XRHandJointID.RingIntermediate);
                    returnValue.Add(XRHandJointID.RingDistal);
                    returnValue.Add(XRHandJointID.RingTip);
                    break;
                case RhinoxFinger.Little:
                    returnValue.Add(XRHandJointID.LittleMetacarpal);
                    returnValue.Add(XRHandJointID.LittleProximal);
                    returnValue.Add(XRHandJointID.LittleIntermediate);
                    returnValue.Add(XRHandJointID.LittleDistal);
                    returnValue.Add(XRHandJointID.LittleTip);
                    break;
                default:
                    Debug.LogError(
                        $"GetJointIdsFromFinger, function called with unsupported RhinoxFinger value: {finger}");
                    return Array.Empty<XRHandJointID>();
            }

            return returnValue;
        }
    }
}

