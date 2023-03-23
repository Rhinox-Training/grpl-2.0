using Rhinox.Lightspeed.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace Rhinox.XR.Grapple.It
{
    /// The logic of how the teleport code works:
    /// When a hand makes the teleport gesture it will call the <see cref="StartVisualCoroutine"/> which starts a timer before actualy enabling the teleport logic
    ///     - If that hand stops making the teleport before the timer finishes the <see cref="StartVisualCoroutine"/> will be stopped
    ///     - If the other hand makes the teleport visual, nothing will happen.
    ///     - If the timer is complete, then the teleport arc will be calculated from the hand that made the gesture.
    ///     
    /// When the hand that the teleport arc is coming from stops making the teleport gesture the <see cref="StopVisualCoroutine"/> gets called, which starts a timer to stop the teleport logic
    ///     - If that hand starts making the gesture again before the timer finishes the <see cref="StopVisualCoroutine"/> will be stopped.
    ///     - If the other hand stops making the teleport visual, nothing will happen.
    ///     - If the timer is complete, then the teleport arc calculation will be stopped including it being visualized
    /// 
    /// When the the other hand enters the trigger on the wrist of the hand emmiting the teleport arc, the teleport location will be confirmed and the player gets teleported.
    /// This will put the teleport on cooldown, to prevent spamming/accidental teleporting.

    public class GRPLTeleport : MonoBehaviour
    {
        //[Header("Visual")]
        [SerializeField] private GameObject _sensorModel = null;

        [Header("Arc Visual Settings")]
        [SerializeField] public LineRenderer _lineRenderer = null;
        [SerializeField] public float _visualStartDelay = 0.3f;
        [SerializeField] public float _visualStopDelay = 0.5f;
        [SerializeField] public float _teleportCooldown = 1.5f;

        [Range(1, 10)]
        [SerializeField] private int _destinationSmoothingAmount = 5;//TODO: IMPLEMENT ACTUAL SMOOTHING

        [SerializeField] public float _maxDistance = 50f;
        [SerializeField] public float _lowestHeight = -50f;
        [SerializeField] public float _initialVelocity = 1f;
        private const float _gravity = -9.81f;

        [Range(0.001f, 2f)]
        [SerializeField] private float _lineSubIterations = 1f;

        //[Header("Fade Settings")]
        //[SerializeField] public float FadeDuration = .15f;
        //[Header("Snapping")]
        //public float SnapAmount = .15f;


        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Enable or disable the line rendering
        /// </summary>
        public bool ShowArc
        {
            get { return _lineRenderer.enabled; }
            set { _lineRenderer.enabled = value; }
        }

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;
        //private LineRenderer _lineRenderer = null;

        private RhinoxGesture _teleportGesture = null;
        private bool _isInitialized = false;

        private RhinoxHand _hand = RhinoxHand.Invalid;
        private GameObject _teleportZoneVisual = null;

        private Coroutine _startVisualCoroutine = null;
        private Coroutine _stopVisualCoroutine = null;

        private LimitedQueue<Vector3> _teleportPositions = new LimitedQueue<Vector3>(5);
        private GameObject _sensorObjL = null;
        private GameObject _sensorObjR = null;
        private GRPLProximitySensor _proxySensorL = null;
        private GRPLProximitySensor _proxySensorR = null;


        private Vector3 _sensorOffset = new Vector3(0f, 0f, -0.05f);
        private Vector3 _sensorSize = new Vector3(0.08f, 0.08f, 0.08f);

        private bool _isOnCooldown = false;

        private bool _isEnabledL = false;
        private bool _isEnabledR = false;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            if (_jointManager == null)
            {
                Debug.LogError($"{nameof(JointManager)} was NULL");
                return;
            }
            _gestureRecognizer = gestureRecognizer;
            if (_gestureRecognizer == null)
            {
                Debug.LogError($"{nameof(GestureRecognizer)} was NULL");
                return;
            }

            if (!TrySetupTeleportGesture())
            {
                Debug.LogError($"no \"Teleport\" gesture was found inside {nameof(GestureRecognizer)}");
                return;
            }

            //if (_lineRenderer == null)
            //{
            //    Debug.LogError($"Given {nameof(LineRenderer)} was NULL");
            //    return;
            //}

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;

            SensorSetup(out _sensorObjL, RhinoxHand.Left, out _proxySensorL);
            SensorSetup(out _sensorObjR, RhinoxHand.Right, out _proxySensorR);

            _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _teleportZoneVisual.transform.localScale = new Vector3(.5f, .1f, .5f);

            //_lineRenderer = gameObject.AddComponent<LineRenderer>();
            //_lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            //_lineRenderer.startColor = Color.green;
            //_lineRenderer.endColor = Color.green;
            //_lineRenderer.positionCount = 10;
            _lineRenderer.widthMultiplier = .05f;
            _lineRenderer.enabled = false;

            _teleportPositions.Limit = _destinationSmoothingAmount;

            _isInitialized = true;
        }

        private bool TrySetupTeleportGesture()
        {
            //getting the teleport gesture and linking events
            _teleportGesture = _gestureRecognizer.Gestures.Find(x => x.Name == "Teleport");
            if (_teleportGesture != null)
            {
                _teleportGesture.AddListenerOnRecognized(StartedPointing);
                _teleportGesture.AddListenerOnUnRecognized(StoppedPointing);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Intial setup function to create and place the sensor object and visual part correctly onto the given hand and setting up the events.
        /// </summary>
        /// <param name="sensorObj">The main object where the sensor script will be placed onto and the sensor model will be childed under</param>
        /// <param name="hand">Mainly to give the sensor <see cref="GameObject"/> the correct name</param>
        /// <param name="proxySensor">The sensor logic to hook into the events</param>
        private void SensorSetup(out GameObject sensorObj, RhinoxHand hand, out GRPLProximitySensor proxySensor)
        {
            sensorObj = new GameObject($"{hand}Hand Sensor");
            sensorObj.transform.parent = transform;
            sensorObj.SetActive(false);
            Instantiate(_sensorModel, _sensorOffset, Quaternion.identity, sensorObj.transform).transform.localScale = _sensorSize;

            var sensCollider = sensorObj.AddComponent<BoxCollider>();
            sensCollider.isTrigger = true;
            sensCollider.center = _sensorOffset;
            sensCollider.size = _sensorSize;
            proxySensor = sensorObj.AddComponent<GRPLProximitySensor>();
            proxySensor.HandLayer = LayerMask.NameToLayer("Hands");
            proxySensor.AddListenerOnSensorEnter(ConfirmTeleport);
        }

        private void OnEnable()
        {
            if (!_isInitialized)
                return;

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;

            _teleportGesture.AddListenerOnRecognized(StartedPointing);
            _teleportGesture.AddListenerOnUnRecognized(StoppedPointing);

            _proxySensorL.AddListenerOnSensorEnter(ConfirmTeleport);
            _proxySensorR.AddListenerOnSensorEnter(ConfirmTeleport);
        }

        private void OnDisable()
        {
            if (!_isInitialized)
                return;

            _teleportGesture.RemoveListenerOnRecognized(StartedPointing);
            _teleportGesture.RemoveListenerOnUnRecognized(StoppedPointing);

            _jointManager.TrackingAcquired -= TrackingAcquired;
            _jointManager.TrackingLost -= TrackingLost;

            _proxySensorL.RemoveListenerOnSensorEnter(ConfirmTeleport);
            _proxySensorR.RemoveListenerOnSensorEnter(ConfirmTeleport);

            StopAllCoroutines();
        }

        private void Update()
        {
            //update hantracked sensors to confirm teleportation
            if (_isEnabledL && _sensorObjL.activeSelf)
            {
                if (_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, RhinoxHand.Left, out var wrist))
                    _sensorObjL.transform.SetPositionAndRotation(wrist.JointPosition, wrist.JointRotation);
            }
            else if (_isEnabledR && _sensorObjR.activeSelf)
            {
                if (_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, RhinoxHand.Right, out var wrist))
                    _sensorObjR.transform.SetPositionAndRotation(wrist.JointPosition, wrist.JointRotation);
            }


            if (ShowArc && _hand != RhinoxHand.Invalid)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, _hand, out var wrist))
                    return;
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.MiddleMetacarpal, _hand, out var palm))
                    return;

                Ray ray = new Ray(wrist.JointPosition, (palm.JointPosition - wrist.JointPosition).normalized);

                CalculateTeleportLocation(ray);
            }
        }

        public void CalculateTeleportLocation(Ray startRay)
        {
            var aimPosition = startRay.origin;
            var aimDirection = startRay.direction * _initialVelocity;
            var rangeSquared = _maxDistance * _maxDistance;
            //calculate all points (logic based on the arc calculations from Meta)
            List<Vector3> points = new List<Vector3>();
            do
            {
                points.Add(aimPosition);

                var aimVector = aimDirection;
                aimVector.y = aimVector.y + _gravity * 0.0111111111f * _lineSubIterations;//0.0111111111f is a magic constant I got from ovr TeleportAimHandlerParabolic.cs 
                aimDirection = aimVector;
                aimPosition += aimVector;

            } while ((aimPosition.y - startRay.origin.y > _lowestHeight) && ((startRay.origin - aimPosition).sqrMagnitude <= rangeSquared));


            //calc ground intersection point
            Vector3 intersectPoint = Vector3.negativeInfinity;
            int indexNextPoint = 1;
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
            for (; indexNextPoint < points.Count; indexNextPoint++)
            {
                Ray currentRay = new Ray(points[indexNextPoint - 1], points[indexNextPoint] - points[indexNextPoint - 1]);

                if (Physics.Raycast(currentRay, out var hitInfo, Vector3.Distance(points[indexNextPoint], points[indexNextPoint - 1]), ~LayerMask.GetMask("Hands")))
                {
                    //intersectPoint = hitInfo.point;

                    _teleportPositions.Enqueue(new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z));

                    if (!_isOnCooldown)
                    {
                        _lineRenderer.startColor = Color.green;
                        _lineRenderer.endColor = Color.green;
                    }
                    break;
                }
            }


            Vector3 avgPos = new Vector3(_teleportPositions.Average(vec => vec.x),
                                         _teleportPositions.Average(vec => vec.y),
                                         _teleportPositions.Average(vec => vec.z));


            //Debug.DrawLine(points[indexNextPoint - 1], new Vector3(intersectPoint.x, _teleportZoneVisual.transform.position.y, intersectPoint.z), Color.cyan, 0f, false);
            _teleportZoneVisual.transform.position = new Vector3(avgPos.x, _teleportZoneVisual.transform.position.y, avgPos.z);

            //rendering part
            _lineRenderer.positionCount = indexNextPoint + 1;
            for (int index = 0; index < indexNextPoint + 1; index++)
            {
                _lineRenderer.SetPosition(index, points[index]);
            }
        }

        /// <summary>
        /// When called it will try and teleport the user to the location indicated by the teleport arc visual 
        /// </summary>
        private void ConfirmTeleport()
        {
            if (_isOnCooldown)
                return;

            gameObject.transform.position = new Vector3(_teleportZoneVisual.transform.position.x, gameObject.transform.position.y, _teleportZoneVisual.transform.position.z);

            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
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
        }

        #region Handtracking Specific Logic
        /// <summary>
        /// After the delay has passed when the function was first called it will:<br></br>
        /// - Enable/Disable the correct sensor of which arm the visual arc is coming from.<br></br>
        /// - Set the arc visual to visible.
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
                    Debug.LogWarning($"[{nameof(GRPLTeleport)}] {nameof(StartVisualCoroutine)}: invalid hand was given");
                    break;
            }

            ShowArc = true;
        }

        /// <summary>
        /// After the delay has passed when the function was first called it will:<br></br>
        /// - Disable the sensor of which arm the visual arc is coming from.<br></br>
        /// - Reset the current arc hand to invalid, indicating that no hand is trying to teleport.
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
                    Debug.LogWarning($"[{nameof(GRPLTeleport)}] {nameof(StopVisualCoroutine)}: invalid hand was given");
                    break;
            }

            _hand = RhinoxHand.Invalid;

            ShowArc = false;
        }

        /// <summary>
        /// Gets called when a hand makes the teleport gesture.<br></br>
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
        /// Gets called when a hand stops making the teleport gesture.<br></br>
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
        #endregion

        #region State Logic
        private void TrackingAcquired(RhinoxHand hand) => SetHandEnabled(true, hand);

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
        #endregion
    }
}