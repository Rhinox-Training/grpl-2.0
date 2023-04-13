using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// The RhinoxGesture class represents a gesture in the Rhinox XR Grapple module.
    /// It contains data related to a gesture, such as the name, finger bend values, joint forward,
    /// distance and rotation thresholds, and events that are invoked when the gesture is recognized or unrecognized.
    /// </summary>
    [Serializable]
    public class RhinoxGesture
    {
        /// <summary>
        ///  A string that represents the name of the gesture.
        /// </summary>
        [JsonProperty(PropertyName = "Gesture name")]
        public string Name;

        /// <summary>
        /// A list of floats that represents the finger bend values of the gesture.<br />
        /// The values are stored in the following order: Thumb, Index, Middle, Ring, Little.
        /// </summary>
        [JsonProperty(PropertyName = "Finger bend values")]
        public List<float> FingerBendValues = new List<float>();

        /// <summary>
        /// A bool that indicates whether the gesture uses joint forward.
        /// </summary>
        [JsonProperty(PropertyName = "Uses joint forward")]
        public bool UseJointForward;

        /// <summary>
        /// An XRHandJointID that represents the forward joint of the gesture.
        /// </summary>
        [JsonProperty(PropertyName = "Forward joint")]
        public XRHandJointID CheckJoint;

        /// <summary>
        /// A Vector3 that represents the wrist rotation of the gesture.
        /// </summary>
        [JsonProperty(PropertyName = "Wrist rotation")]
        public Vector3 JointForward;

        /// <summary>
        /// A float that represents the bend threshold of the gesture.
        /// </summary>
        [JsonProperty(PropertyName = "Distance threshold")]
        public float BendThreshold;

        /// <summary>
        /// A float that represents the rotation threshold of the gesture.
        /// </summary>
        [JsonProperty(PropertyName = "Rotation threshold")]
        public float RotationThreshold;

        /// <summary>
        /// A UnityEvent that is invoked when the gesture is recognized. It has a parameter of type RhinoxHand.
        /// </summary>
        [JsonIgnore] [SerializeField] private UnityEvent<RhinoxHand> _onRecognized;

        /// <summary>
        /// A UnityEvent that is invoked when the gesture is unrecognized. It has a parameter of type RhinoxHand.
        /// </summary>
        [JsonIgnore] [SerializeField] private UnityEvent<RhinoxHand> _onUnrecognized;

        [JsonIgnore] private static RhinoxGesture _noGesture;

        //================
        //Recognized events
        //================
        /// <summary>
        /// Adds a listener to the OnRecognized event.
        /// </summary>
        /// <param name="action"></param>
        public void AddListenerOnRecognized(UnityAction<RhinoxHand> action)
        {
            if (_onRecognized == null)
                _onRecognized = new UnityEvent<RhinoxHand>();

            _onRecognized.AddListener(action);
        }

        /// <summary>
        /// Removes a listener from the OnRecognized event.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveListenerOnRecognized(UnityAction<RhinoxHand> action)
        {
            _onRecognized?.RemoveListener(action);
        }

        /// <summary>
        /// Removes all listeners from the OnRecognized event.
        /// </summary>
        public void RemoveAllListenersOnRecognized()
        {
            _onRecognized?.RemoveAllListeners();
        }

        /// <summary>
        /// Invokes the OnRecognized event with the provided RhinoxHand parameter.
        /// </summary>
        /// <param name="hand"></param>
        public void InvokeOnRecognized(RhinoxHand hand)
        {
            _onRecognized?.Invoke(hand);
        }

        //==================
        //Unrecognized events
        //==================
        /// <summary>
        /// Adds a listener to the OnUnrecognized event.
        /// </summary>
        /// <param name="action"></param>
        public void AddListenerOnUnRecognized(UnityAction<RhinoxHand> action)
        {
            if (_onUnrecognized == null)
                _onUnrecognized = new UnityEvent<RhinoxHand>();

            _onUnrecognized.AddListener(action);
        }

        /// <summary>
        /// Removes a listener from the OnUnrecognized event.
        /// </summary>
        /// <param name="action"></param>
        public void RemoveListenerOnUnRecognized(UnityAction<RhinoxHand> action)
        {
            _onUnrecognized?.RemoveListener(action);
        }

        /// <summary>
        /// Removes all listeners from the OnUnrecognized event.
        /// </summary>
        public void RemoveAllListenersOnUnRecognized()
        {
            _onUnrecognized?.RemoveAllListeners();
        }

        /// <summary>
        /// Invokes the OnUnrecognized event with the provided RhinoxHand parameter.
        /// </summary>
        /// <param name="hand"></param>
        public void InvokeOnUnRecognized(RhinoxHand hand)
        {
            _onUnrecognized?.Invoke(hand);
        }

        /// <remarks>Does not compare the name or events!</remarks>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof(RhinoxGesture))
                return false;

            var otherGesture = (RhinoxGesture)obj;
            if (FingerBendValues is null && otherGesture.FingerBendValues is null)
                return true;

            if (FingerBendValues is null || otherGesture.FingerBendValues is null)
                return false;

            if (FingerBendValues.Count == otherGesture.FingerBendValues.Count)
            {
                for (var i = 0; i < FingerBendValues.Count; i++)
                    if (!Mathf.Approximately(FingerBendValues[i], otherGesture.FingerBendValues[i]))
                        return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            hashCode.Add(FingerBendValues);
            hashCode.Add(UseJointForward);
            hashCode.Add((int)CheckJoint);
            hashCode.Add(JointForward);
            hashCode.Add(BendThreshold);
            hashCode.Add(RotationThreshold);
            hashCode.Add(_onRecognized);
            hashCode.Add(_onUnrecognized);
            return hashCode.ToHashCode();
        }

        public bool Equals(RhinoxGesture other)
        {
            if (Equals(other, null))
                return false;

            return Name == other.Name && Equals(FingerBendValues, other.FingerBendValues) &&
                   Equals(_onRecognized, other._onRecognized) && Equals(_onUnrecognized, other._onUnrecognized);
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

        /// <summary>
        /// Returns an instance of RhinoxGesture with default values, representing the absence of a gesture.
        /// </summary>
        /// <returns>The empty instance.</returns>
        public static RhinoxGesture NoGesture()
        {
            if (_noGesture == null)
            {
                _noGesture = new RhinoxGesture
                {
                    Name = "No Gesture",
                    FingerBendValues = new List<float>(5)
                };
            }

            return _noGesture;
        }
    }
}