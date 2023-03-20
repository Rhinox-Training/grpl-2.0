using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rhinox.GUIUtils.Attributes;

namespace Rhinox.XR.Grapple
{
    public class JointManager : MonoBehaviour
    {
        private XRHandSubsystem _subsystem;

        private List<RhinoxJoint> _leftHandJoints = new List<RhinoxJoint>();
        private List<RhinoxJoint> _rightHandJoints = new List<RhinoxJoint>();

        private RhinoxJointCapsule[] _leftHandCapsules;
        private RhinoxJointCapsule[] _rightHandCapsules;

        private GameObject _leftHandCollidersParent;
        private GameObject _rightHandCollidersParent;
        public bool AreJointsInitialised { get; private set; } = false;

        public bool HandTrackingProviderContainsCapsules = false;

        public float ColliderActivationDelay = 1.5f;

        // As it is impossible for the joints to fully touch each other, this value represents a total bend
        public float BendDistMin = 0.3f;

        // As it is almost impossible for a finger to be fully stretched, this value represents a total stretch
        public float StretchDistMin = 0.9f;

        public bool JointCollisionsEnabled
        {
            get => _jointCollisionsEnabled;
            set
            {
                _jointCollisionsEnabled = value;
                SetHandCollisions(_jointCollisionsEnabled, RhinoxHand.Left);
                SetHandCollisions(_jointCollisionsEnabled, RhinoxHand.Right);
            }
        }

        private bool _jointCollisionsEnabled = true;

        [Layer] public int HandLayer = -1;

        public bool IsLeftHandTracked { get; private set; } = false;
        public bool IsRightHandTracked { get; private set; } = false;

        public event Action<RhinoxHand> TrackingAcquired;
        public event Action<RhinoxHand> TrackingLost;
        public event Action<RhinoxHand> OnHandsUpdated;

        public event Action Initialized;
        public static event Action<JointManager> GlobalInitialized;

        private bool _fixedUpdateAfterTrackingLeftFound = false;
        private bool _fixedUpdateAfterTrackingRightFound = false;
        private bool _firstHandUpdateAfterTrackingFound = false;

        //-----------------------------
        // Finger bend fields
        //-----------------------------
        [Header("Bend Thresholds")] [SerializeField]
        public float ThumbBendThreshold = 0.75f;

        [SerializeField] public float IndexBendThreshold = 0.3f;
        [SerializeField] public float MiddleBendThreshold = 0.25f;
        [SerializeField] public float RingBendThreshold = 0.3f;
        [SerializeField] public float LittleBendThreshold = 0.3f;

        // Finger full stretch values
        private float _thumbFullStretchVal = 0;
        private float _indexFullStretchVal = 0;
        private float _middleFullStretchVal = 0;
        private float _ringFullStretchVal = 0;
        private float _littleFullStretchVal = 0;

        #region Initialization Methods

        private void Awake()
        {
            InitializeHandJoints();
        }

        private void Start()
        {
            // Disable collision between the hands and between the joints
            Physics.IgnoreLayerCollision(HandLayer, HandLayer);
        }

        private void OnEnable()
        {
            if (_subsystem == null)
                return;

            _subsystem.updatedHands += OnUpdatedHands;
            _subsystem.trackingAcquired += OnTrackingAcquired;
            _subsystem.trackingLost += OnTrackingLost;
        }

        private void OnDisable()
        {
            if (_subsystem == null)
                return;

            _subsystem.updatedHands -= OnUpdatedHands;
            _subsystem.trackingAcquired -= OnTrackingAcquired;
            _subsystem.trackingLost -= OnTrackingLost;
        }


