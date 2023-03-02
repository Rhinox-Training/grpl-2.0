using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System.Linq;

namespace Rhinox.XR.Grapple
{
    [Serializable]
    public struct RhinoxGesture
    {
        public string Name;
        public List<float> JointData;
        public UnityEvent OnRecognized;
    }
    
    public class GestureRecognizer : MonoBehaviour
    {
        #region Gesture Saving fields
        public string SavedGestureName = "New Gesture";
        
        #endregion

        #region Recognizer fields

        private List<RhinoxGesture> _leftHandGestures = new List<RhinoxGesture>();
        private List<RhinoxGesture> _rightHandGestures = new List<RhinoxGesture>();

        private RhinoxGesture? _previousLeftGesture = null;
        private RhinoxGesture? _previousRightGesture = null;

        public float Threshold = 0.05f;

        #endregion
        
        private BoneManager _boneManager;

        private bool _isInitialized = false;

        public void Initialize(BoneManager boneManager)
        {
            _boneManager = boneManager;
            _isInitialized = true;
        }
        
        private void Update()
        {
            if(!_isInitialized)
                return;

            if(Input.GetKeyDown(KeyCode.Space))
                SaveGesture(Handedness.Right);

            var gesture = RecognizeGesture(Handedness.Right);
            if (gesture != null)
                Debug.Log($"Gesture {gesture.Value.Name} recognized");

        }

        private void SaveGesture(Handedness handedness)
        {
            var newGesture = new RhinoxGesture();
            newGesture.Name = SavedGestureName;
            var gestureDistances = new List<float>();
            var joints = _boneManager.GetBonesFromHand(handedness);
            var wristJoint = _boneManager.GetBone(XRHandJointID.Wrist, handedness);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), no wrist joint found.");
                return;
            }
            
            foreach (var joint in joints)
            {
                //Save the vector from the wrist to the current joint
                var currentDist = Vector3.Distance(joint.BonePosition, wristJoint.BonePosition);
                gestureDistances.Add(currentDist);
            }

            newGesture.JointData = gestureDistances;
            switch (handedness)
            {
                case Handedness.Invalid:
                    Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), given handedness ({handedness}) is invalid, can't save gesture.");
                    break;
                case Handedness.Left:
                {
                    var duplicateGestures = _leftHandGestures.FindAll(x => x.Name == SavedGestureName);
                    if (duplicateGestures.Count > 0)
                    {
                        Debug.LogWarning(
                            $"GestureRecognizer.cs - SaveGesture({handedness}), list with {handedness} gestures already contains {duplicateGestures.Count()} gestures with the same name, adding duplicate.");
                        newGesture.Name = SavedGestureName + duplicateGestures.Count;
                    }

                    _leftHandGestures.Add(newGesture);
                    break;
                }
                case Handedness.Right:
                {
                    
                    var duplicateGestures = _leftHandGestures.FindAll(x => x.Name == SavedGestureName);
                    if (duplicateGestures.Count > 0)
                    {
                        Debug.LogWarning(
                            $"GestureRecognizer.cs - SaveGesture({handedness}), list with {handedness} gestures already contains {duplicateGestures.Count()} gestures with the same name, adding duplicate.");
                        newGesture.Name = SavedGestureName + duplicateGestures.Count;
                    }
                    _rightHandGestures.Add(newGesture);
                    break;
                }
            }
        }

        private RhinoxGesture? RecognizeGesture(Handedness handedness)
        {
            var currentGesture = new RhinoxGesture();
            var currentMin = Mathf.Infinity;
            var joints = _boneManager.GetBonesFromHand(handedness);
            var gestureFound = false;
            List<RhinoxGesture> gestures;
            var wristJoint = _boneManager.GetBone(XRHandJointID.Wrist, handedness);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), no wrist joint found.");
                return null;
            }
            
            //Get the correct gesture collection
            switch (handedness)
            {
                case Handedness.Invalid:
                    return null;
                case Handedness.Left:
                    gestures = _leftHandGestures;
                    break;
                case Handedness.Right:
                    gestures = _rightHandGestures;
                    break;
                default:
                    return null;
            }

            foreach (var gesture in gestures)
            {
                float sumDistance = 0;
                var isDiscarded = false;
                for (var i = 0; i < joints.Count; i++)
                {
                    var currentDist = Vector3.Distance(wristJoint.BonePosition, joints[i].BonePosition);
                    var distance = currentDist - gesture.JointData[i];
                    
                    if (-Threshold > distance || distance > Threshold)
                    {
                        isDiscarded = true;
                        break;
                    }

                    sumDistance += distance;
                }

                if (!isDiscarded && sumDistance < currentMin)
                {
                    currentMin = sumDistance;
                    currentGesture = gesture;
                    gestureFound = true;
                }
            }

            if (gestureFound)
                return currentGesture;
            else
                return null;
        }

    }
}
