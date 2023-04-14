using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine.InputSystem;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This class implements the behaviour to detect gestures. These gestures can be imported from a json or recording during play mode.
    /// There is also the possibility to export the gestures in a (new) json file.
    /// </summary>
    /// <dependencies> <see cref="GRPLJointManager"/> </dependencies>
    public class GRPLGestureRecognizer : MonoBehaviour
    {
        /// <summary>
        /// A boolean flag to indicate if imported gestures should overwrite any existing gestures or not.
        /// </summary>
        public bool OverwriteGesturesOnImport;

        /// <summary>
        /// A boolean flag to indicate if gestures should be imported when the play mode starts.
        /// </summary>
        public bool ImportOnPlay = false;

        /// <summary>
        /// A string that represents the path to the file containing gestures.
        /// </summary>
        public string ImportFilePath = "";

        /// <summary>
        /// A boolean flag to indicate if gestures should be exported when the component is destroyed.
        /// </summary>
        public bool ExportOnDestroy = false;

        /// <summary>
        /// A string that represents the path where the exported gestures should be saved.
        /// </summary>
        public string ExportFilePath = "";

        /// <summary>
        /// A string that represents the name of the exported gestures file.
        /// </summary>
        public string ExportFileName = "RecordedGestures";

        /// <summary>
        /// An InputActionReference object that represents the input action that should be used to record new gestures.
        /// </summary>
        public InputActionReference RecordActionReference;

        /// <summary>
        /// A string that represents the name of the next gesture that is recorded.
        /// </summary>
        public string SavedGestureName = "New Gesture";

        /// <summary>
        /// An enum value that represents the hand that should be used to record new gestures.
        /// </summary>
        public RhinoxHand HandToRecord = RhinoxHand.Left;

        /// <summary>
        /// A boolean flag that indicates whether the forward vector of a joint should be used when trying to recognize the next recorded gesture.
        /// </summary>
        public bool UseJointForward = false;

        /// <summary>
        /// An enum value that represents the joint that should be used as the forward vector.
        /// </summary>
        public XRHandJointID ForwardJoint;

        /// <summary>
        /// A float value that represents the bend threshold used to compare the bend values of gestures to the current hand.
        /// </summary>
        public float GestureBendThreshold = 0.2f;

        /// <summary>
        /// A float value that represents the angle threshold used to compare the direction of a gesture with the forward vector of a joint.
        /// </summary>
        public float GestureForwardThreshold = 0.5f;

        /// <summary>
        /// A list of RhinoxGesture objects that represents the gestures that can be recognized by the component.
        /// </summary>
        public List<RhinoxGesture> Gestures = new List<RhinoxGesture>();

        public static event Action<GRPLGestureRecognizer> GlobalInitialized;

        /// <summary>
        /// A Unity event that is invoked when any gesture is recognized.
        /// </summary>
        public UnityEvent<RhinoxHand, string> OnGestureRecognized;

        /// <summary>
        /// A Unity event that is invoked when any gesture is unrecognized.
        /// </summary>
        public UnityEvent<RhinoxHand, string> OnGestureUnrecognized;

        /// <summary>
        /// A RhinoxGesture object that represents the current gesture of the left hand.
        /// </summary>
        public RhinoxGesture CurrentLeftGesture => _currentLeftGesture;

        private RhinoxGesture _currentLeftGesture;

        /// <summary>
        /// A RhinoxGesture object that represents the current gesture of the right hand.
        /// </summary>
        public RhinoxGesture CurrentRightGesture => _currentRightGesture;

        private RhinoxGesture _currentRightGesture;

        /// <summary>
        /// A boolean flag that indicates whether a gesture was recognized for the first time this frame on the left hand.
        /// </summary>
        public bool LeftHandGestureRecognizedThisFrame { get; private set; }

        /// <summary>
        /// A boolean flag that indicates whether a gesture was recognized for the first time this frame on the right hand.
        /// </summary>
        public bool RightHandGestureRecognizedThisFrame { get; private set; }

        /// <summary>
        /// A RhinoxGesture object that represents the gesture on the left hand in the previous frame.
        /// </summary>
        private RhinoxGesture _lastLeftGesture;

        /// <summary>
        /// A RhinoxGesture object that represents the gesture on the right hand in the previous frame.
        /// </summary>
        private RhinoxGesture _lastRightGesture;

        /// <summary>
        /// A GRPLJointManager object that is used to track the hand joints.
        /// </summary>
        private GRPLJointManager _jointManager;

        /// <summary>
        /// A boolean flag that indicates whether the component has been initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// As the bone manager is an integral part of gesture recognition, this should always be called when creating this component! 
        /// </summary>
        /// <param name="jointManager"></param>
        private void Initialize(GRPLJointManager jointManager)
        {
            if (_isInitialized)
                return;

            _jointManager = jointManager;
            _jointManager.TrackingLost += OnTrackingLost;
            _isInitialized = true;

            GlobalInitialized?.Invoke(this);
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
                Debug.LogError($"{nameof(GRPLGestureRecognizer)} - {nameof(OnTrackingLost)}, " +
                               $"function called with unsupported rhinoxHand value : {rhinoxHand}");
                return;
            }

            if (rhinoxHand == RhinoxHand.Left)
                InvokeGestureLostEvents(ref _currentLeftGesture, ref _lastLeftGesture, rhinoxHand);
            else
                InvokeGestureLostEvents(ref _currentRightGesture, ref _lastRightGesture, rhinoxHand);
        }

        private void InvokeGestureLostEvents(ref RhinoxGesture currentGesture, ref RhinoxGesture lastGesture,
            RhinoxHand rhinoxHand)
        {
            if (currentGesture != null)
            {
                currentGesture.InvokeOnRecognized(rhinoxHand);
                OnGestureUnrecognized.Invoke(rhinoxHand, currentGesture.Name);
                lastGesture = null;
                currentGesture = null;
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            if (ExportOnDestroy)
                WriteGesturesToJson();
        }

        private void OnEnable()
        {
            if (RecordActionReference == null)
                Debug.LogWarning("GRPLGestureRecognizer.cs, Record action reference not set!");

            UnityTypeExtensions.SubscribeAndActivateAsset(RecordActionReference, SaveGesture);
        }

        private void OnDisable()
        {
            UnityTypeExtensions.Unsubscribe(RecordActionReference, SaveGesture);
        }
#endif

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

                for (int i = 0; i < gesture.FingerBendValues.Count; i++)
                {
                    if (!_jointManager.TryGetFingerBend(handedness, (RhinoxFinger)i, out var currentBendVal))
                        break;

                    float distance = currentBendVal - gesture.FingerBendValues[i];

                    if (!distance.IsBetweenIncl(-gesture.BendThreshold, gesture.BendThreshold))
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
                HandleRecognizedGesture(currentGesture, ref _currentLeftGesture, ref _lastLeftGesture, handedness);
            else
                HandleRecognizedGesture(currentGesture, ref _currentRightGesture, ref _lastRightGesture, handedness);
        }

        private void HandleRecognizedGesture(RhinoxGesture newGesture, ref RhinoxGesture currentGesture,
            ref RhinoxGesture lastGesture, RhinoxHand handedness)
        {
            lastGesture = currentGesture;
            currentGesture = newGesture;

            if (currentGesture != lastGesture)
            {
                if (handedness == RhinoxHand.Left)
                    LeftHandGestureRecognizedThisFrame = true;
                else
                    RightHandGestureRecognizedThisFrame = true;
                lastGesture?.InvokeOnUnRecognized(handedness);
                OnGestureUnrecognized?.Invoke(handedness, lastGesture?.Name);
                currentGesture?.InvokeOnRecognized(handedness);
                if (currentGesture != null) OnGestureRecognized?.Invoke(handedness, currentGesture.Name);
            }
            else
            {
                if (handedness == RhinoxHand.Left)
                    LeftHandGestureRecognizedThisFrame = false;
                else
                    RightHandGestureRecognizedThisFrame = false;
            }
        }

        /// <summary>
        /// Returns the current gesture on the given hand if the hand is either left or right. 
        /// Returns null if the given hand is invalid.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>The gesture on hand.</returns>
        public RhinoxGesture GetCurrentGestureOfHand(RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    return _currentLeftGesture;
                case RhinoxHand.Right:
                    return _currentRightGesture;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLLogger>(
                        $"GRPLGestureRecognizer.cs - GetCurrentGestureOfHand({hand}), " +
                        $"function called with invalid hand: {hand}.");
                    return null;
            }
        }

        /// <summary>
        /// Returns whether the current gesture on the given hand was recognized for the first time this frame. 
        /// Returns false if the given hand is invalid.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>Whether the current gesture on the given hand was recognized for the first time this frame.</returns>
        public bool WasRecognizedGestureStartedThisFrame(RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                    return LeftHandGestureRecognizedThisFrame;
                case RhinoxHand.Right:
                    return RightHandGestureRecognizedThisFrame;
                case RhinoxHand.Invalid:
                default:
                    PLog.Error<GRPLLogger>($"GRPLGestureRecognizer.cs - GetCurrentGestureOfHand({hand}), " +
                                              $"function called with invalid hand: {hand}.");
                    return false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Saves the current pose of the "RhinoxHandTo " rhinoxHand as a new gesture under the name "SavedGestureName". <br />
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
                BendThreshold = GestureBendThreshold,
                RotationThreshold = GestureForwardThreshold
            };

            // Saving the joint forward allows for more detailed recognition
            // Possible applications could be making a distinction between a thumbs up and a thumbs down
            // Remark: is very sensitive
            if (UseJointForward)
            {
                newGesture.CheckJoint = ForwardJoint;
                _jointManager.TryGetJointFromHandById(ForwardJoint, HandToRecord, out var joint);

                if (joint == null)
                {
                    Debug.LogError($"Can't find joint {ForwardJoint} on {HandToRecord} rhinoxHand");
                    return;
                }

                newGesture.JointForward = joint.Forward;
            }

            // Save the individual finger bends
            foreach (RhinoxFinger finger in Enum.GetValues(typeof(RhinoxFinger)))
            {
                if (!_jointManager.TryGetFingerBend(HandToRecord, finger, out float bendValue))
                {
                    PLog.Error<GRPLLogger>("[GRPLGestureRecognizer:SaveGesture({RhinoxHandToRecord})], " +
                                              $"Failed to get bend value for finger {finger} on {HandToRecord} rhinoxHand");
                    return;
                }

                newGesture.FingerBendValues.Add(bendValue);
            }

            //Handle duplicate gesture names (at least a bit)
            var duplicateGestures = Gestures.FindAll(x => x.Name == SavedGestureName);
            if (duplicateGestures.Count > 0)
            {
                Debug.LogWarning(
                    $"GRPLGestureRecognizer.cs - SaveGesture({HandToRecord}), list with {HandToRecord} gestures already contains {duplicateGestures.Count} gestures with the same name, adding duplicate.");
                newGesture.Name = SavedGestureName + "_Dupe";
            }

            Gestures.Add(newGesture);
        }

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
                        $"GRPLGestureRecognizer.cs - {nameof(WriteGesturesToJson)}, could not create new directory:  \"{finalPath}\"!");
                    return;
                }
            }

            finalPath = Path.Combine(finalPath, $"{ExportFileName}.json");

            string json = JsonConvert.SerializeObject(Gestures);

            var writer = new StreamWriter(finalPath, false);
            writer.Write(json);
            writer.Close();
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
                    $"GRPLGestureRecognizer.cs - {nameof(ReadGesturesFromJson)}, could not find directory:  \"{path}\"!");
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