        private void TryEnsureInitialized()
        {
            var generalSettings = XRGeneralSettings.Instance;
            if (generalSettings == null)
            {
                Debug.Log("Instance of XRGeneralSettings could not be acquired.");
                return;
            }

            var manager = generalSettings.Manager;
            if (manager == null)
            {
                Debug.Log("Instance of XrManagerSettings could not be acquired.");
                return;
            }

            var activeLoader = manager.activeLoader;
            if (activeLoader == null)
            {
                Debug.Log("Instance of XrLoader could not be acquired.");
            }

            //Load the subsystem if possible
            _subsystem = activeLoader.GetLoadedSubsystem<XRHandSubsystem>();

            if (_subsystem == null)
                return;

            // SubscribeAndActivateAsset the corresponding events
            _subsystem.updatedHands += OnUpdatedHands;
            _subsystem.trackingAcquired += OnTrackingAcquired;
            _subsystem.trackingLost += OnTrackingLost;

            Initialized?.Invoke();
            GlobalInitialized?.Invoke(this);
        }

        private void InitializeHandJoints()
        {
            if (AreJointsInitialised)
                return;

            //Initialize wrist
            {
                var leftWristJoint = new RhinoxJoint(XRHandJointID.Wrist);
                _leftHandJoints.Add(leftWristJoint);

                var rightWristJoint = new RhinoxJoint(XRHandJointID.Wrist);
                _rightHandJoints.Add(rightWristJoint);
            }

            //Initialize finger joints
            foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
            {
                if (jointId is XRHandJointID.Invalid or XRHandJointID.Wrist or XRHandJointID.BeginMarker
                    or XRHandJointID.EndMarker)
                    continue;

                var leftJoint = new RhinoxJoint(jointId);
                _leftHandJoints.Add(leftJoint);
                var rightJoint = new RhinoxJoint(jointId);
                _rightHandJoints.Add(rightJoint);
            }

            AreJointsInitialised = true;
        }

        /// <summary>
        /// This function collects the correct variables for the given handedness and calls <see cref="InitializeCapsulesForHand"/>.
        /// </summary>
        /// <param name="handedness"></param>
        private void InitializeJointCapsules(RhinoxHand handedness)
        {
            if (HandTrackingProviderContainsCapsules)
            {
                Debug.LogError("RhinoxHand tracking provider has it's own capsules, don't create them manually");
                return;
            }

            // Check the parent object. If it doesn't exist, create it.
            // Parent is only null when the capsules are initialized for the first time
            switch (handedness)
            {
                case RhinoxHand.Left:
                {
                    if (_leftHandCollidersParent)
                        break;
                    _leftHandCollidersParent = new GameObject($"{handedness}_Capsules");
                    Transform parentTransform = _leftHandCollidersParent.transform;
                    parentTransform.SetParent(transform, false);
                    parentTransform.localPosition = Vector3.zero;
                    parentTransform.localRotation = Quaternion.identity;
                    break;
                }
                case RhinoxHand.Right:
                {
                    if (_rightHandCollidersParent)
                        break;
                    _rightHandCollidersParent = new GameObject($"{handedness}_Capsules");
                    Transform parentTransform = _rightHandCollidersParent.transform;
                    parentTransform.SetParent(transform, false);
                    parentTransform.localPosition = Vector3.zero;
                    parentTransform.localRotation = Quaternion.identity;
                    break;
                }
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(InitializeJointCapsules)}" +
                        $", function called with incorrect rhinoxHand {handedness}. Only left or right supported!");
                    return;
            }

            // Get the correct parent, joints and capsule for the given handedness
            RhinoxJointCapsule[] capsules;
            GameObject parent;
            List<RhinoxJoint> joints;
            if (handedness == RhinoxHand.Left)
            {
                if (_leftHandCapsules == null || _leftHandCapsules.Length != _leftHandJoints.Count)
                    _leftHandCapsules = new RhinoxJointCapsule[_leftHandJoints.Count];
                capsules = _leftHandCapsules;
                parent = _leftHandCollidersParent;
                joints = _leftHandJoints;
            }
            else
            {
                if (_rightHandCapsules == null || _rightHandCapsules.Length != _rightHandJoints.Count)
                    _rightHandCapsules = new RhinoxJointCapsule[_rightHandJoints.Count];
                capsules = _rightHandCapsules;
                parent = _rightHandCollidersParent;
                joints = _rightHandJoints;
            }

