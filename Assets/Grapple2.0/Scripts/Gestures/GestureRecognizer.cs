using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.InputSystem;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This struct represents a gesture in grapple. <br />
    /// It contains all data related to one gesture, like the name, joint distances and joint forward.
    /// </summary>
    [Serializable]
    public struct RhinoxGesture
    {
        [JsonProperty(PropertyName = "Gesture name")]
        public string Name;

        [JsonProperty(PropertyName = "Joint distances")]
        public List<float> JointData;

        [JsonProperty (PropertyName = "Uses joint forward")]
        public bool UseJointForward;

        [JsonProperty(PropertyName = "Forward joint")]
        public XRHandJointID CheckJoint;
        
        [JsonProperty (PropertyName = "Wrist rotation")]
        public Vector3 JointForward;
        
        [JsonIgnore]
        public UnityEvent<Handedness> OnRecognized;

        [JsonIgnore]
        public UnityEvent<Handedness> OnUnrecognized;

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
    
    /// <summary>
    /// This class implements the behaviour to detect gestures. These gestures can be imported from a json or recording during play mode.
    /// There is also the possibility to export the gestures in a (new) json file.
    /// </summary>
    public class GestureRecognizer : MonoBehaviour
    {
        #region Gesture import fields

        public bool ImportOnPlay = true;
        
        public string ImportFilePath = "";
        public bool OverwriteGesturesOnImport;
        #endregion
        
        #region Gesture Saving fields
        public bool ExportOnDestroy = false;
        public string ExportFilePath = "";
        public string ExportFileName = "RecordedGestures";

        public InputActionReference RecordActionReference;
        public string SavedGestureName = "New Gesture";
        public Handedness HandToRecord = Handedness.Left;
        public bool UseJointForward = false;
        public XRHandJointID ForwardJoint;
        #endregion
 
        #region Recognizer fields

        public float GestureDistanceThreshold = 0.02f;
        public float GestureForwardThreshold = 0.5f;
        public List<RhinoxGesture> Gestures = new List<RhinoxGesture>();

        private RhinoxGesture? _currentLeftGesture;
        private RhinoxGesture? _currentRightGesture;
        
        private RhinoxGesture? _lastLeftGesture;
        private RhinoxGesture? _lastRightGesture;

        public UnityEvent<Handedness, string> OnGestureRecognized;
        public UnityEvent<Handedness, string> OnGestureUnrecognized;

        #endregion
        
        private JointManager _jointManager;

        private bool _isInitialized;

        /// <summary>
        /// As the bone manager is an integral part of gesture recognition, this should always be called when creating this component! 
        /// </summary>
        /// <param name="jointManager"></param>
        public void Initialize(JointManager jointManager)
        {
            _jointManager = jointManager;
            _isInitialized = true;
        }
        
        private void Awake()
        {
            if(ImportOnPlay)
                ReadGesturesFromJson();
        }

        private void OnDestroy()
        {
            if(ExportOnDestroy)
                WriteGesturesToJson();
        }

        private void OnEnable()
        {
            if(RecordActionReference == null)
                Debug.LogWarning("GestureRecognizer.cs, Record action reference not set!");
            
            UnityTypeExtensions.Subscribe(RecordActionReference,SaveGesture);
        }

        private void OnDisable()
        {
            UnityTypeExtensions.Unsubscribe(RecordActionReference, SaveGesture);
        }

        private void Update()
        {
            if(!_isInitialized)
                return;

            //Check for gesture recognition
            RecognizeGesture(Handedness.Left);
            RecognizeGesture(Handedness.Right);
    
        }
        
        /// <summary>
        /// Saves the current pose of the "HandToRecord" hand as a new gesture under the name "SavedGestureName". <br />
        /// This also includes the option to record the forward of the joint "ForwardJoint", for more restricted recognition. 
        /// </summary>
        /// <param name="ctx">See <see cref="InputAction.CallbackContext"/></param>
        private void SaveGesture(InputAction.CallbackContext ctx)
        {
            if (!_isInitialized)
                return;
            
            var newGesture = new RhinoxGesture
            {
                Name = SavedGestureName,
                UseJointForward = UseJointForward
            };
            var gestureDistances = new List<float>();
            var joints = _jointManager.GetJointsFromHand(HandToRecord);
            
            //Get the root (wrist joint)
            var wristJoint = _jointManager.GetJoint(XRHandJointID.Wrist, HandToRecord);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({HandToRecord}), no wrist joint found.");
                return;
            }

            // Saving the joint forward allows for more detailed recognition
            // Possible applications could be making a distinction between a thumbs up and a thumbs down
            // Remark: is very sensitive
            if (UseJointForward)
            {
                newGesture.CheckJoint = ForwardJoint;
                var joint = _jointManager.GetJoint(ForwardJoint, HandToRecord);

                if (joint == null)
                {
                    Debug.LogError($"Can't find joint {ForwardJoint} on {HandToRecord} hand");
                    return;
                }
                newGesture.JointForward = joint.Forward;
            }
            
            //Save the distances from each joint to the root
            foreach (var joint in joints)
            {
                var currentDist = Vector3.Distance(joint.JointPosition, wristJoint.JointPosition);
                gestureDistances.Add(currentDist);
            }

            newGesture.JointData = gestureDistances;
          
            //Handle duplicate gesture names (at least a bit)
             var duplicateGestures = Gestures.FindAll(x => x.Name == SavedGestureName);
             if (duplicateGestures.Count > 0)
             {
                 Debug.LogWarning(
                     $"GestureRecognizer.cs - SaveGesture({HandToRecord}), list with {HandToRecord} gestures already contains {duplicateGestures.Count} gestures with the same name, adding duplicate.");
                 newGesture.Name = SavedGestureName + "_Dupe";
             }

             Gestures.Add(newGesture);
        }
        
        /// <summary>
        /// Checks if the given hand "handedness" is currently representing a gesture. <br /> If a gesture is recognized, it is set as the current gesture and the corresponding events are invoked.
        /// <remarks>Use the "RecognitionDistanceThreshold" and "RecognitionForwardThreshold" to change the harshness of the recognition. </remarks>
        /// </summary>
        /// <param name="handedness"></param>
        private void RecognizeGesture(Handedness handedness)
        {
            var currentGesture = new RhinoxGesture();
            var currentMin = Mathf.Infinity;
            var joints = _jointManager.GetJointsFromHand(handedness);
            var wristJoint = _jointManager.GetJoint(XRHandJointID.Wrist, handedness);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), no wrist joint found.");
                return;
            }
            
            foreach (var gesture in Gestures)
            {
                float sumDistance = 0;
                var isDiscarded = false;

                if (gesture.UseJointForward)
                {
                    //Get the correct forward of the current hand
                    var bone = _jointManager.GetJoint(gesture.CheckJoint, handedness);
                    if(bone == null)
                        continue;
                    
                    if (!UnityTypeExtensions.Approximately(bone.Forward, gesture.JointForward,GestureForwardThreshold))
                        continue;
                }
                
                for (var i = 0; i < joints.Count; i++)
                {
                    var currentDist = Vector3.Distance(wristJoint.JointPosition, joints[i].JointPosition);
                    var distance = currentDist - gesture.JointData[i];
                    
                    if (-GestureDistanceThreshold > distance || distance > GestureDistanceThreshold)
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

        /// <summary>
        /// Writes all current gestures to a .json file at directory "ExportFilePath" with name "ExportFileName".json.
        /// <remarks>If the ExportFilePath directory is not valid, the application data path is used.</remarks>
        /// </summary>
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
            //Debug.Log($"Wrote gestures to {finalPath}");
        }

        /// <summary>
        /// Reds the gestures from the json file at "ImportFilePath".
        /// <remarks>See <see cref="ReadGesturesFromJson(string)"/></remarks>
        /// </summary>
        private void ReadGesturesFromJson()
        {
            ReadGesturesFromJson(ImportFilePath);
        }

        /// <summary>
        /// Imports the gestures from the given json file at path "path".
        /// <remarks>If the directory or file is not valid, an empty list is added.</remarks>
        /// <remarks>Specify whether to overwrite the current gesture using "OverwriteGesturesOnImport"</remarks>
        /// </summary>
        /// <param name="path"></param>
        public void ReadGesturesFromJson(string path)
        {
            if (path.Length == 0)
                return;

            var pathIndex = path.LastIndexOf('/');

            if (!Directory.Exists(path.Substring(0, pathIndex)))
            {
                Debug.LogError(
                    $"GestureRecognizer.cs - {nameof(ReadGesturesFromJson)}, could not find directory:  \"{path}\"!");
            }

            if (!File.Exists(path))
            {
                Gestures ??= new List<RhinoxGesture>();
                Debug.Log($"ReadGesturesFromJson(),File with path {path} could not be found, returning.");
                return;
            }

            var reader = new StreamReader(path);
            var fileContent = reader.ReadToEnd();
            var json = JsonConvert.DeserializeObject<List<RhinoxGesture>>(fileContent);

            if (!OverwriteGesturesOnImport)
            {
                foreach (var gesture in json)
                    Gestures.Add(gesture);
            }
            else
                Gestures = json;

            Gestures ??= new List<RhinoxGesture>();
        }

    }
}
