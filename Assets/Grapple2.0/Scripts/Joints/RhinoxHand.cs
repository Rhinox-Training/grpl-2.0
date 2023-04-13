using Rhinox.Perceptor;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This enum defines the different types of hands.
    /// </summary>
    public enum RhinoxHand
    {
        /// <summary>
        /// A left hand.
        /// </summary>
        Left,
        /// <summary>
        /// A right hand.
        /// </summary>
        Right,
        /// <summary>
        /// A value to represent an invalid value.
        /// </summary>
        Invalid
    }


    public static class RhinoxHandExtensions
    {
        /// <summary>
        /// Returns the inverse hand for the given hand. If the hand is Left, returns Right; if the hand is Right, returns Left.
        /// If the input hand is Invalid, logs an error and returns Invalid.
        /// </summary>
        /// <param name="hand">The hand to get the inverse of.</param>
        /// <returns>The inverse hand.</returns>
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
                    PLog.Error<GRPLLogger>($"[RhinoxHandExtensions, GetInverse()] ,Invalid hand {hand} passed");
                    return RhinoxHand.Invalid;
            }
        }
    }
}