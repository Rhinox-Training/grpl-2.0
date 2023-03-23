using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
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

        [JsonIgnore][SerializeField] private UnityEvent<RhinoxHand> OnRecognized;

        [JsonIgnore][SerializeField] private UnityEvent<RhinoxHand> OnUnrecognized;

        [JsonIgnore]
        private static RhinoxGesture _noGesture;

        [JsonIgnore]
        private const int AMOUNTOFJOINTS = 26;

        #region Wrapping Unity events for external code
        public void AddListenerOnRecognized(UnityAction<RhinoxHand> action)
        {
            if (OnRecognized == null)
                OnRecognized = new UnityEvent<RhinoxHand>();

            OnRecognized.AddListener(action);
        }

        public void RemoveListenerOnRecognized(UnityAction<RhinoxHand> action)
        {
            OnRecognized?.RemoveListener(action);
        }

        public void RemoveAllListenersOnRecognized()
        {
            OnRecognized?.RemoveAllListeners();
        }

        public void InvokeOnRecognized(RhinoxHand hand)
        {
            OnRecognized?.Invoke(hand);
        }


        public void AddListenerOnUnRecognized(UnityAction<RhinoxHand> action)
        {
            if (OnUnrecognized == null)
                OnUnrecognized = new UnityEvent<RhinoxHand>();

            OnUnrecognized.AddListener(action);
        }

        public void RemoveListenerOnUnRecognized(UnityAction<RhinoxHand> action)
        {
            OnUnrecognized?.RemoveListener(action);
        }

        public void RemoveAllListenersOnUnRecognized()
        {
            OnUnrecognized?.RemoveAllListeners();
        }

        public void InvokeOnUnRecognized(RhinoxHand hand)
        {
            OnUnrecognized?.Invoke(hand);
        }
        #endregion

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
            if (Equals(other, null))
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

        public static RhinoxGesture NoGesture()
        {
            if (_noGesture == null)
            {
                _noGesture = new RhinoxGesture
                {
                    Name = "No Gesture",
                    JointData = new List<float>(AMOUNTOFJOINTS)
                };
            }
            return _noGesture;
        }
    }
}