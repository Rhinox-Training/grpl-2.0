using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This struct represents a gesture in grapple. <br />
    /// It contains all data related to one gesture, like the name, joint distances and joint forward.
    /// </summary>
    [Serializable]
    public class RhinoxGesture
    {
        [JsonProperty(PropertyName = "Gesture name")]
        public string Name;

        [JsonProperty(PropertyName = "Joint distances")]
        public List<float> JointData;

        [JsonProperty(PropertyName = "Uses joint forward")]
        public bool UseJointForward;

        [JsonProperty(PropertyName = "Forward joint")]
        public XRHandJointID CheckJoint;

        [JsonProperty(PropertyName = "Wrist rotation")]
        public Vector3 JointForward;

        [JsonProperty(PropertyName = "Distance threshold")]
        public float DistanceThreshold;

        [JsonProperty(PropertyName = "Rotation threshold")]
        public float RotationThreshold;

        [JsonIgnore] public UnityEvent<RhinoxHand> OnRecognized;

        [JsonIgnore] public UnityEvent<RhinoxHand> OnUnrecognized;

        /// <remarks>Does not compare the name or events!</remarks>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof(RhinoxGesture))
                return false;

            var otherGesture = (RhinoxGesture)obj;
            if (JointData is null && otherGesture.JointData is null)
                return true;

            if (JointData is null || otherGesture.JointData is null)
                return false;

            if (JointData.Count == otherGesture.JointData.Count)
            {
                for (var i = 0; i < JointData.Count; i++)
                    if (!Mathf.Approximately(JointData[i], otherGesture.JointData[i]))
                        return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            hashCode.Add(JointData);
            hashCode.Add(UseJointForward);
            hashCode.Add((int)CheckJoint);
            hashCode.Add(JointForward);
            hashCode.Add(DistanceThreshold);
            hashCode.Add(RotationThreshold);
            hashCode.Add(OnRecognized);
            hashCode.Add(OnUnrecognized);
            return hashCode.ToHashCode();
        }

        public bool Equals(RhinoxGesture other)
        {
            if(Equals(other,null))
                return false;
            
            return Name == other.Name && Equals(JointData, other.JointData) &&
                   Equals(OnRecognized, other.OnRecognized) && Equals(OnUnrecognized, other.OnUnrecognized);
        }

        public static bool operator ==(RhinoxGesture gestureOne, RhinoxGesture gestureTwo)
        {
            if (ReferenceEquals(gestureOne, gestureTwo))
                return true;
            if (ReferenceEquals(gestureOne, null))
                return false;
            if (ReferenceEquals(gestureTwo, null))
                return false;
            return gestureOne.Equals(gestureTwo);
        }

        public static bool operator !=(RhinoxGesture obj1, RhinoxGesture obj2) => !(obj1 == obj2);
    }
}