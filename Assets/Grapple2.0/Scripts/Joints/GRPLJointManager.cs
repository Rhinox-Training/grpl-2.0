using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// Takes the Joint data from <see cref="XRHandSubsystem"/> and maps them to our own <see cref="RhinoxJoint"/> structure.<br />
    /// This manager also supplies and updates Capsule colliders with rigidbodies <see cref="RhinoxJointCapsule"/>.<br />
    /// This manager also relays events from the XRHandSubsystem when tracking is either lost or acquired.
    /// </summary>
    /// <remarks>Almost all classes in the Interaction Toolkit rely on this jointManager to function</remarks>
    /// <dependencies> <see cref="XRHandSubsystem"/> </dependencies>
    public class GRPLJointManager : MonoBehaviour
    {
        /// <summary>
        /// A private Vector3 variable named _handSocketOffset that represents the offset position of a socket used to
        /// attach an object to a hand in a 3D space.
        /// The value of this variable is set using magic numbers that were obtained through testing.
        /// </summary>
        [Header("Socket")] [SerializeField] private Vector3 _handSocketOffset = new Vector3(0f, -0.035f, 0.0725f);

        //-----------------------------
        // Finger bend fields
        //-----------------------------
        /// <summary>
        /// Float value that is used to remap the thumb bend value, if desired.
        /// </summary>
        [Header("Bend Thresholds")] [SerializeField]
        private float _thumbBendThreshold = 0.75f;

        /// <summary>
        /// Float value that is used to remap the index bend value, if desired.
        /// </summary>
        [SerializeField] private float _indexBendThreshold = 0.3f;

        /// <summary>
        /// Float value that is used to remap the middle bend value, if desired.
        /// </summary>
        [SerializeField] private float _middleBendThreshold = 0.25f;

        /// <summary>
        /// Float value that is used to remap the ring bend value, if desired.
        /// </summary>
        [SerializeField] private float _ringBendThreshold = 0.3f;

        /// <summary>
        /// Float value that is used to remap the little bend value, if desired.
        /// </summary>
        [SerializeField] private float _littleBendThreshold = 0.3f;

        /// <summary>
        /// An int variable that represents the layer of the hand. This variable is decorated with the Layer attribute
        /// </summary>
        [Layer] public int HandLayer = 3;

        /// <summary>
        /// A float variable that represents the delay time before colliders are reactivated with a delay.
        /// </summary>
        public float ColliderActivationDelay = 1.5f;

        /// <summary>
        /// A static event that is triggered when the GRPLJointManager is initialized globally, which passes itself as a parameter.
        /// </summary>
        public static event Action<GRPLJointManager> GlobalInitialized;

        /// <summary>
        /// An event that is triggered when tracking is acquired for a hand.
        /// </summary>
        public event Action<RhinoxHand> TrackingAcquired;

        /// <summary>
        /// An event that is triggered when tracking is lost for a hand.
        /// </summary>
        public event Action<RhinoxHand> TrackingLost;

        /// <summary>
        /// An event that is triggered when a hand is updated.
        /// </summary>
        public event Action<RhinoxHand> OnHandsUpdated;

        /// <summary>
        /// An event that is triggered when the joint capsules are initialized.
        /// </summary>
        public event Action<RhinoxHand> OnJointCapsulesInitialized;

        /// <summary>
        /// A boolean variable that indicates if the left hand is being tracked.
        /// </summary>
        public bool IsLeftHandTracked { get; private set; } = false;

        /// <summary>
        /// A boolean variable that indicates if the right hand is being tracked.
        /// </summary>
        public bool IsRightHandTracked { get; private set; } = false;

        /// <summary>
        /// A boolean variable that indicates if the joints are initialized.
        /// </summary>
        public bool AreJointsInitialised { get; private set; } = false;

        /// <summary>
        /// A boolean property that gets or sets the value of _jointCollisionsEnabled.
        /// When this value is set, it also sets the collisions of the hands with the joint capsules.
        /// </summary>
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

        /// <summary>
        /// An array of RhinoxJointCapsule objects that represent the joint capsules of the left hand.
        /// </summary>
        public RhinoxJointCapsule[] LeftHandCapsules => _leftHandCapsules;

        /// <summary>
        /// An array of RhinoxJointCapsule objects that represent the joint capsules of the right hand.
        /// </summary>
        public RhinoxJointCapsule[] RightHandCapsules => _rightHandCapsules;

        /// <summary>
        /// A GameObject variable that represents the parent object of the left hand.
        /// </summary>
        public GameObject LeftHandParentObj => _leftHandParent;

        /// <summary>
        /// A GameObject variable that represents the socket used to attach objects to the left hand.
        /// </summary>
        public GameObject LeftHandSocket => _leftHandSocket;

        /// <summary>
        /// A GameObject variable that represents the parent object of the right hand.
        /// </summary>
        public GameObject RightHandParentObj => _rightHandParent;

        /// <summary>
        /// A GameObject variable that represents the socket used to attach objects to the right hand.
        /// </summary>
        public GameObject RightHandSocket => _rightHandSocket;


        private bool _jointCollisionsEnabled = true;

        private XRHandSubsystem _subsystem;

        private List<RhinoxJoint> _leftHandJoints = new List<RhinoxJoint>();
        private List<RhinoxJoint> _rightHandJoints = new List<RhinoxJoint>();

        private RhinoxJointCapsule[] _leftHandCapsules;
        private RhinoxJointCapsule[] _rightHandCapsules;

        private GameObject _leftHandCollidersParent;
        private GameObject _rightHandCollidersParent;

        private GameObject _leftHandParent;
        private GameObject _leftHandSocket;
        private GameObject _rightHandParent;
        private GameObject _rightHandSocket;

        private bool _fixedUpdateAfterTrackingLeftFound = false;
        private bool _fixedUpdateAfterTrackingRightFound = false;
        private bool _firstHandUpdateAfterTrackingFound = false;

        // Finger full stretch values
        private float _thumbFullStretchVal = 0;
        private float _indexFullStretchVal = 0;
        private float _middleFullStretchVal = 0;
        private float _ringFullStretchVal = 0;
        private float _littleFullStretchVal = 0;

        private event Action Initialized;


        //======================
        //Initialization Methods
        //======================
        private void Awake()
        {
            _leftHandParent = new GameObject("Left Hand");
            _leftHandParent.transform.SetParent(transform);

            _leftHandSocket = new GameObject("Socket");
            _leftHandSocket.transform.SetParent(_leftHandParent.transform);
            //needs to be rotate 90°, otherwise object would go through handpalm and this one is rotate another 180°, because it's the opposite of right hand
            _leftHandSocket.transform.SetLocalPositionAndRotation(_handSocketOffset, Quaternion.Euler(0f, 0f, 270f));


            _rightHandParent = new GameObject("Right Hand");
            _rightHandParent.transform.SetParent(transform);

            _rightHandSocket = new GameObject("Socket");
            _rightHandSocket.transform.SetParent(_rightHandParent.transform);

            //needs to be rotate 90°, otherwise object would go through handpalm.
            _rightHandSocket.transform.SetLocalPositionAndRotation(_handSocketOffset, Quaternion.Euler(0f, 0f, 90f));


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
                return;
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
                    parentTransform.SetParent(_leftHandParent.transform, false);
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
                    parentTransform.SetParent(_rightHandParent.transform, false);
                    parentTransform.localPosition = Vector3.zero;
                    parentTransform.localRotation = Quaternion.identity;
                    break;
                }
                default:
                    PLog.Error<GRPLLogger>(
                        $"[GRPLJointManager:InitializeJointCapsules], " +
                        $"function called with incorrect rhinoxHand {handedness}. Only left or right supported!", this);
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

            OnJointCapsulesInitialized?.Invoke(handedness);
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
            //Loop over the capsules and process them (from tip to wrist)
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
                //rbGo.tag = hand.ToString();
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

        //=================
        //Collision Setters
        //=================
        /// <summary>
        /// This method enables collisions for the hand specified by the RhinoxHand parameter.
        /// It internally calls the method SetHandCollisions with a true value for the state parameter and the specified RhinoxHand.
        /// </summary>
        /// <param name="hand">Hand to enable collisions on.</param>
        public void EnableHandCollisions(RhinoxHand hand)
        {
            SetHandCollisions(true, hand);
        }

        /// <summary>
        /// This method enables collisions for the hand specified by the RhinoxHand parameter.
        /// It internally calls the method SetHandCollisions with a false value for the state parameter and the specified RhinoxHand.
        /// </summary>
        /// <param name="hand">Hand to disable collisions on.</param>
        public void DisableHandCollisions(RhinoxHand hand)
        {
            SetHandCollisions(false, hand);
        }

        private IEnumerator SetHandCollisionsCoroutine(bool state, RhinoxHand hand)
        {
            yield return new WaitForSecondsRealtime(ColliderActivationDelay);
            SetHandCollisions(state, hand);
        }

        public void EnableHandCollisionsAfterDelay(RhinoxHand hand)
        {
            StartCoroutine(SetHandCollisionsCoroutine(true, hand));
        }

        public void DisableHandCollisionsAfterDelay(RhinoxHand hand)
        {
            StartCoroutine(SetHandCollisionsCoroutine(false, hand));
        }

        public void SetHandCollisions(bool state, RhinoxHand hand)
        {
            if (!_jointCollisionsEnabled)
                return;

            switch (hand)
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
                        $"{nameof(GRPLJointManager)} - {nameof(SetHandCollisions)}, function called with invalid rhinoxHand value: {hand}");
                    break;
            }
        }

        //=============
        //Event Methods
        //=============
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
                        $"{nameof(GRPLJointManager)} - {nameof(OnTrackingAcquired)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
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
                        $"{nameof(GRPLJointManager)} - {nameof(OnTrackingLost)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
                    break;
            }

            if (_jointCollisionsEnabled)
                SetHandCollisions(false, hand.handedness.ToRhinoxHand());
            TrackingLost?.Invoke(hand.handedness.ToRhinoxHand());
        }

        //============
        //Update Logic
        //============
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
            {
                UpdateRootPose(RhinoxHand.Left);
                _leftHandParent.transform.SetLocalPositionAndRotation(_leftHandJoints[0].JointPosition,
                    _leftHandJoints[0].JointRotation);
            }

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
            {
                UpdateRootPose(RhinoxHand.Right);
                _rightHandParent.transform.SetLocalPositionAndRotation(_rightHandJoints[0].JointPosition,
                    _rightHandJoints[0].JointRotation);
            }

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
                    var worldPose = rootPose.GetTransformedBy(new Pose(transform.position, transform.rotation));

                    _leftHandJoints[0].JointPosition = worldPose.position;
                    _leftHandJoints[0].JointRotation = worldPose.rotation;
                    _leftHandJoints[0].Forward = rootPose.forward;

                    if (_subsystem.leftHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                        _leftHandJoints[0].JointRadius = radius;
                    break;
                }
                case RhinoxHand.Right:
                {
                    var rootPose = _subsystem.rightHand.rootPose;
                    var worldPose = rootPose.GetTransformedBy(new Pose(transform.position, transform.rotation));

                    _rightHandJoints[0].JointPosition = worldPose.position;
                    _rightHandJoints[0].JointRotation = worldPose.rotation;
                    _rightHandJoints[0].Forward = rootPose.forward;

                    if (_subsystem.rightHand.GetJoint(XRHandJointID.Wrist).TryGetRadius(out var radius))
                        _leftHandJoints[0].JointRadius = radius;
                    break;
                }
                default:
                    PLog.Error<GRPLLogger>($"[GRPLJointManager:UpdateRootPose], " +
                                              $"function called with incorrect rhinoxHand {hand}. Only left or right supported!",
                        this);
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
                        $"{nameof(GRPLJointManager)} - {nameof(UpdateJoints)}, function called with incorrect rhinoxHand {hand}. Only left or right supported!");
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
            Vector3 rigPos = transform.position;
            Quaternion rigRot = transform.rotation;

            foreach (XRHandJointID jointId in Enum.GetValues(typeof(XRHandJointID)))
            {
                if (jointId is XRHandJointID.Invalid or XRHandJointID.EndMarker)
                    continue;

                var currentJoint = joints[(int)jointId - 1];

                var subsystemJoint = hand.GetJoint(jointId);
                if (!subsystemJoint.TryGetPose(out var pose))
                    return;

                var worldPose = pose.GetTransformedBy(new Pose(rigPos, rigRot));

                currentJoint.JointPosition = worldPose.position;
                currentJoint.JointRotation = worldPose.rotation;
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

        //===========
        //Get methods
        //===========
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
                        $"{nameof(GRPLJointManager)} - {nameof(TryGetJointFromHandById)}, " +
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
                    threshold = _thumbBendThreshold;
                    break;
                case RhinoxFinger.Index:
                    threshold = _indexBendThreshold;
                    break;
                case RhinoxFinger.Middle:
                    threshold = _middleBendThreshold;
                    break;
                case RhinoxFinger.Ring:
                    threshold = _ringBendThreshold;
                    break;
                case RhinoxFinger.Little:
                    threshold = _littleBendThreshold;
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
            var jointIds = finger.GetJointIdsFromFinger();
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
        /// Gets all joints with XRHandJointID "jointID" from both hands.
        /// </summary>
        /// <param name="jointID">The desired joint ID</param>
        /// <returns> An ICollection holding all the RhinoxJoints corresponding with jointID</returns>
        public ICollection<RhinoxJoint> GetJointsFromBothHand(XRHandJointID jointID)
        {
            ICollection<RhinoxJoint> returnVal = new List<RhinoxJoint>();

            // Create compare local function
            bool Match(RhinoxJoint joint) => joint.JointID == jointID;

            // If the left hand is being tracked,
            // Get all instances in the left hand
            if (IsLeftHandTracked)
                returnVal.AddRange(_leftHandJoints.FindAll(Match));

            // If the right hand is being tracked,
            // Get all instances in the right hand
            if (IsRightHandTracked)
                returnVal.AddRange(_rightHandJoints.FindAll(Match));

            return returnVal.Count == 0 ? Array.Empty<RhinoxJoint>() : returnVal;
        }

        //==============
        //Finger Bending
        //==============
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