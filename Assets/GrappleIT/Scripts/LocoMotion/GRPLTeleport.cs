using Rhinox.Lightspeed.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLTeleport : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private GameObject _sensorModel = null;

        //[Header("Pointing Visual")]
        [SerializeField] public float _pointingStartDelay = 0.5f;
        [SerializeField] public float _pointingStopDelay = 0.5f;
        //[SerializeField] public float _chargingTime = 2.0f;

        [Range(1, 10)]
        [SerializeField] private int _smoothingAmount = 5;

        //public float TeleportTreshold = 0.75f;

        //[Header("Fade Settings")]
        //[SerializeField] public float FadeDuration = .15f;
        //[Header("Snapping")]
        //public float SnapAmount = .15f;



        public bool IsInitialized => _isInitialized;

        [Header("Arc Visual Settings")]
        [SerializeField] public float _maxDistance = 50f;
        [SerializeField] public float _lowestHeight = -50f;
        [SerializeField] public float _initialVelocity = 1f;
        private const float _gravity = -9.81f;

        [Range(0.001f, 2f)]
        [SerializeField] private float _lineSubIterations = 1f;


        private LimitedQueue<Vector3> _teleportPositions = new LimitedQueue<Vector3>(5);

        private float _timeLeft = 0f;

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;
        private LineRenderer _lineRenderer = null;
        private bool _isInitialized = false;
        public bool Initialized => _isInitialized;
        private RhinoxHand _hand = RhinoxHand.Invalid;
        private GameObject _teleportZoneVisual = null;

        private GameObject _leftHandSensor = null;
        private GameObject _rightHandSensor = null;

        private Vector3 _sensorOffset = new Vector3(0f, -0.02f, -0.04f); //sensColliderL.center = new Vector3(0f, -0.03f, -0.025f);
        private Vector3 _sensorSize = new Vector3(0.04f, 0.03f, 0.04f);//sensColliderL.size = new Vector3(0.05f, 0.05f, 0.05f);

        private bool _isPointing = false;
        private bool _isCharging = false;

        private bool _isEnabledL = false;
        private bool _isEnabledR = false;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            if (_jointManager == null)
            {
                Debug.LogError($"{nameof(JointManager)} was NULL");
                _isInitialized = false;
                return;
            }
            _gestureRecognizer = gestureRecognizer;
            if (_gestureRecognizer == null)
            {
                Debug.LogError($"{nameof(GestureRecognizer)} was NULL");
                _isInitialized = false;
                return;
            }

            _gestureRecognizer.OnGestureRecognized.AddListener(StartedPointing);
            _gestureRecognizer.OnGestureUnrecognized.AddListener(StoppedPointing);

            _jointManager.TrackingAcquired += TrackingAcquired;
            _jointManager.TrackingLost += TrackingLost;

            #region Sensor SetupS
            //Left hand sensor
            _leftHandSensor = new GameObject("LeftHand Sensor");
            _leftHandSensor.transform.parent = transform;
            _leftHandSensor.SetActive(false);
            Instantiate(_sensorModel, _sensorOffset, Quaternion.identity, _leftHandSensor.transform).transform.localScale = _sensorSize;

            var sensColliderL = _leftHandSensor.AddComponent<BoxCollider>();
            sensColliderL.isTrigger = true;
            sensColliderL.center = _sensorOffset;
            sensColliderL.size = _sensorSize;
            var proxSensL = _leftHandSensor.AddComponent<GRPLProximitySensor>();
            proxSensL.HandLayer = LayerMask.NameToLayer("Hands");
            proxSensL.OnSensorEnter.AddListener(TryTeleport);

            //Right hand sensor
            _rightHandSensor = new GameObject("RightHand Sensor");
            _rightHandSensor.transform.parent = transform;
            _rightHandSensor.SetActive(false);
            Instantiate(_sensorModel, _sensorOffset, Quaternion.identity, _rightHandSensor.transform).transform.localScale = _sensorSize;

            var sensColliderR = _rightHandSensor.AddComponent<BoxCollider>();
            sensColliderR.isTrigger = true;
            sensColliderR.center = _sensorOffset;
            sensColliderR.size = _sensorSize;
            var proxSensR = _rightHandSensor.AddComponent<GRPLProximitySensor>();
            proxSensR.HandLayer = LayerMask.NameToLayer("Hands");
            proxSensR.OnSensorEnter.AddListener(TryTeleport);
            #endregion





            _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _teleportZoneVisual.transform.localScale = new Vector3(.5f, .1f, .5f);

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.positionCount = 10;
            _lineRenderer.widthMultiplier = .05f;
            _lineRenderer.enabled = false;

            _teleportPositions.Limit = _smoothingAmount;

            _isInitialized = true;
        }

        private void CalculateTeleportLocation(Ray startRay)
        {
            var aimPosition = startRay.origin;
            var aimDirection = startRay.direction * _initialVelocity;
            var rangeSquared = _maxDistance * _maxDistance;
            //calculate all points
            List<Vector3> points = new List<Vector3>();
            do
            {
                points.Add(aimPosition);

                var aimVector = aimDirection;
                aimVector.y = aimVector.y + _gravity * 0.0111111111f * _lineSubIterations;//0.0111111111f is constant value i got from ovr TeleportAimHandlerParabolic.cs 
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
                    intersectPoint = hitInfo.point;
                    _lineRenderer.startColor = Color.green;
                    _lineRenderer.endColor = Color.green;
                    break;
                }
            }

            Debug.DrawLine(points[indexNextPoint - 1], new Vector3(intersectPoint.x, _teleportZoneVisual.transform.position.y, intersectPoint.z), Color.cyan, 0f, false);
            _teleportZoneVisual.transform.position = new Vector3(intersectPoint.x, _teleportZoneVisual.transform.position.y, intersectPoint.z);

            //rendering part
            _lineRenderer.positionCount = indexNextPoint + 1;
            for (int index = 0; index < indexNextPoint + 1; index++)
            {
                _lineRenderer.SetPosition(index, points[index]);
            }
        }

        private void OnEnable()
        {
            if (_isInitialized || _gestureRecognizer == null)
                return;

            _gestureRecognizer.OnGestureRecognized.AddListener(StartedPointing);
            _gestureRecognizer.OnGestureUnrecognized.AddListener(StoppedPointing);
        }

        private void OnDisable()
        {
            if (!_isInitialized)
                return;

            _gestureRecognizer.OnGestureRecognized.RemoveListener(StartedPointing);
            _gestureRecognizer.OnGestureUnrecognized.RemoveListener(StoppedPointing);

            _jointManager.TrackingAcquired -= TrackingAcquired;
            _jointManager.TrackingLost -= TrackingLost;

            StopAllCoroutines();
        }

        private void Update()
        {
            if (_isEnabledL && _leftHandSensor.activeSelf)
            {
                if (_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, RhinoxHand.Left, out var wrist))
                    _leftHandSensor.transform.SetPositionAndRotation(wrist.JointPosition, wrist.JointRotation);
            }

            if (_isEnabledR && _rightHandSensor.activeSelf)
            {
                if (_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, RhinoxHand.Right, out var wrist))
                    _rightHandSensor.transform.SetPositionAndRotation(wrist.JointPosition, wrist.JointRotation);
            }


            if (_isCharging)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, _hand, out var wrist))
                    return;
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.MiddleMetacarpal, _hand, out var palm))
                    return;

                Debug.DrawLine(wrist.JointPosition, palm.JointPosition, Color.magenta, 0f, false);

                Ray ray = new Ray(wrist.JointPosition, (palm.JointPosition - wrist.JointPosition).normalized);

                CalculateTeleportLocation(ray);
            }
        }

        private void TryTeleport()
        {
            gameObject.transform.position = new Vector3(_teleportZoneVisual.transform.position.x, gameObject.transform.position.y, _teleportZoneVisual.transform.position.z);

            //disabling sensor so you can't teleport multiple times
            switch (_hand)
            {
                case RhinoxHand.Left:
                    _leftHandSensor.SetActive(false);
                    break;
                case RhinoxHand.Right:
                    _rightHandSensor.SetActive(false);
                    break;
                case RhinoxHand.Invalid:
                default:
                    break;
            }

            //Debug.Log("\\._./ SCHMOVE");
        }

        //private void Teleport(Vector3 position)
        //{
        //    gameObject.transform.position = new Vector3(position.x, gameObject.transform.position.y, position.z);
        //}

        private IEnumerator TryStartVisual()
        {
            yield return new WaitForSeconds(_pointingStartDelay);

            _isCharging = true;
            //_timeLeft = _chargingTime;
            _lineRenderer.enabled = true;
        }

        IEnumerator TryStopVisual()
        {
            yield return new WaitForSeconds(_pointingStopDelay);

            _lineRenderer.enabled = false;
            _isCharging = false;
            _lineRenderer.enabled = false;
        }

        private Coroutine _startVisualCoroutine = null;
        private Coroutine _stopVisualCoroutine = null;

        private void StartedPointing(RhinoxHand hand, string gestureName)
        {
            if ((_hand == RhinoxHand.Invalid || _hand == hand) && gestureName == "Teleport")
            {
                _isPointing = true;
                if (_isCharging)
                {
                    StopCoroutine(_stopVisualCoroutine);
                    _stopVisualCoroutine = null;
                }
                else
                {
                    _hand = hand;
                    _startVisualCoroutine = StartCoroutine(nameof(TryStartVisual));

                    switch (hand)
                    {
                        case RhinoxHand.Left:
                            _leftHandSensor.SetActive(true);
                            break;
                        case RhinoxHand.Right:
                            _rightHandSensor.SetActive(true);
                            break;
                        case RhinoxHand.Invalid:
                        default:
                            Debug.LogWarning($"[{nameof(GRPLTeleport)}] StartedPointing: invalid hand was given");
                            break;
                    }
                }
            }
        }

        private void StoppedPointing(RhinoxHand hand, string gestureName)
        {
            if (_hand == hand && gestureName == "Teleport")
            {
                _isPointing = false;

                if (_isCharging)
                {
                    _stopVisualCoroutine = StartCoroutine(nameof(TryStopVisual));
                }
                else
                {
                    StopCoroutine(_startVisualCoroutine);
                    _startVisualCoroutine = null;
                    _hand = RhinoxHand.Invalid;

                    switch (hand)
                    {
                        case RhinoxHand.Left:
                            _leftHandSensor.SetActive(false);
                            break;
                        case RhinoxHand.Right:
                            _rightHandSensor.SetActive(false);
                            break;
                        case RhinoxHand.Invalid:
                        default:
                            Debug.LogWarning($"[{nameof(GRPLTeleport)}] StoppedPointing: invalid hand was given");
                            break;
                    }
                }
            }
        }


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