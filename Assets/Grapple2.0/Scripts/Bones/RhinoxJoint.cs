using UnityEngine;
using UnityEngine.XR.Hands;


namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// 
    /// </summary>
    public class RhinoxJoint
    {
        public readonly XRHandJointID JointID;

        /// <summary>
        /// JointPosition is in WorldSpace
        /// </summary>
        public Vector3 JointPosition = Vector3.zero;

        /// <summary>
        /// JointPosition is in WorldSpace
        /// </summary>
        public Quaternion JointRotation = Quaternion.identity;

        public Vector3 Forward;
        public float JointRadius;

        public RhinoxJoint(XRHandJointID jointID)
        {
            JointID = jointID;
        }
    }
}