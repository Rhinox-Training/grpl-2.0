namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// The possible states a Grapple Interactible can be in.
    /// </summary>
    public enum GRPLInteractionState
    {
        /// <summary>
        /// The neutral state of a grapple interactible. This means all checks can happen for the object.
        /// </summary>
        Active,
        /// <summary>
        /// This state is used when a grapple interactible is in proximity to hands.
        /// </summary>
        Proximate,
        /// <summary>
        /// This state is used when a hand is currently interacting with the grapple interactible.
        /// </summary>
        Interacted,
        /// <summary>
        /// This state is used when a grapple interactible is disabled and no proximity or interactions checks should happen.
        /// </summary>
        Disabled
    }
}