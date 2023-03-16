using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;
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

        public event Action Initialized;
        public static event Action<JointManager> GlobalInitialized;
        
        private bool _fixedUpdateAfterTrackingLeftFound = false;
        private bool _fixedUpdateAfterTrackingRightFound = false;

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
            if(AreJointsInitialised)
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
                UpdateJoints(RhinoxHand.Left);

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
                if(!subsystemJoint.TryGetPose(out var pose))
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

        public IReadOnlyCollection<RhinoxJoint> GetJointsFromHand(Handedness hand)
        {
            switch (hand)
            {
                case Handedness.Left:
                    return _leftHandJoints;
                case Handedness.Right:
                    return _rightHandJoints;
                default:
                    Debug.LogError(
                        $"{nameof(JointManager)} - {nameof(GetJointsFromHand)}, " +
                        $"function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    return Array.Empty<RhinoxJoint>();
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

        #endregion
    }
}