using UnityEngine;

namespace Rhinox.XR.Grapple
{
    public class RhinoxJointCapsule
    {
        public Rigidbody JointRigidbody;
        public CapsuleCollider JointCollider;

        public RhinoxJoint StartJoint;
        public RhinoxJoint EndJoint;
    }

}