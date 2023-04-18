using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Collections;
using Rhinox.Perceptor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// Teleport script that can be used with handtracking or controller.<br />
    /// Nothing needs to be called externally when usinghandtracking, as all the internal logic takes care of that.<br />
    /// When using it with controller, the user needs to call '<see cref="ShowArc"/>' to toggle the arc visual
    /// and <see cref="CalculateTeleportLocation(Ray)"/>, where the ray is the startpoint and direction the teleport arc should go in.
    /// </summary>
    /// <remarks />
    /// <dependencies>
    /// - <see cref="GRPLJointManager"/>
    /// - <see cref="GRPLGestureRecognizer"/>
    /// </dependencies>
    public class GRPLTeleport : MonoBehaviour
    {
        /// <summary>
        /// The name of the gesture used to initiate the teleportation.
        /// </summary>
        [Header("Teleport Gesture")] [SerializeField]
        private string _teleportGestureName = "Teleport";

        /// <summary>
        /// A game object that represents the teleportation area.
        /// </summary>
        [Header("Visuals")] [SerializeField] private GameObject _sensorModel = null;

        /// <summary>
        /// A visual representation of the teleportation zone.
        /// </summary>
        [SerializeField] private GameObject _teleportZoneVisual = null;

        /// <summary>
        /// A component that draws a parabolic arc from the user's hand to the target location.
        /// </summary>
        [Header("Arc Visual Settings")] [SerializeField]
        private LineRenderer _lineRenderer = null;

        /// <summary>
        /// The time delay before the line renderer and teleportation zone visual are enabled.
        /// </summary>
        [SerializeField] private float _visualStartDelay = 0.3f;

        /// <summary>
        /// The time delay before the line renderer and teleportation zone visual are disabled.
        /// </summary>
        [SerializeField] private float _visualStopDelay = 0.5f;

        /// <summary>
        /// The cooldown time for teleportation.
        /// </summary>
        [SerializeField] private float _teleportCooldown = 1.5f;

        /// <summary>
        /// The number of points used to smooth the destination point.
        /// </summary>
        [Header("Arc General Settings")] [Range(1, 10)] [SerializeField]
        private int _destinationSmoothingAmount = 5;

        /// <summary>
        /// The maximum distance that the user can teleport.
        /// </summary>
        [SerializeField] private float _maxDistance = 50f;

        /// <summary>
        /// The lowest point that the user can teleport to.
        /// </summary>
        [SerializeField] private float _lowestHeight = -50f;

        /// <summary>
        /// The initial velocity of the parabolic arc.
        /// </summary>
        [SerializeField] private float _initialVelocity = 1f;

        /// <summary>
        /// The number of sub-iterations used to calculate the parabolic arc.
        /// </summary>
        [Range(0.001f, 2f)] [SerializeField] private float _lineSubIterations = 1f;

        /// <summary>
        /// A flag that enables or disables snapping to a valid teleportation point.
        /// </summary>
        [Header("Snapping")] [SerializeField] private bool _enableSnapping = true;

        /// <summary>
        /// The maximum distance that the user can snap to a valid teleportation point.
        /// </summary>
        [SerializeField] private float _maxSnapDistance = 2.5f;

        /// <summary>
        /// The NavMesh area mask that specifies which areas are valid for teleportation.
        /// </summary>
        [Header("Miscellaneous Settings")] [NavMeshArea(true)] [SerializeField]
        private int _teleportableNavMeshAreas;

        /// <summary>
        /// Enable or disable the line rendering
        /// </summary>
        public bool ShowArc
        {
            get { return _lineRenderer.enabled; }
            set
            {
                _lineRenderer.enabled = value;
                _teleportZoneVisual.SetActive(value);
            }
        }

        /// <summary>
        /// Public getter property for _isInitialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Public getter property for _isValidTeleportPoint.
        /// </summary>
        public bool IsValidTeleportPoint => _isValidTeleportPoint;

        /// <summary>
        /// Public getter property for _avgTeleportPoint.
        /// </summary>
        public Vector3 TeleportPoint => _avgTeleportPoint;


        /// <summary>
        /// The joint manager that is responsible for tracking the user's hand.
        /// </summary>
        private GRPLJointManager _jointManager = null;

        /// <summary>
        /// The gesture that initiates the teleportation.
        /// </summary>
        private RhinoxGesture _teleportGesture = null;

        /// <summary>
        /// A flag that indicates whether the class is initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// The user's hand.
        /// </summary>
        private RhinoxHand _hand = RhinoxHand.Invalid;

        /// <summary>
        /// The layer that the user's hand is on.
        /// </summary>
        private int _handLayer;

        /// <summary>
        /// The coroutine that starts the line renderer and teleportation zone visual.
        /// </summary>
        private Coroutine _startVisualCoroutine = null;

        /// <summary>
        /// The coroutine that stops the line renderer and teleportation zone visual.
        /// </summary>
        private Coroutine _stopVisualCoroutine = null;

        /// <summary>
        /// A queue that stores the recent teleportation points.
        /// </summary>
        private LimitedQueue<Vector3> _teleportPositions = new LimitedQueue<Vector3>(5);

        /// <summary>
        /// The left sensor object.
        /// </summary>
        private GameObject _sensorObjL = null;

        /// <summary>
        /// The right sensor object.
        /// </summary>
        private GameObject _sensorObjR = null;

        /// <summary>
        /// The left trigger sensor.
        /// </summary>
        private GRPLTriggerSensor _proxySensorL = null;

        /// <summary>
        /// The right trigger sensor.
        /// </summary>
        private GRPLTriggerSensor _proxySensorR = null;

        /// <summary>
        /// The average teleportation point.
        /// </summary>
        private Vector3 _avgTeleportPoint = Vector3.zero;

        /// <summary>
        /// The gravitational constant.
        /// </summary>
        private const float _gravity = -9.81f;

        /// <summary>
        /// The offset for the sensor object.
        /// </summary>
        private Vector3 _sensorOffset = new Vector3(0f, 0f, -0.06f);

        /// <summary>
        /// A flag that indicates whether the teleportation is on cooldown.
        /// </summary>
        private bool _isOnCooldown = false;

        /// <summary>
        /// A flag that indicates whether the current teleportation point is valid.
        /// </summary>
        private bool _isValidTeleportPoint = false;

        /// <summary>
        /// A flag that indicates whether the left sensor is enabled.
        /// </summary>
        private bool _isEnabledL = false;

        /// <summary>
        /// A flag that indicates whether the right sensor is enabled.
        /// </summary>
        private bool _isEnabledR = false;

        // The logic of how the teleport code works:
        // When a hand makes the teleport gesture it will call the <see cref="StartVisualCoroutine"/> which starts a timer before actualy enabling the teleport logic
        //     - If that hand stops making the teleport before the timer finishes the <see cref="StartVisualCoroutine"/> will be stopped
        //     - If the other hand makes the teleport visual, nothing will happen.
        //     - If the timer is complete, then the teleport arc will be calculated from the hand that made the gesture.
        //     
        // When the hand that the teleport arc is coming from stops making the teleport gesture the <see cref="StopVisualCoroutine"/> gets called, which starts a timer to stop the teleport logic
        //     - If that hand starts making the gesture again before the timer finishes the <see cref="StopVisualCoroutine"/> will be stopped.
        //     - If the other hand stops making the teleport visual, nothing will happen.
        //     - If the timer is complete, then the teleport arc calculation will be stopped including it being visualized
        // 
        // When the the other hand enters the trigger on the wrist of the hand emmiting the teleport arc, the teleport location will be confirmed and the player gets teleported.
        // This will put the teleport on cooldown, to prevent spamming/accidental teleporting.


        /// <summary>
        /// Subscribes to the GlobalInitialized events of the GRPLJointManager and GRPLGestureRecognizer.
        /// If the _teleportableNavMeshAreas is zero or the _lineRenderer is null, the function logs a warning
        /// </summary>
        private void Start()
        {
            GRPLJointManager.GlobalInitialized += JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;

            if (_teleportableNavMeshAreas == 0)
                PLog.Warn<GRPLITLogger>($"[GRPLTeleport:Start]," +
                                        $" Teleportable NavMesh Areas was {LayerMask.LayerToName(_teleportableNavMeshAreas)}",
                    this);

            if (_lineRenderer == null)
            {
                PLog.Error<GRPLITLogger>($"Given {nameof(LineRenderer)} was NULL", this);
                return;
            }
        }

        private void JointManagerInitialized(GRPLJointManager jointManager)
        {
            _jointManager = jointManager;

            if (_jointManager != null && _teleportGesture != null)
                Initialize();
        }

        private void GestureRecognizerInitialized(GRPLGestureRecognizer gestureRecognizer)
        {
            //getting the grab gesture and linking events
            _teleportGesture = gestureRecognizer.Gestures.Find(x => x.Name == _teleportGestureName);
            if (_teleportGesture != null)
            {
                _teleportGesture.AddListenerOnRecognized(StartedPointing);
                _teleportGesture.AddListenerOnUnRecognized(StoppedPointing);
            }

            if (_jointManager != null && _teleportGesture != null)
                Initialize();
        }

        /// <summary>
        /// Initializes the Teleport system by hooking up to the <see cref="GRPLJointManager"/> events and to the <see cref="GRPLGestureRecognizer"/> teleport gesture.<br></br>
        /// This also sets up the proximity sensor to the correct location and to only trigger on the the other hand. 
        /// </summary>
        /// <remarks>
        /// Both <see cref="GRPLJointManager"/> AND <see cref="GRPLGestureRecognizer"/>should be initialized before calling this.
        /// </remarks>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (_jointManager == null)
            {
                PLog.Error<GRPLITLogger>($"[GRPLTeleport:Initialize], {nameof(GRPLJointManager)} was NULL", this);
                return;
            }

            if (_teleportGesture == null)
            {
                PLog.Error<GRPLITLogger>(
                    $"[GRPLTeleport:Initialize], no teleport gesture with name {_teleportGestureName} was found in GestureRecognizer",
                    this);
                return;
            }

            if (_sensorModel == null)
            {
                PLog.Error<GRPLITLogger>($"[GRPLTeleport:Initialize], no teleportZoneVisual was NULL", this);
                return;
            }

            if (_teleportZoneVisual == null)
            {
                _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _teleportZoneVisual.transform.localScale = new Vector3(.5f, .1f, .5f);
                Destroy(_teleportZoneVisual.GetComponent<BoxCollider>());

                PLog.Warn<GRPLITLogger>(
                    $"[GRPLTeleport:Initialize], no teleportZoneVisual was given, fallback to Cube Primitive", this);
            }

            _handLayer = _jointManager.HandLayer;

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;
            _jointManager.OnJointCapsulesInitialized += InitializeIgnoreList;

            if (!SensorSetup(out _sensorObjL, RhinoxHand.Left, _jointManager.LeftHandParentObj.transform,
                    out _proxySensorL))
                return;

            if (!SensorSetup(out _sensorObjR, RhinoxHand.Right, _jointManager.RightHandParentObj.transform,
                    out _proxySensorR))
                return;

            _lineRenderer.enabled = false;
            _teleportPositions.Limit = _destinationSmoothingAmount;

            _isInitialized = true;
        }

        /// <summary>
        /// This method initializes a list of colliders to ignore during raycasting. This list includes the sensors,
        /// the teleportation zone visual, and the player's own colliders.
        /// </summary>
        /// <param name="handedness">The hand that triggered the colliderInitialize</param>
        private void InitializeIgnoreList(RhinoxHand handedness)
        {
            switch (handedness)
            {
                case RhinoxHand.Left:
                    _proxySensorL.SetIgnoreList(_jointManager.LeftHandCapsules);
                    break;
                case RhinoxHand.Right:
                    _proxySensorR.SetIgnoreList(_jointManager.RightHandCapsules);
                    break;
                default:
                    PLog.Error<GRPLITLogger>(
                        $"{nameof(GRPLTeleport)} - {nameof(InitializeIgnoreList)}" +
                        $", function called with incorrect rhinoxHand {handedness}. Only left or right supported!",
                        this);
                    break;
            }
        }

        /// <summary>
        /// Intial setup function to create and place the sensor object and visual part correctly onto the given hand and setting up the events.
        /// </summary>
        /// <param name="sensorObj">The main object where the sensor script will be placed onto and the sensor model will be childed under</param>
        /// <param name="hand">Mainly to give the sensor <see cref="GameObject"/> the correct name</param>
        /// <param name="proxySensor">The sensor logic to hook into the events</param>
        /// <returns>If the sensor setup for the hand succeeded.</returns>
        private bool SensorSetup(out GameObject sensorObj, RhinoxHand hand, Transform handParentObj,
            out GRPLTriggerSensor proxySensor)
        {
            sensorObj = Instantiate(_sensorModel, _sensorOffset, Quaternion.identity, handParentObj);
            sensorObj.name = $"{hand}Hand Sensor";
            sensorObj.SetActive(false);

            if (!sensorObj.TryGetComponent<Collider>(out var sensCollider))
            {
                PLog.Error<GRPLITLogger>(
                    $"[GRPLTeleport:SensorSetup], Sensor model prefab {nameof(_sensorModel)} Did not contain a form of collider",
                    this);
                proxySensor = null;
                return false;
            }

            sensCollider.isTrigger = true;
            proxySensor = sensorObj.GetOrAddComponent<GRPLTriggerSensor>();
            proxySensor.HandLayer = _handLayer; // LayerMask.NameToLayer("Hands");
            proxySensor.AddListenerOnSensorEnter(ConfirmTeleport);
            return true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
                return;

            GRPLJointManager.GlobalInitialized += JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized += GestureRecognizerInitialized;

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;
            _jointManager.OnJointCapsulesInitialized += InitializeIgnoreList;

            _teleportGesture.AddListenerOnRecognized(StartedPointing);
            _teleportGesture.AddListenerOnUnRecognized(StoppedPointing);

            _proxySensorL.AddListenerOnSensorEnter(ConfirmTeleport);
            _proxySensorR.AddListenerOnSensorEnter(ConfirmTeleport);
        }

        private void OnDisable()
        {
            if (!_isInitialized)
                return;

            GRPLJointManager.GlobalInitialized -= JointManagerInitialized;
            GRPLGestureRecognizer.GlobalInitialized -= GestureRecognizerInitialized;

            _jointManager.TrackingAcquired -= TrackingAcquired;
            _jointManager.TrackingLost -= TrackingLost;
            _jointManager.OnJointCapsulesInitialized -= InitializeIgnoreList;

            _teleportGesture.RemoveListenerOnRecognized(StartedPointing);
            _teleportGesture.RemoveListenerOnUnRecognized(StoppedPointing);

            _proxySensorL.RemoveListenerOnSensorEnter(ConfirmTeleport);
            _proxySensorR.RemoveListenerOnSensorEnter(ConfirmTeleport);

            StopAllCoroutines();
        }

        /// <summary>
        /// This method is called every frame to update the teleportation arc. If ShowArc is true and _hand is a valid
        /// hand, it gets the Wrist and MiddleMetacarpal joint positions from the JointManager and uses them to create
        /// a Ray that will be used to calculate the teleportation location by calling the CalculateTeleportLocation
        /// method.
        /// </summary>
        private void Update()
        {
            if (ShowArc && _hand != RhinoxHand.Invalid)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, _hand,
                        out var wrist))
                    return;
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.MiddleMetacarpal, _hand,
                        out var palm))
                    return;

                Ray ray = new Ray(wrist.JointPosition, (palm.JointPosition - wrist.JointPosition).normalized);

                CalculateTeleportLocation(ray);
            }
        }

        /// <summary>
        /// This method takes a Ray as input and calculates the points of the teleportation arc using the initial
        /// velocity, gravity, and maximum distance. It then calls CalculateIntersectionPoint to find the intersection
        /// point between the arc and the environment. If a valid teleportation point is found, it sets
        /// _avgTeleportPoint to the average position of all the valid teleportation points, sets the position of the
        /// _teleportZoneVisual game object to _avgTeleportPoint, and sets _isValidTeleportPoint to true. Otherwise, it
        /// sets _lineRenderer's colors to red, sets _isValidTeleportPoint to false, and clears the _teleportPositions
        /// queue.
        /// </summary>
        /// <param name="startRay">Ray that gives a start point and direction the teleport arc should go in</param>
        public void CalculateTeleportLocation(Ray startRay)
        {
            var aimPosition = startRay.origin;
            var aimDirection = startRay.direction * _initialVelocity;
            var rangeSquared = _maxDistance * _maxDistance;
            List<Vector3> points = new List<Vector3>();

            //calculate all points (logic based on the arc calculations from Meta)
            do
            {
                points.Add(aimPosition);

                var aimVector = aimDirection;
                aimVector.y +=
                    _gravity * 0.0111111111f *
                    _lineSubIterations; //0.0111111111f is a magic constant I got from ovr TeleportAimHandlerParabolic.cs 
                aimDirection = aimVector;
                aimPosition += aimVector;
            } while ((aimPosition.y - startRay.origin.y > _lowestHeight) &&
                     ((startRay.origin - aimPosition).sqrMagnitude <= rangeSquared));


            //calc ground intersection point
            CalculateIntersectionPoint(points, out Vector3 intersectPoint, out int indexNextPoint);

            //if it valid calculate the avg, if not reset and hide
            if (_isValidTeleportPoint)
            {
                _avgTeleportPoint = new Vector3(_teleportPositions.Average(vec => vec.x),
                    _teleportPositions.Average(vec => vec.y),
                    _teleportPositions.Average(vec => vec.z));

                _teleportZoneVisual.transform.position = _avgTeleportPoint;
                _teleportZoneVisual.SetActive(true);
            }
            else
            {
                _lineRenderer.startColor = Color.red;
                _lineRenderer.endColor = Color.red;
                _teleportZoneVisual.SetActive(false);
                _teleportPositions.Clear();
            }

            //rendering part
            _lineRenderer.positionCount = indexNextPoint;
            for (int index = 0; index < indexNextPoint - 1; index++)
            {
                _lineRenderer.SetPosition(index, points[index]);
            }

            if (!intersectPoint.AnyIsNegativeInfinity())
                _lineRenderer.SetPosition(indexNextPoint - 1, intersectPoint);
        }

        /// <summary>
        /// This method takes a list of points on the teleportation arc and finds the first intersection point between
        /// the arc and the environment by casting a ray from each point to the next point in the list. If a valid
        /// teleportation point is found, it sets _lineRenderer's colors to green, sets _isValidTeleportPoint to true,
        /// and sets intersectPoint to the intersection point. If snapping is enabled and the hit object has a
        /// GRPLTeleportAnchor component, it clears _teleportPositions, adds the anchor position to _teleportPositions,
        /// and returns true. Otherwise, it calls CheckAndAddIfPointOnNavmesh to check if the hit point is on the
        /// navmesh and adds it to _teleportPositions if it is. If a valid teleportation point is not found, it sets _lineRenderer's colors to red, sets _isValidTeleportPoint to false, and clears the _teleportPositions queue.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="intersectPoint"></param>
        /// <param name="indexNextPoint"></param>
        private void CalculateIntersectionPoint(List<Vector3> points, out Vector3 intersectPoint,
            out int indexNextPoint)
        {
            intersectPoint = Vector3.negativeInfinity;
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
            _isValidTeleportPoint = false;

            indexNextPoint = 1;
            for (int index = 0; indexNextPoint < points.Count; ++index, ++indexNextPoint)
            {
                Ray currentRay = new Ray(points[index], points[indexNextPoint] - points[index]);

                //check which segment of the arc intersects with the ground
                //~(0b1 << _handLayer) converts the handlayer indext to a mask via bitshifting
                if (Physics.Raycast(currentRay, out var hitInfo,
                        Vector3.Distance(points[indexNextPoint], points[index]), ~(0b1 << _handLayer)))
                {
                    //if it was a collider, check if it has a teleportanchor
                    if (_enableSnapping && hitInfo.collider.TryGetComponent<GRPLTeleportAnchor>(out var teleportAnchor))
                    {
                        if (Vector3.Distance(teleportAnchor.transform.position, hitInfo.point) <= _maxSnapDistance)
                        {
                            _teleportPositions.Clear();
                            _teleportPositions.Enqueue(teleportAnchor.AnchorTransform.position);
                            _isValidTeleportPoint = true;
                        }
                        else
                            _isValidTeleportPoint = CheckAndAddIfPointOnNavmesh(hitInfo.point);
                    }
                    else
                        _isValidTeleportPoint = CheckAndAddIfPointOnNavmesh(hitInfo.point);

                    intersectPoint = hitInfo.point;

                    if (!_isOnCooldown)
                    {
                        _lineRenderer.startColor = Color.green;
                        _lineRenderer.endColor = Color.green;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// This method takes a position and checks if it is on the navmesh by calling NavMesh.SamplePosition.
        /// If the position is on the navmesh, it adds it to the _teleportPositions queue and returns true.
        /// Otherwise, it returns false.
        /// </summary>
        /// <param name="pos"> The position to check on the navmesh</param>
        /// <returns>Returns <see langword="true"/> if the given point is on the navmesh</returns>
        private bool CheckAndAddIfPointOnNavmesh(Vector3 pos)
        {
            //adding .5f to the y axis to avoid the problem of the position is at the same level as the navmesh making the navmesh raycast fail
            if (NavMesh.SamplePosition(new Vector3(pos.x, pos.y + .5f, pos.z), out var info, 1f,
                    _teleportableNavMeshAreas)) //1 << NavMesh.GetAreaFromName("Teleportable")))
            {
                _teleportPositions.Enqueue(pos);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method is called when the user confirms a teleportation.
        /// If the teleport is on cooldown or the teleportation point is not valid, it returns without doing anything.
        /// Otherwise, it sets the position of the game object to _avgTeleportPoint, sets _lineRenderer's colors to red,
        /// clears the _teleportPositions queue, and sets _isOnCooldown to true. It then calls ResetCooldown after
        /// _teleportCooldown seconds to reset the cooldown and activate the sensor object for the hand that was used to
        /// teleport.
        /// </summary>
        private void ConfirmTeleport()
        {
            if (_isOnCooldown || !_isValidTeleportPoint)
                return; //could call even for failed teleport

            gameObject.transform.position = _avgTeleportPoint;

            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
            _teleportPositions.Clear();
            _isOnCooldown = true;

            Invoke(nameof(ResetCooldown), _teleportCooldown);

            //disabling sensor so you can't teleport multiple times
            switch (_hand)
            {
                case RhinoxHand.Left:
                    _sensorObjL.SetActive(false);
                    break;
                case RhinoxHand.Right:
                    _sensorObjR.SetActive(false);
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }
        }

        private void ResetCooldown()
        {
            _isOnCooldown = false;

            //enable sensor after cooldown
            switch (_hand)
            {
                case RhinoxHand.Left:
                    _sensorObjL.SetActive(true);
                    break;
                case RhinoxHand.Right:
                    _sensorObjR.SetActive(true);
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }
        }

        /// <summary>
        /// This coroutine starts the visualization of the teleportation arc. It waits for _visualStartDelay seconds
        /// before starting the arc, and then continuously draws the arc based on the position of the hand until it is
        /// disabled or the arc reaches its maximum distance.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartVisualCoroutine()
        {
            yield return new WaitForSeconds(_visualStartDelay);

            //if left hand is doing gesture than right hand sensor should be disabled and vice versa
            switch (_hand)
            {
                case RhinoxHand.Left:
                    _sensorObjL.SetActive(true);
                    _sensorObjR.SetActive(false);
                    break;
                case RhinoxHand.Right:
                    _sensorObjL.SetActive(false);
                    _sensorObjR.SetActive(true);
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Warn<GRPLITLogger>(
                        $"[{nameof(GRPLTeleport)}] {nameof(StartVisualCoroutine)}: invalid hand was given", this);
                    break;
            }

            ShowArc = true;
        }

        /// <summary>
        /// This coroutine stops the visualization of the teleportation arc. It waits for _visualStopDelay seconds
        /// before disabling the arc.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StopVisualCoroutine()
        {
            yield return new WaitForSeconds(_visualStopDelay);

            switch (_hand)
            {
                case RhinoxHand.Left:
                    _sensorObjL.SetActive(false);
                    break;
                case RhinoxHand.Right:
                    _sensorObjR.SetActive(false);
                    break;
                case RhinoxHand.Invalid:
                default:
                    PLog.Warn<GRPLITLogger>(
                        $"[{nameof(GRPLTeleport)}] {nameof(StopVisualCoroutine)}: invalid hand was given", this);
                    break;
            }

            _hand = RhinoxHand.Invalid;

            ShowArc = false;
        }

        /// <summary>
        /// This method is called when the <see cref="RhinoxGesture"/> associated with this teleporter is recognized. It
        /// initializes the teleportation process by setting _isEnabledL or _isEnabledR to true, depending on which hand
        /// is pointing, and starting a coroutine to draw the arc.
        /// </summary>
        /// <param name="hand">Which hand is triggered the teleport gesture</param>
        private void StartedPointing(RhinoxHand hand)
        {
            if (_hand == hand)
            {
                if (_stopVisualCoroutine != null)
                {
                    StopCoroutine(_stopVisualCoroutine);
                    _stopVisualCoroutine = null;
                }
            }
            else if (_hand == RhinoxHand.Invalid)
            {
                _hand = hand;
                _startVisualCoroutine = StartCoroutine(nameof(StartVisualCoroutine));
            }
        }

        /// <summary>
        /// This method is called when the <see cref="RhinoxGesture"/> associated with this teleporter is no longer
        /// recognized. It stops the visualization coroutine, and disables the teleportation process by setting
        /// _isEnabledL or _isEnabledR to false.
        /// </summary>
        /// <param name="hand">Which hand is triggered the teleport gesture</param>
        private void StoppedPointing(RhinoxHand hand)
        {
            if (ShowArc && _hand == hand)
                _stopVisualCoroutine = StartCoroutine(nameof(StopVisualCoroutine));
            else if (_hand == hand)
            {
                _hand = RhinoxHand.Invalid;
                if (_startVisualCoroutine != null)
                {
                    StopCoroutine(_startVisualCoroutine);
                    _startVisualCoroutine = null;
                }
            }
        }

        /// <summary>
        /// This method is called when the joint manager acquires tracking for a hand. If the hand is the left or right
        /// hand, and the corresponding sensor has not been set up yet, this method sets up the corresponding sensor.
        /// </summary>
        /// <param name="hand">The hand that acquired tracking.</param>
        private void TrackingAcquired(RhinoxHand hand) => SetHandEnabled(true, hand);

        /// <summary>
        /// This method is called when tracking is lost for a hand. If the hand is the left or right hand, and the
        /// corresponding sensor has been set up, this method disables the corresponding sensor.
        /// </summary>
        /// <param name="hand">The hand that lost tracking.</param>
        private void TrackingLost(RhinoxHand hand) => SetHandEnabled(false, hand);

        public void SetHandEnabled(bool newState, RhinoxHand handedness)
        {
            switch (handedness)
            {
                case RhinoxHand.Left:
                    _isEnabledL = newState;
                    break;
                case RhinoxHand.Right:
                    _isEnabledR = newState;
                    break;
            }
        }

        public bool IsHandEnabled(RhinoxHand handedness)
        {
            switch (handedness)
            {
                case RhinoxHand.Left:
                    return _isEnabledL;
                case RhinoxHand.Right:
                    return _isEnabledR;
                default:
                    return false;
            }
        }
    }
}