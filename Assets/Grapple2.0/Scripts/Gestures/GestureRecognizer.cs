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
    /// This class implements the behaviour to detect gestures. These gestures can be imported from a json or recording during play mode.
    /// There is also the possibility to export the gestures in a (new) json file.
    /// </summary>
    public class GestureRecognizer : MonoBehaviour
    {
        #region Gesture import fields
        public bool ImportOnPlay = false;
        public string ImportFilePath = "";
        public bool OverwriteGesturesOnImport;
        #endregion

        #region Gesture Saving fields

        public bool ExportOnDestroy = false;
        public string ExportFilePath = "";
        public string ExportFileName = "RecordedGestures";

        public InputActionReference RecordActionReference;
        public string SavedGestureName = "New Gesture";
        public RhinoxHand RhinoxHandToRecord = RhinoxHand.Left;
        public bool UseJointForward = false;
        public XRHandJointID ForwardJoint;

        public float GestureDistanceThreshold = 0.02f;
        public float GestureForwardThreshold = 0.5f;

        #endregion

        #region Recognizer fields
        public List<RhinoxGesture> Gestures = new List<RhinoxGesture>();

        private RhinoxGesture _currentLeftGesture;
        private RhinoxGesture _currentRightGesture;

        private RhinoxGesture _lastLeftGesture;
        private RhinoxGesture _lastRightGesture;

        public UnityEvent<RhinoxHand, string> OnGestureRecognized;
        public UnityEvent<RhinoxHand, string> OnGestureUnrecognized;
        #endregion

        private GRPLJointManager _jointManager;

        private bool _isInitialized;

        /// <summary>
        /// As the bone manager is an integral part of gesture recognition, this should always be called when creating this component! 
        /// </summary>
        /// <param name="jointManager"></param>
        private void Initialize(GRPLJointManager jointManager)
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
            //Subscribe to the global initialized event of the joint manager
            GRPLJointManager.GlobalInitialized += Initialize;
        }

        private void OnTrackingLost(RhinoxHand rhinoxHand)
        {
            if (rhinoxHand == RhinoxHand.Invalid)
            {
                Debug.LogError($"{nameof(GestureRecognizer)} - {nameof(OnTrackingLost)}, " +
                               $"function called with unsupported rhinoxHand value : {rhinoxHand}");
                return;
            }
            
            if (rhinoxHand == RhinoxHand.Left)
                InvokeGestureLostEvents(ref _currentLeftGesture, ref _lastLeftGesture, rhinoxHand);
            else
                InvokeGestureLostEvents(ref _currentRightGesture, ref _lastRightGesture, rhinoxHand);
        }

        private void InvokeGestureLostEvents(ref RhinoxGesture currentGesture,ref RhinoxGesture lastGesture, RhinoxHand rhinoxHand)
        {
            if (currentGesture != null)
            {
                currentGesture.InvokeOnRecognized(rhinoxHand);// .OnUnrecognized.Invoke(rhinoxHand);
                OnGestureUnrecognized.Invoke(rhinoxHand, currentGesture.Name);
                lastGesture = null;
                currentGesture = null;
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

            UnityTypeExtensions.SubscribeAndActivateAsset(RecordActionReference, SaveGesture);
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
            if (_jointManager.IsLeftHandTracked)
                RecognizeGesture(RhinoxHand.Left);
            if (_jointManager.IsRightHandTracked)
                RecognizeGesture(RhinoxHand.Right);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Saves the current pose of the "RhinoxHandToRecord" rhinoxHand as a new gesture under the name "SavedGestureName". <br />
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
            if (!_jointManager.TryGetJointsFromHand(RhinoxHandToRecord, out var joints))
                return;

            //Get the root (wrist joint)
            _jointManager.TryGetJointFromHandById(XRHandJointID.Wrist, RhinoxHandToRecord, out var wristJoint);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({RhinoxHandToRecord}), no wrist joint found.");
                return;
            }

            // Saving the joint forward allows for more detailed recognition
            // Possible applications could be making a distinction between a thumbs up and a thumbs down
            // Remark: is very sensitive
            if (UseJointForward)
            {
                newGesture.CheckJoint = ForwardJoint;
                _jointManager.TryGetJointFromHandById(ForwardJoint, RhinoxHandToRecord, out var joint);

                if (joint == null)
                {
                    Debug.LogError($"Can't find joint {ForwardJoint} on {RhinoxHandToRecord} rhinoxHand");
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
                    $"GestureRecognizer.cs - SaveGesture({RhinoxHandToRecord}), list with {RhinoxHandToRecord} gestures already contains {duplicateGestures.Count} gestures with the same name, adding duplicate.");
                newGesture.Name = SavedGestureName + "_Dupe";
            }

            Gestures.Add(newGesture);
        }
#endif

        /// <summary>
        /// Checks if the given rhinoxHand "handedness" is currently representing a gesture. <br /> If a gesture is recognized, it is set as the current gesture and the corresponding events are invoked.
        /// <remarks>Use the "RecognitionDistanceThreshold" and "RecognitionForwardThreshold" to change the harshness of the recognition. </remarks>
        /// </summary>
        /// <param name="handedness"></param>
        private void RecognizeGesture(RhinoxHand handedness)
        {
            if (handedness == RhinoxHand.Invalid)
            {
                Debug.LogWarning("Handedness is invalid, returning");
                return;
            }

            RhinoxGesture currentGesture = null;
            float currentMin = float.MaxValue;
            if (!_jointManager.TryGetJointsFromHand(handedness, out var joints))
                return;

            _jointManager.TryGetJointFromHandById(XRHandJointID.Wrist, handedness, out var wristJoint);
            if (wristJoint == null)
            {
                Debug.LogError($"GestureRecognizer.cs - SaveGesture({handedness}), no wrist joint found.");
                return;
            }

            foreach (var gesture in Gestures)
            {
                float sumDistance = 0;
                bool isDiscarded = false;

                if (gesture.UseJointForward)
                {
                    //Get the correct forward of the current rhinoxHand
                    if (!_jointManager.TryGetJointFromHandById(gesture.CheckJoint, handedness, out var bone))
                        continue;

                    if (!bone.Forward.Approximately(gesture.JointForward, gesture.RotationThreshold))
                        continue;
                }

                for (var i = 0; i < joints.Count; i++)
                {
                    float currentDist = Vector3.Distance(wristJoint.JointPosition, joints[i].JointPosition);
                    float distance = currentDist - gesture.JointData[i];

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

            if (handedness == RhinoxHand.Left)
               HandleRecognizedGesture(currentGesture, ref _currentLeftGesture, ref _lastLeftGesture,handedness);
            else
                HandleRecognizedGesture(currentGesture, ref _currentRightGesture, ref _lastRightGesture,handedness);
        }

        private void HandleRecognizedGesture(RhinoxGesture newGesture, ref RhinoxGesture currentGesture,
            ref RhinoxGesture lastGesture, RhinoxHand handedness)
        {
            lastGesture = currentGesture;
            currentGesture = newGesture;
            
            if (currentGesture != lastGesture)
            {
                lastGesture?.InvokeOnUnRecognized(handedness);// .OnUnrecognized?.Invoke(handedness);
                OnGestureUnrecognized?.Invoke(handedness, lastGesture?.Name);
                currentGesture?.InvokeOnRecognized(handedness);// .OnRecognized?.Invoke(handedness);
                if (currentGesture != null) OnGestureRecognized?.Invoke(handedness, currentGesture.Name);
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