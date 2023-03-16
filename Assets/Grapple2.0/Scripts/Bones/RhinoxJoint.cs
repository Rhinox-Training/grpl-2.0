using UnityEngine;
using UnityEngine.XR.Hands;


namespace Rhinox.XR.Grapple
{
    public class RhinoxJoint
    {
        public readonly XRHandJointID JointID;

        public Vector3 JointPosition = Vector3.zero;
        public Quaternion JointRotation = Quaternion.identity;

        public Vector3 Forward;
        public float JointRadius;

        public RhinoxJoint(XRHandJointID jointID)
        {
            JointID = jointID;
        }
    }
}