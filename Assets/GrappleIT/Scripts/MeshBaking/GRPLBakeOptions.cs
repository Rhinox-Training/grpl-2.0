namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This enum is used to control mesh baking behaviour.
    /// </summary>
    public enum GRPLBakeOptions
    {
        /// <summary>
        /// Indicates that no baking of meshes will occur.
        /// </summary>
        NoBake,

        /// <summary>
        /// Indicates that standard baking of meshes will occur.
        /// </summary>
        StandardBake,

        /// <summary>
        /// Indicates that baking of meshes will occur, and the result will be parented to the object.
        /// </summary>
        BakeAndParent
    }
}