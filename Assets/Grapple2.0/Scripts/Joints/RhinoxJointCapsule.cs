using UnityEngine;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// A class representing a joint capsule in Rhinox XR Grapple, with a rigidbody and a capsule collider.
    /// </summary>
    public class RhinoxJointCapsule
    {
        /// <summary>
        /// The rigidbody component of the joint capsule.
        /// </summary>
        public Rigidbody JointRigidbody;
        /// <summary>
        /// The capsule collider component of the joint capsule.
        /// </summary>
        public CapsuleCollider JointCollider;

        /// <summary>
        /// The starting joint connected to this joint capsule.
        /// </summary>
        public RhinoxJoint StartJoint;
        /// <summary>
        /// The ending joint connected to this joint capsule.
        /// </summary>
        public RhinoxJoint EndJoint;
    }
}