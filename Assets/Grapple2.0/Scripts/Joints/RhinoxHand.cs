using System;
using Rhinox.Perceptor;

namespace Rhinox.XR.Grapple
{
    public enum RhinoxHand
    {
        Left,
        Right,
        Invalid
    }


    public static class RhinoxHandExtensions
    {
        public static RhinoxHand GetInverse(this RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    return RhinoxHand.Right;
                case RhinoxHand.Right:
                    return RhinoxHand.Left;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GrappleLogger>($"[RhinoxHandExtensions, GetInverse()] ,Invalid hand {hand} passed");
                    return RhinoxHand.Invalid;
            }
        }
    }
}