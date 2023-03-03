using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Rhinox.XR.Grapple
{
    [Serializable]
    public struct RhinoxGesture
    {
        public string Name;
        public List<float> JointData;
        public UnityEvent<Handedness> OnRecognized;
        public UnityEvent<Handedness> OnUnrecognized;

        /// <remarks>Does not compare the name or events!</remarks>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            
            var otherGesture = (RhinoxGesture)obj;
            if (JointData is null && otherGesture.JointData is null)
                return true;

            if (JointData is null || otherGesture.JointData is null)
                return false;
            
            if (JointData.Count == otherGesture.JointData.Count)
            {
                for (var i = 0; i < JointData.Count; i++)
                    if (!Mathf.Approximately(JointData[i],otherGesture.JointData[i]))
                        return false;
            }

            return true;
        }

        public bool Equals(RhinoxGesture other)
        {
            return Name == other.Name && Equals(JointData, other.JointData) && Equals(OnRecognized, other.OnRecognized) && Equals(OnUnrecognized, other.OnUnrecognized);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, JointData, OnRecognized, OnUnrecognized);
        }

        public static bool operator ==(RhinoxGesture gestureOne, RhinoxGesture gestureTwo)
        {
            return gestureOne.Equals(gestureTwo);
        }

        public static bool operator !=(RhinoxGesture gestureOne, RhinoxGesture gestureTwo)
        {
            return !gestureOne.Equals(gestureTwo);
        }
    }
    
    public class GestureRecognizer : MonoBehaviour
    {
        #region Gesture import fields

        public string ImportFilePath = "";
        public bool OverwriteGesturesOnImport;
        #endregion
        
        #region Gesture Saving fields
        
        public string ExportFilePath = "";
        public string ExportFileName = "RecordedGestures";
        
        public string SavedGestureName = "New Gesture";
        public Handedness HandToRecord = Handedness.Left;
        #endregion
 
        #region Recognizer fields

        public float RecognitionThreshold = 0.02f;
        public List<RhinoxGesture> Gestures = new List<RhinoxGesture>();

        private RhinoxGesture? _currentLeftGesture;
        private RhinoxGesture? _currentRightGesture;
        
        private RhinoxGesture? _lastLeftGesture;
        private RhinoxGesture? _lastRightGesture;

        public UnityEvent<Handedness, string> OnGestureRecognized;
        public UnityEvent<Handedness, string> OnGestureUnrecognized;

        #endregion
        
        private BoneManager _boneManager;

        private bool _isInitialized;

        public void Initialize(BoneManager boneManager)
        {
            _boneManager = boneManager;
            _isInitialized = true;
        }
        
        private void Awake()
        {
            ReadGesturesFromJson();
        }

        private void OnDestroy()
        {
            WriteGesturesToJson();
        }

        private void Update()
        {
            if(!_isInitialized)
                return;
            
            if(Input.GetKeyDown(KeyCode.Space))
                SaveGesture(HandToRecord);
            
            RecognizeGesture(Handedness.Left);
            RecognizeGesture(Handedness.Right);

        }

        private void SaveGesture(Handedness handedness)
        {
            if (!_isInitialized)
                return;
            
            var newGesture = new RhinoxGesture
            {
                Name = SavedGestureName
            };
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
          
             var duplicateGestures = Gestures.FindAll(x => x.Name == SavedGestureName);
             if (duplicateGestures.Count > 0)
             {
                 Debug.LogWarning(
                     $"GestureRecognizer.cs - SaveGesture({handedness}), list with {handedness} gestures already contains {duplicateGestures.Count()} gestures with the same name, adding duplicate.");
                 newGesture.Name = SavedGestureName + "_Dupe";
             
             Gestures.Add(newGesture);
                
            }
        }

        
        private void RecognizeGesture(Handedness handedness)
        {
            var currentGesture = new RhinoxGesture();
            var currentMin = Mathf.Infinity;
            var joints = _boneManager.GetBonesFromHand(handedness);
            var wristJoint = _boneManager.GetBone(XRHandJointID.Wrist, handedness);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), no wrist joint found.");
                return;
            }
            
            foreach (var gesture in Gestures)
            {
                float sumDistance = 0;
                var isDiscarded = false;
                for (var i = 0; i < joints.Count; i++)
                {
                    var currentDist = Vector3.Distance(wristJoint.BonePosition, joints[i].BonePosition);
                    var distance = currentDist - gesture.JointData[i];
                    
                    if (-RecognitionThreshold > distance || distance > RecognitionThreshold)
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
                }
            }

            switch (handedness)
            {
                case Handedness.Left:
                    _currentLeftGesture = currentGesture;
                    if (_currentLeftGesture != _lastLeftGesture)
                    {
                        _lastLeftGesture?.OnRecognized?.Invoke(handedness);
                        OnGestureUnrecognized?.Invoke(handedness, _lastLeftGesture?.Name);
                        _currentLeftGesture?.OnRecognized?.Invoke(handedness);
                        OnGestureRecognized?.Invoke(handedness,currentGesture.Name);
                    }
                    
                    _lastLeftGesture = _currentLeftGesture;
                    break;
                case Handedness.Right:
                    _currentRightGesture = currentGesture;
                    if (_currentRightGesture != _lastRightGesture)
                    {
                        _lastRightGesture?.OnUnrecognized?.Invoke(handedness);
                        OnGestureUnrecognized?.Invoke(handedness, _lastRightGesture?.Name);
                        _currentRightGesture?.OnRecognized?.Invoke(handedness);
                        OnGestureRecognized?.Invoke(handedness, currentGesture.Name);
                    }

                    _lastRightGesture = _currentRightGesture;
                    break;
            }
            
        }

        private void WriteGesturesToJson()
        {
            string finalPath;
            if (ExportFilePath.Length > 0)
            {
                finalPath = ExportFilePath;
            }
            else
            {
                finalPath = Application.dataPath;
                finalPath = finalPath.Replace("/Assets", "");
            }
            
            if (!Directory.Exists(finalPath))
            {
                var newDir =Directory.CreateDirectory(finalPath);

                if (!newDir.Exists)
                {
                    Debug.LogError(
                        $"GestureRecognizer.cs - {nameof(WriteGesturesToJson)}, could not create new directory:  \"{finalPath}\"!");
                    return;
                }
            }

            finalPath = Path.Combine(finalPath,$"{ExportFileName}.json");

            string json = JsonConvert.SerializeObject(Gestures);

            var writer = new StreamWriter(finalPath,false);
            writer.Write(json);
            writer.Close();
            Debug.Log($"Wrote gestures to {finalPath}");
        }

        private void ReadGesturesFromJson()
        {
            if(ImportFilePath.Length==0)
                return;
            
            var pathIndex = ImportFilePath.LastIndexOf('/');

            if (!Directory.Exists(ImportFilePath.Substring(0,pathIndex)))
            {
                Debug.LogError($"GestureRecognizer.cs - {nameof(ReadGesturesFromJson)}, could not find directory:  \"{ImportFilePath}\"!");
            }
            
            var reader = new StreamReader(ImportFilePath);
            var fileContent = reader.ReadToEnd();
            var json = JsonConvert.DeserializeObject<List<RhinoxGesture>>(fileContent);

            if (!OverwriteGesturesOnImport)
            {
                foreach (var gesture in json)
                    Gestures.Add(gesture);
            }
            else
                Gestures = json;



        }
        
    }
}
