using Rhinox.Lightspeed.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;
//using System.Threading;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.Events;
//using UnityEngine.Experimental.GlobalIllumination;
//using UnityEngine.XR;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLTeleport : MonoBehaviour
    {
        [Header("Pointing Visual")]
        public float PointingStartDelay = 0.5f;
        public float PointingStopDelay = 0.5f;
        public float ChargingTime = 2.0f;

        [Range(1, 10)]
        [SerializeField] private int _smoothingAmount = 5;

        public float TeleportTreshold = 0.75f;

        //[Space(2)]
        [Header("Fade Settings")]
        public float FadeDuration = .15f;
        //[Space(2)]
        //[Header("Snapping")]
        //public float SnapAmount = .15f;

        public bool IsInitialized => _isInitialized;


        public float _maxDistance = 50f;
        public float _lowestHeight = -50f;
        public float _initialVelocity = 1f;
        private const float _gravity = -9.81f;

        [Range(0.001f, 2f)]
        [SerializeField] private float _lineSubIterations = 1f;


        private Vector3 _teleportPos = Vector3.zero;

        private LimitedQueue<Vector3> _teleportPositions = new LimitedQueue<Vector3>(5);

        private float _timeLeft = 0f;

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;

        private LineRenderer _lineRenderer = null;

        private RhinoxHand _hand = RhinoxHand.Invalid;

        GameObject _teleportZoneVisual = null;

        private bool _isPointing = false;
        private bool _isCharging = false;

        private bool _isInitialized = false;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            _gestureRecognizer = gestureRecognizer;

            _gestureRecognizer.OnGestureRecognized.AddListener(StartedPointing);
            _gestureRecognizer.OnGestureUnrecognized.AddListener(StoppedPointing);

            _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _teleportZoneVisual.transform.localScale = new Vector3(.5f, .1f, .5f);

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.positionCount = 10;
            _lineRenderer.widthMultiplier = .05f;

            _teleportPositions.Limit = _smoothingAmount;

            _isInitialized = true;

            //calculate points
            //CalculateTeleportLocation();


            //for (int indexNextPoint = 0; indexNextPoint < 10; indexNextPoint++)
            //{
            //    //_lineRenderer.SetPosition(indexNextPoint, new Vector3(starpos.x, Mathf.Pow(step * (indexNextPoint + 1), 2), step * (indexNextPoint + 1)));
            //    _lineRenderer.SetPosition(indexNextPoint, new Vector3(starpos.x, Mathf.Pow(step * (indexNextPoint + 1), 2), step * (indexNextPoint + 1)));
            //}

            _lineRenderer.enabled = false;
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
            Vector3 intersectPoint = Vector3.zero;
            int indexNextPoint = 1;
            for (; indexNextPoint < points.Count; indexNextPoint++)
            {
                Ray currentRay = new Ray(points[indexNextPoint - 1], points[indexNextPoint] - points[indexNextPoint - 1]);

                if (Physics.Raycast(currentRay, out var hitInfo, Vector3.Distance(points[indexNextPoint], points[indexNextPoint - 1]), ~LayerMask.GetMask("Hands")))
                {
                    intersectPoint = hitInfo.point;
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

            StopAllCoroutines();
        }

        private void Update()
        {
            if (_isCharging)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.Wrist, _hand, out var wrist))
                    return;
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.MiddleMetacarpal, _hand, out var palm))
                    return;

                Debug.DrawLine(wrist.JointPosition, palm.JointPosition, Color.magenta, 0f, false);

                Ray ray = new Ray(wrist.JointPosition, (palm.JointPosition - wrist.JointPosition).normalized);

                CalculateTeleportLocation(ray);

                //joint.Forward
                //Debug.Log(joint.JointRotation.eulerAngles);
                //Quaternion.

                //if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, _hand, out var indexTip))
                //return;

                //if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
                //{
                //    _teleportPositions.Enqueue(new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z));

                //    Vector3 avgPos = new Vector3(_teleportPositions.Average(vec => vec.x),
                //                                 _teleportPositions.Average(vec => vec.y),
                //                                 _teleportPositions.Average(vec => vec.z));

                //    //_teleportZoneVisual.transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
                //    _teleportZoneVisual.transform.position = avgPos;

                //    _lineRenderer.SetPosition(0, indexTip.JointPosition);
                //    //_lineRenderer.SetPosition(1, hitInfo.point);
                //    _lineRenderer.SetPosition(1, avgPos);
                //    _lineRenderer.enabled = true;


                //    if (Mathf.Abs(hitInfo.point.x - avgPos.x) <= TeleportTreshold ||
                //        Mathf.Abs(hitInfo.point.z - avgPos.z) <= TeleportTreshold)
                //    {


                //        _timeLeft -= Time.deltaTime;
                //        if (_timeLeft <= 0f)
                //        {
                //            _isCharging = false;

                //            _lineRenderer.enabled = false;
                //            Teleport(avgPos);
                //        }
                //        else
                //        {
                //            _lineRenderer.startColor = Color.Lerp(Color.blue, Color.green, _timeLeft / ChargingTime);
                //            _lineRenderer.endColor = Color.Lerp(Color.blue, Color.green, _timeLeft / ChargingTime);
                //            _lineRenderer.widthMultiplier = Mathf.Lerp(0.25f, 0f, _timeLeft / ChargingTime);//change magic numbers to vars
                //        }
                //    }
                //    else
                //    {
                //        _timeLeft = ChargingTime;
                //        _lineRenderer.startColor = Color.green;
                //        _lineRenderer.endColor = Color.green;
                //        _lineRenderer.widthMultiplier = 0f;
                //    }
                //}
                //else
                //{
                //    _lineRenderer.enabled = false;
                //}
            }
        }

        void Teleport(Vector3 position)
        {
            gameObject.transform.position = new Vector3(position.x, gameObject.transform.position.y, position.z);
        }

        IEnumerator TryStartVisual()
        {
            yield return new WaitForSeconds(PointingStartDelay);

            _isCharging = true;
            _timeLeft = ChargingTime;
            _lineRenderer.enabled = true;
        }

        IEnumerator TryStopVisual()
        {
            yield return new WaitForSeconds(PointingStopDelay);

            _lineRenderer.enabled = false;
            _isCharging = false;
            _lineRenderer.enabled = false;
        }

        private Coroutine _startVisualCoroutine = null;
        private Coroutine _stopVisualCoroutine = null;

        void StartedPointing(RhinoxHand hand, string gestureName)
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
                }
            }
        }

        void StoppedPointing(RhinoxHand hand, string gestureName)
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
                }
            }
        }
    }
}