            InitializeCapsulesForHand(handedness, parent, capsules, joints);
        }

        /// <summary>
        /// This function creates/ updates the capsules in a hand when tracking is acquired. 
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="parent"></param>
        /// <param name="capsules"></param>
        /// <param name="joints"></param>
        private void InitializeCapsulesForHand(RhinoxHand hand, GameObject parent, RhinoxJointCapsule[] capsules,
            List<RhinoxJoint> joints)
        {
            //Loop over the capsules and process them
            for (var index = capsules.Length - 1; index > 0; index--)
            {
                // Get the current joint and get/create its corresponding capsule
                // The current joint is the start joint of its corresponding capsule
                if (!TryGetJointFromHandById((XRHandJointID)index + 1, hand, out var joint))
                    return;
                var jointCapsule = capsules[index] ?? (capsules[index] = new RhinoxJointCapsule());
                jointCapsule.StartJoint = joint;

                // Create the current capsules rigidbody if it does not exist yet
                if (jointCapsule.JointRigidbody == null)
                {
                    jointCapsule.JointRigidbody =
                        new GameObject(joint.JointID.ToString() + "_Rigidbody").AddComponent<Rigidbody>();
                    jointCapsule.JointRigidbody.mass = 1.0f;
                    jointCapsule.JointRigidbody.isKinematic = true;
                    jointCapsule.JointRigidbody.useGravity = false;
                    jointCapsule.JointRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }

                // Get the game object of the rigidbody and the component
                // Disable the rigidbody to prevent scene explosions
                var rbGo = jointCapsule.JointRigidbody.gameObject;
                var rbTransform = rbGo.transform;
                rbGo.layer = HandLayer;
                rbTransform.SetParent(parent.transform, false);
                rbTransform.position = joint.JointPosition;
                rbTransform.rotation = joint.JointRotation;

                // Create the current capsules collider if it does not exist yet
                if (jointCapsule.JointCollider == null)
                {
                    jointCapsule.JointCollider = new GameObject(joint.JointID.ToString() + "_CapsuleCollider")
                        .AddComponent<CapsuleCollider>();
                    jointCapsule.JointCollider.isTrigger = false;
                }

                // Get the transform of the connected joints
                var p0 = joints[index].JointPosition;
                var endJoint = !joint.JointID.IsMetacarpal() ? joints[index - 1] : joints[0];
                var p1 = endJoint.JointPosition;

                // Calculate the orientation and length of the capsule collider
                var delta = p1 - p0;
                var colliderLength = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);

                // Set all the collider data
                jointCapsule.JointCollider.enabled = JointCollisionsEnabled;
                jointCapsule.JointCollider.radius = 0.01f;
                jointCapsule.JointCollider.height = colliderLength;
                jointCapsule.JointCollider.direction = 0;
                jointCapsule.JointCollider.center = Vector3.right * (colliderLength * 0.5f);

                // Set the correct physics info of the collider Game Object
                var colliderGo = jointCapsule.JointCollider.gameObject;
                colliderGo.layer = HandLayer;
                colliderGo.transform.SetParent(rbGo.transform, false);
                colliderGo.transform.rotation = rot;

                // Set the end joint of the capsule
                jointCapsule.EndJoint = endJoint;
            }
        }


        private void InitializeFingerBendFields(RhinoxHand hand)
        {
            foreach (RhinoxFinger finger in Enum.GetValues(typeof(RhinoxFinger)))
            {
                // Get the corresponding joint positions
                if (!TryGetFingerJointPositions(hand, finger, out var jointPositions))
                    continue;

                // Calculate the distance for a total stretch
                // This distance is the sum of the distance between connected joints
                float fullStretchDistance = 0;
                for (int i = 0; i + 1 < jointPositions.Count; i++)
                {
                    var boneBeginPos = jointPositions[i];
                    var boneEndPos = jointPositions[i + 1];
                    fullStretchDistance += Vector3.Distance(boneBeginPos, boneEndPos);
                }

                SetFullStretchDistance(finger, fullStretchDistance);
            }
        }

        #endregion

        #region Collision Setters

        public void EnableHandCollisions(RhinoxHand rhinoxHand)
        {
            SetHandCollisions(true, rhinoxHand);
        }

        public void DisableHandCollisions(RhinoxHand rhinoxHand)
        {
            SetHandCollisions(false, rhinoxHand);
        }

        private IEnumerator SetHandCollisionsCoroutine(bool state, RhinoxHand rhinoxHand)
        {
            yield return new WaitForSecondsRealtime(ColliderActivationDelay);
            SetHandCollisions(state, rhinoxHand);
        }

        public void EnableHandCollisionsAfterDelay(RhinoxHand rhinoxHand)
        {
            StartCoroutine(SetHandCollisionsCoroutine(true, rhinoxHand));
        }

        public void DisableHandCollisionsAfterDelay(RhinoxHand rhinoxHand)
        {
            StartCoroutine(SetHandCollisionsCoroutine(false, rhinoxHand));
        }

        public void SetHandCollisions(bool state, RhinoxHand rhinoxHand)
        {
            if (!_jointCollisionsEnabled)
                return;

            switch (rhinoxHand)
            {
                case RhinoxHand.Left:
                    if (_leftHandCollidersParent)
                        _leftHandCollidersParent.SetActive(state);
                    break;
                case RhinoxHand.Right:
                    if (_rightHandCollidersParent)
                        _rightHandCollidersParent.SetActive(state);
                    break;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(SetHandCollisions)}, function called with invalid rhinoxHand value: {rhinoxHand}");
                    break;
            }
        }

        #endregion

        #region Event Methods

        private void OnTrackingAcquired(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Left:
                    _fixedUpdateAfterTrackingLeftFound = true;
                    IsLeftHandTracked = true;
                    break;
                case Handedness.Right:
                    _fixedUpdateAfterTrackingRightFound = true;
                    IsRightHandTracked = true;
                    break;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(OnTrackingAcquired)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    break;
            }

            if (_jointCollisionsEnabled)
                SetHandCollisions(true, hand.handedness.ToRhinoxHand());
            TrackingAcquired?.Invoke(hand.handedness.ToRhinoxHand());

            _firstHandUpdateAfterTrackingFound = true;
        }

        private void OnTrackingLost(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Invalid:
                    break;
                case Handedness.Left:
                    IsLeftHandTracked = false;
                    break;
                case Handedness.Right:
                    IsRightHandTracked = false;
                    break;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(OnTrackingLost)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    break;
            }

            if (_jointCollisionsEnabled)
                SetHandCollisions(false, hand.handedness.ToRhinoxHand());
            TrackingLost?.Invoke(hand.handedness.ToRhinoxHand());
        }

        #endregion

        #region Update Logic

        /// <summary>
        /// This function is a callback for the OnUpdatedHands event on the XRHandSubsystem.
        /// </summary>
        /// <param name="subsystem"></param>
        /// <param name="updateSuccessFlags"></param>
        /// <param name="updateType"></param>
        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            //This update types timing is similar to that of the Update
            //Use this for game logic
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
                return;

            if (updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose))
                UpdateRootPose(RhinoxHand.Left);

            if (updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints))
            {
                UpdateJoints(RhinoxHand.Left);
                if (_firstHandUpdateAfterTrackingFound)
                {
                    _firstHandUpdateAfterTrackingFound = false;
                    InitializeFingerBendFields(RhinoxHand.Left);
                }
            }

            if (updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose))
                UpdateRootPose(RhinoxHand.Right);

            if (updateSuccessFlags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints))
                UpdateJoints(RhinoxHand.Right);
        }

        private void Update()
        {
            if (_subsystem == null)
                TryEnsureInitialized();
        }

        /// <summary>
        /// Updates the root post (wrist)
        /// </summary>
        /// <param name="hand"></param>
        private void UpdateRootPose(RhinoxHand hand)
        {
            switch (hand)
            {
                case RhinoxHand.Left:
                {
                    var rootPose = _subsystem.leftHand.rootPose;
                    _leftHandJoints[0].JointPosition = rootPose.position;
                    _leftHandJoints[0].JointRotation = rootPose.rotation;
                    _leftHandJoints[0].Forward = rootPose.forward;

                    if (_subsystem.leftHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                        _leftHandJoints[0].JointRadius = radius;
                    break;
                }
                case RhinoxHand.Right:
                {
                    var rootPose = _subsystem.rightHand.rootPose;
                    _rightHandJoints[0].JointPosition = rootPose.position;
                    _rightHandJoints[0].JointRotation = rootPose.rotation;
                    _rightHandJoints[0].Forward = rootPose.forward;

                    if (_subsystem.rightHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                        _leftHandJoints[0].JointRadius = radius;
                    break;
                }
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(UpdateRootPose)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    break;
            }
        }

        /// <summary>
        /// Selects the correct data for the given hand and calls <see cref="UpdateJointsForHand"/> and <see cref="UpdateCapsuleColliders"/>.
        /// </summary>
        /// <param name="hand"> The handedness of the hand to update</param>
        private void UpdateJoints(RhinoxHand hand)
        {
            XRHand xrHand;
            List<RhinoxJoint> joints;

            switch (hand)
            {
                case RhinoxHand.Left:
                    xrHand = _subsystem.leftHand;
                    joints = _leftHandJoints;
                    break;
                case RhinoxHand.Right:
                    xrHand = _subsystem.rightHand;
                    joints = _rightHandJoints;
                    break;
                case RhinoxHand.Invalid:
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(UpdateJoints)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    return;
            }

            UpdateJointsForHand(xrHand, joints);
            UpdateCapsuleColliders(hand);
            OnHandsUpdated?.Invoke(hand);
        }

        /// <summary>
        /// This functions handles the update of the joints for one hand.
        /// </summary>
        /// <param name="hand"> The XRHand object of this hand</param>
        /// <param name="joints">The joints of the hand</param>
        private void UpdateJointsForHand(XRHand hand, List<RhinoxJoint> joints)
        {
            foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
            {
                if (jointId is XRHandJointID.Invalid or XRHandJointID.EndMarker)
                    continue;

                var currentJoint = joints[(int)jointId - 1];

                var subsystemJoint = hand.GetJoint(jointId);
                if (!subsystemJoint.TryGetPose(out var pose))
                    return;

                currentJoint.JointPosition = pose.position;
                currentJoint.JointRotation = pose.rotation;
                currentJoint.Forward = pose.forward;

                if (subsystemJoint.TryGetRadius(out var radius))
                    currentJoint.JointRadius = radius;
            }
        }

        /// <summary>
        /// This function updates the capsule collider of the hand with given handedness
        /// </summary>
        /// <param name="handedness"></param>
        private void UpdateCapsuleColliders(RhinoxHand handedness)
        {
            RhinoxJointCapsule[] capsules;
            switch (handedness)
            {
                case RhinoxHand.Left:
                    capsules = _leftHandCapsules;
                    break;
                case RhinoxHand.Right:
                    capsules = _rightHandCapsules;
                    break;
                default:
                    return;
            }

            if (capsules == null)
                return;

            foreach (var capsule in capsules)
            {
                if (capsule == null)
                    continue;
                //Get the transform of the connected joints
                var p0 = capsule.StartJoint.JointPosition;
                var p1 = capsule.EndJoint.JointPosition;
                var delta = p1 - p0;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                capsule.JointCollider.gameObject.transform.rotation = rot;
                capsule.JointCollider.enabled = true;
            }
        }

        private void FixedUpdate()
        {
            if (_fixedUpdateAfterTrackingLeftFound)
            {
                InitializeJointCapsules(RhinoxHand.Left);
                _fixedUpdateAfterTrackingLeftFound = false;
            }
            else if (IsLeftHandTracked)
                FixedUpdateCapsules(RhinoxHand.Left);

            if (_fixedUpdateAfterTrackingRightFound)
            {
                InitializeJointCapsules(RhinoxHand.Right);
                _fixedUpdateAfterTrackingRightFound = false;
            }
            else if (IsRightHandTracked)
                FixedUpdateCapsules(RhinoxHand.Right);
        }

        /// <summary>
        /// Selects the correct data for the given hand and calls <see cref="FixedUpdateCapsulesForHand"/>.
        /// </summary>
        /// <param name="rhinoxHand"></param>
        private void FixedUpdateCapsules(RhinoxHand rhinoxHand)
        {
            GameObject parent = null;
            RhinoxJointCapsule[] list = null;
            var joints = new List<RhinoxJoint>();
            switch (rhinoxHand)
            {
                case RhinoxHand.Left:
                    parent = _leftHandCollidersParent;
                    list = _leftHandCapsules;
                    joints = _leftHandJoints;
                    break;
                case RhinoxHand.Right:
                    parent = _rightHandCollidersParent;
                    list = _rightHandCapsules;
                    joints = _rightHandJoints;
                    break;
            }

            if (parent == null)
                return;

            FixedUpdateCapsulesForHand(parent, list, joints);
        }

        /// <summary>
        /// This functions handles the fixed update for one hand.
        /// </summary>
        /// <param name="parent"> The parentObject of the capsules of this hand</param>
        /// <param name="capsules"> The capsules of the hand</param>
        /// <param name="joints">The joints of the hand</param>
        private void FixedUpdateCapsulesForHand(GameObject parent, RhinoxJointCapsule[] capsules,
            List<RhinoxJoint> joints)
        {
            if (parent.activeSelf)
            {
                for (var i = 1; i < capsules.Length; i++)
                {
                    var capsule = capsules[i];
                    var joint = joints[i];
                    var rigidBodyGo = capsule.JointRigidbody.gameObject;
                    capsule.JointCollider.isTrigger = false;
                    if (rigidBodyGo.activeSelf)
                    {
                        capsule.JointRigidbody.MovePosition(joint.JointPosition);
                        capsule.JointRigidbody.MoveRotation(joint.JointRotation);
                        capsule.JointCollider.enabled = true;
                    }
                    else
                    {
                        rigidBodyGo.SetActive(true);
                        capsule.JointRigidbody.position = joint.JointPosition;
                        capsule.JointRigidbody.rotation = joint.JointRotation;
                    }
                }
            }
        }

        #endregion

        #region Get methods

        /// <summary>
        /// Attempts to get all joint of the given hand.
        /// </summary>
        /// <param name="rhinoxHand">The target hand</param>
        /// <param name="jointList">Out parameter for the list of joints</param>
        /// <returns>Whether the function succeeded</returns>
        public bool TryGetJointsFromHand(RhinoxHand rhinoxHand, out List<RhinoxJoint> jointList)
        {
            switch (rhinoxHand)
            {
                case RhinoxHand.Left:
                    jointList = _leftHandJoints;
                    return IsLeftHandTracked;
                case RhinoxHand.Right:
                    jointList = _rightHandJoints;
                    return IsRightHandTracked;
                default:
                    jointList = new List<RhinoxJoint>();
                    return false;
            }
        }

        public bool TryGetJointFromHandById(XRHandJointID jointID, RhinoxHand rhinoxHand, out RhinoxJoint joint)
        {
            switch (rhinoxHand)
            {
                case RhinoxHand.Left:
                    joint = _leftHandJoints.FirstOrDefault(rhinoxJoint => rhinoxJoint.JointID == jointID);
                    return IsLeftHandTracked && joint != null;
                case RhinoxHand.Right:
                    joint = _rightHandJoints.FirstOrDefault(rhinoxJoint => rhinoxJoint.JointID == jointID);
                    return IsRightHandTracked && joint != null;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(TryGetJointFromHandById)}, " +
                        $"function called with incorrect rhinoxHand {rhinoxHand}. Only left or right supported!");
                    joint = null;
                    return false;
            }
        }

        /// <summary>
        /// Attempts to get the bend value of the given finger on the given hand.This bend value is a float between 0 and 1;
        /// </summary>
        /// <param name="hand">The target hand</param>
        /// <param name="finger">The target finger</param>
        /// <param name="bendValue">Out parameter for the bend value</param>
        /// <param name="remap">Specifies whether to remap the values so the bend value is a percentage of [0, 1 - finger bend threshold]</param>
        /// <returns>Whether the function succeeded.</returns>
        /// <remarks>Not remapping the value, can make it impossible to recognize a finger as fully bend or stretched!</remarks>
        public bool TryGetFingerBend(RhinoxHand hand, RhinoxFinger finger, out float bendValue, bool remap = true)
        {
            bendValue = float.MaxValue;

            if (!TryGetFingerJointPositions(hand, finger, out var jointPositions))
                return false;

            if (!TryGetFingerBendThreshold(finger, out var bendThreshold))
                return false;

            // Get the full stretch distance corresponding with this finger
            if (!TryGetFingerStretchDistance(finger, out float fullStretchDistance))
                return false;

            // Calculate the distance between: 
            // - The root of the finger (metacarpal), this is the first element of the list of positions
            // - The tip of the finger (tip), this is the last element of the list of positions
            var fingerMetacarpalPos = jointPositions.First();
            var fingerTipPos = jointPositions.Last();
            float currentDist = Vector3.Distance(fingerMetacarpalPos, fingerTipPos);

            // The current bend value is the quotient of the current distance and the full stretch distance
            bendValue = Mathf.Clamp01(currentDist / fullStretchDistance);

            // Find the percentage of bend value in [0; 1 - bendThreshold] 
            if (remap)
            {
                var mappedMax = 1 - bendThreshold;
                var mappedValue = bendValue - bendThreshold;
                bendValue = Mathf.Clamp01(mappedValue / mappedMax);
            }

            return true;
        }

        /// <summary>
        /// This function attempts to return the full stretch distance for the given finger.
        /// </summary>
        /// <param name="finger"></param>
        /// <param name="stretchDistance"></param>
        /// <returns></returns>
        private bool TryGetFingerStretchDistance(RhinoxFinger finger, out float stretchDistance)
        {
            switch (finger)
            {
                case RhinoxFinger.Thumb:
                    stretchDistance = _thumbFullStretchVal;
                    break;
                case RhinoxFinger.Index:
                    stretchDistance = _indexFullStretchVal;
                    break;
                case RhinoxFinger.Middle:
                    stretchDistance = _middleFullStretchVal;
                    break;
                case RhinoxFinger.Ring:
                    stretchDistance = _ringFullStretchVal;
                    break;
                case RhinoxFinger.Little:
                    stretchDistance = _littleFullStretchVal;
                    break;
                default:
                    stretchDistance = float.MaxValue;
                    Debug.LogError($"TryGetFingerStretchDistance, RhinoxFinger {finger} not supported!");
                    return false;
            }

            return true;
        }

        /// <summary>
        /// This function attempts to return the minimum bend threshold for the given finger.
        /// </summary>
        /// <param name="finger">The finger to get the threshold of.</param>
        /// <param name="threshold">Out float parameter holding said threshold.</param>
        /// <returns></returns>
        private bool TryGetFingerBendThreshold(RhinoxFinger finger, out float threshold)
        {
            switch (finger)
            {
                case RhinoxFinger.Thumb:
                    threshold = ThumbBendThreshold;
                    break;
                case RhinoxFinger.Index:
                    threshold = IndexBendThreshold;
                    break;
                case RhinoxFinger.Middle:
                    threshold = MiddleBendThreshold;
                    break;
                case RhinoxFinger.Ring:
                    threshold = RingBendThreshold;
                    break;
                case RhinoxFinger.Little:
                    threshold = LittleBendThreshold;
                    break;
                default:
                    Debug.LogError($"TryGetFingerBendThreshold, RhinoxFinger {finger} is not supported!");
                    threshold = float.MaxValue;
                    return false;
            }

            return true;
        }


        /// <summary>
        /// Attempts to create a list of the joint positions of all joints of the given finger on the given hand.
        /// </summary>
        /// <param name="hand">The target hand</param>
        /// <param name="finger">The target finger</param>
        /// <param name="jointPositions">An out parameter that will hold the joint positions, if the function succeeds.</param>
        /// <returns>Whether the function succeeded</returns>
        private bool TryGetFingerJointPositions(RhinoxHand hand, RhinoxFinger finger, out List<Vector3> jointPositions)
        {
            //Get the joints of the given finger
            jointPositions = new List<Vector3>();
            var jointIds = GetJointIdsFromFinger(finger);
            var positions = Enumerable.Repeat(default(Vector3), jointIds.Count).ToList();
            int currentIdx = 0;
            foreach (XRHandJointID id in jointIds)
            {
                if (!TryGetJointFromHandById(id, hand, out var joint))
                    return false;
                positions[currentIdx] = joint.JointPosition;
                ++currentIdx;
            }

            jointPositions = positions;
            return true;
        }

        /// <summary>
        /// Gets all the XRHandJointIds from the given finger. 
        /// </summary>
        /// <param name="finger"></param>
        /// <returns>An ICollection holding all the joint ids of the given finger</returns>
        public static ICollection<XRHandJointID> GetJointIdsFromFinger(RhinoxFinger finger)
        {
            var returnValue = new Collection<XRHandJointID>();
            switch (finger)
            {
                case RhinoxFinger.Thumb:
                    returnValue.Add(XRHandJointID.ThumbMetacarpal);
                    returnValue.Add(XRHandJointID.ThumbProximal);
                    returnValue.Add(XRHandJointID.ThumbDistal);
                    returnValue.Add(XRHandJointID.ThumbTip);
                    break;
                case RhinoxFinger.Index:
                    returnValue.Add(XRHandJointID.IndexMetacarpal);
                    returnValue.Add(XRHandJointID.IndexProximal);
                    returnValue.Add(XRHandJointID.IndexIntermediate);
                    returnValue.Add(XRHandJointID.IndexDistal);
                    returnValue.Add(XRHandJointID.IndexTip);
                    break;
                case RhinoxFinger.Middle:
                    returnValue.Add(XRHandJointID.MiddleMetacarpal);
                    returnValue.Add(XRHandJointID.MiddleProximal);
                    returnValue.Add(XRHandJointID.MiddleIntermediate);
                    returnValue.Add(XRHandJointID.MiddleDistal);
                    returnValue.Add(XRHandJointID.MiddleTip);
                    break;
                case RhinoxFinger.Ring:
                    returnValue.Add(XRHandJointID.RingMetacarpal);
                    returnValue.Add(XRHandJointID.RingProximal);
                    returnValue.Add(XRHandJointID.RingIntermediate);
                    returnValue.Add(XRHandJointID.RingDistal);
                    returnValue.Add(XRHandJointID.RingTip);
                    break;
                case RhinoxFinger.Little:
                    returnValue.Add(XRHandJointID.LittleMetacarpal);
                    returnValue.Add(XRHandJointID.LittleProximal);
                    returnValue.Add(XRHandJointID.LittleIntermediate);
                    returnValue.Add(XRHandJointID.LittleDistal);
                    returnValue.Add(XRHandJointID.LittleTip);
                    break;
                default:
                    Debug.LogError(
                        $"GetJointIdsFromFinger, function called with unsupported RhinoxFinger value: {finger}");
                    return Array.Empty<XRHandJointID>();
            }

            return returnValue;
        }

        #endregion

        /// <summary>
        /// Sets the full stretch distance of the desired finger.
        /// </summary>
        /// <param name="finger">The finger of which to set the full stretch distance</param>
        /// <param name="newValue">The new value of the full stretch distance</param>
        private void SetFullStretchDistance(RhinoxFinger finger, float newValue)
        {
            switch (finger)
            {
                case RhinoxFinger.Thumb:
                    _thumbFullStretchVal = newValue;
                    break;
                case RhinoxFinger.Index:
                    _indexFullStretchVal = newValue;
                    break;
                case RhinoxFinger.Middle:
                    _middleFullStretchVal = newValue;
                    break;
                case RhinoxFinger.Ring:
                    _ringFullStretchVal = newValue;
                    break;
                case RhinoxFinger.Little:
                    _littleFullStretchVal = newValue;
                    break;
                default:
                    Debug.LogError($"SetFullStretchDistance, RhinoxFinger {finger} is not supported!");
                    break;
            }
        }
    }
}