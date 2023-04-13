using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// Represents a joint in a Grapple application.
    /// </summary>
    public class RhinoxJoint
    {
        /// <summary>
        /// The ID of the XRHandJoint that this RhinoxJoint represents.
        /// </summary>
        public readonly XRHandJointID JointID;

        /// <summary>
        /// The world-space position of this joint.
        /// </summary>
        public Vector3 JointPosition = Vector3.zero;

        /// <summary>
        /// The rotation of this joint.
        /// </summary>
        public Quaternion JointRotation = Quaternion.identity;

        /// <summary>
        /// The forward direction of this joint.
        /// </summary>
        public Vector3 Forward;

        /// <summary>
        /// The radius of this joint.
        /// </summary>
        public float JointRadius;

        /// <summary>
        /// Creates a new RhinoxJoint instance for the specified XRHandJointID.
        /// </summary>
        /// <param name="jointID">The ID of the XRHandJoint that this RhinoxJoint represents.</param>
        public RhinoxJoint(XRHandJointID jointID)
        {
            JointID = jointID;
        }
    }
}