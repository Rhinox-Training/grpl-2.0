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
        
        [JsonIgnore]
        public UnityEvent<Hand> OnRecognized;

        [JsonIgnore]
        public UnityEvent<Hand> OnUnrecognized;

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
        public Hand HandToRecord = Hand.Left;
        public bool UseJointForward = false;
        public XRHandJointID ForwardJoint;

        public float GestureDistanceThreshold = 0.02f;
        public float GestureForwardThreshold = 0.5f;
        #endregion

        #region Recognizer fields
        public List<RhinoxGesture> Gestures = new List<RhinoxGesture>();

        private RhinoxGesture? _currentLeftGesture;
        private RhinoxGesture? _currentRightGesture;

        private RhinoxGesture? _lastLeftGesture;
        private RhinoxGesture? _lastRightGesture;

        public UnityEvent<Hand, string> OnGestureRecognized;
        public UnityEvent<Hand, string> OnGestureUnrecognized;

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
            _jointManager.TrackingLost += OnTrackingLost;
            _isInitialized = true;
        }

        private void Awake()
        {
            #if UNITY_EDITOR
            if (ImportOnPlay)
                ReadGesturesFromJson();   
            #endif
            
        }

        private void OnTrackingLost(Hand hand)
        {
            switch (hand)
            {
                case Hand.Left:
                    if (_currentLeftGesture != null)
                    {
                        _currentLeftGesture.Value.OnUnrecognized.Invoke(hand);
                        OnGestureUnrecognized.Invoke(hand,_currentLeftGesture.Value.Name);
                        _lastLeftGesture = _currentLeftGesture;
                        _currentLeftGesture = null;
                    }
                    break;
                case Hand.Right:
                    if (_currentRightGesture != null)
                    {
                        _currentRightGesture.Value.OnUnrecognized.Invoke(hand);
                        OnGestureUnrecognized.Invoke(hand, _currentRightGesture.Value.Name);
                        _lastRightGesture = _currentRightGesture;
                        _currentRightGesture = null;
                    }
                    break;
                default:
                    Debug.LogError($"{nameof(GestureRecognizer)} - {nameof(OnTrackingLost)}, function called with unsupported hand value : {hand}");
                    break;
            }
        }
        
        private void OnDestroy()
        {
            #if UNITY_EDITOR
            if (ExportOnDestroy)
                WriteGesturesToJson();
            #endif
        }

        private void OnEnable()
        {
            #if UNITY_EDITOR

            if (RecordActionReference == null)
                Debug.LogWarning("GestureRecognizer.cs, Record action reference not set!");

            UnityTypeExtensions.Subscribe(RecordActionReference, SaveGesture);
            #endif
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            UnityTypeExtensions.Unsubscribe(RecordActionReference, SaveGesture);
            #endif
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            //Check for gesture recognition
            if(_jointManager.IsLeftHandTracked)
                RecognizeGesture(Hand.Left);
            if(_jointManager.IsRightHandTracked)
                RecognizeGesture(Hand.Right);

        }
        
        #if UNITY_EDITOR
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
                UseJointForward = UseJointForward,
                DistanceThreshold = GestureDistanceThreshold,
                RotationThreshold = GestureForwardThreshold
            };
            var gestureDistances = new List<float>();
            _jointManager.TryGetJointsFromHand(HandToRecord, out var joints);

            //Get the root (wrist joint)
            _jointManager.TryGetJointFromHandById(XRHandJointID.Wrist, HandToRecord, out var wristJoint);
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
                _jointManager.TryGetJointFromHandById(ForwardJoint, HandToRecord, out var joint);

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
        #endif
        
        /// <summary>
        /// Checks if the given hand "handedness" is currently representing a gesture. <br /> If a gesture is recognized, it is set as the current gesture and the corresponding events are invoked.
        /// <remarks>Use the "RecognitionDistanceThreshold" and "RecognitionForwardThreshold" to change the harshness of the recognition. </remarks>
        /// </summary>
        /// <param name="handedness"></param>
        private void RecognizeGesture(Hand handedness)
        {
            RhinoxGesture? currentGesture = null;
            var currentMin = Mathf.Infinity;
            _jointManager.TryGetJointsFromHand(handedness, out var joints);
            _jointManager.TryGetJointFromHandById(XRHandJointID.Wrist, handedness, out var wristJoint);
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
                    if (!_jointManager.TryGetJointFromHandById(gesture.CheckJoint, handedness, out var bone))
                        continue;

                    if (!UnityTypeExtensions.Approximately(bone.Forward, gesture.JointForward, gesture.RotationThreshold))
                        continue;
                }

                for (var i = 0; i < joints.Count; i++)
                {
                    var currentDist = Vector3.Distance(wristJoint.JointPosition, joints[i].JointPosition);
                    var distance = currentDist - gesture.JointData[i];

                    if (-GestureDistanceThreshold > distance || distance > gesture.DistanceThreshold)
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
                case Hand.Left:
                    _currentLeftGesture = currentGesture;
                    if (_currentLeftGesture != _lastLeftGesture)
                    {
                        _lastLeftGesture?.OnRecognized?.Invoke(handedness);
                        OnGestureUnrecognized?.Invoke(handedness, _lastLeftGesture?.Name);
                        _currentLeftGesture?.OnRecognized?.Invoke(handedness);
                        if (currentGesture != null) OnGestureRecognized?.Invoke(handedness, currentGesture.Value.Name);
                    }

                    _lastLeftGesture = _currentLeftGesture;
                    break;
                case Hand.Right:
                    _currentRightGesture = currentGesture;
                    if (_currentRightGesture != _lastRightGesture)
                    {
                        _lastRightGesture?.OnUnrecognized?.Invoke(handedness);
                        OnGestureUnrecognized?.Invoke(handedness, _lastRightGesture?.Name);
                        _currentRightGesture?.OnRecognized?.Invoke(handedness);
                        if (currentGesture != null) OnGestureRecognized?.Invoke(handedness, currentGesture.Value.Name);
                    }

                    _lastRightGesture = _currentRightGesture;
                    break;
            }

        }

        #if UNITY_EDITOR
        /// <summary>
        /// Writes all current gestures to a .json file at directory "ExportFilePath" with name "ExportFileName".json.
        /// <remarks>If the ExportFilePath directory is not valid, the application data path is used.</remarks>
        /// </summary>
        public void WriteGesturesToJson()
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
                var newDir = Directory.CreateDirectory(finalPath);

                if (!newDir.Exists)
                {
                    Debug.LogError(
                        $"GestureRecognizer.cs - {nameof(WriteGesturesToJson)}, could not create new directory:  \"{finalPath}\"!");
                    return;
                }
            }

            finalPath = Path.Combine(finalPath, $"{ExportFileName}.json");

            string json = JsonConvert.SerializeObject(Gestures);

            var writer = new StreamWriter(finalPath, false);
            writer.Write(json);
            writer.Close();
        }
        #endif

        #if UNITY_EDITOR
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
            reader.Close();
        }
        #endif
    }
}
