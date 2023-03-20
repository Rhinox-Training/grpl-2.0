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
        public int Smoothing = 5;

        public float TeleportTreshold = 0.75f;

        //[Space(2)]
        [Header("Fade Settings")]
        public float FadeDuration = .15f;
        //[Space(2)]
        //[Header("Snapping")]
        //public float SnapAmount = .15f;

        public bool IsInitialized => _isInitialized;

        private Vector3 _teleportPos = Vector3.zero;

        private LimitedQueue<Vector3> _teleportPositions = new LimitedQueue<Vector3>(5);

        private float _timeLeft = 0f;

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;

        private LineRenderer _lineRenderer = null;

        private Hand _hand = Hand.Invalid;

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
            _teleportZoneVisual.transform.localScale = new Vector3(3f, .1f, 3f);

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.positionCount = 2;
            _lineRenderer.widthMultiplier = .05f;

            _teleportPositions.Limit = Smoothing;

            _isInitialized = true;
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
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, _hand, out var indexTip))
                    return;

                if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
                {
                    _teleportPositions.Enqueue(new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z));

                    Vector3 avgPos = new Vector3(_teleportPositions.Average(vec => vec.x),
                                                 _teleportPositions.Average(vec => vec.y),
                                                 _teleportPositions.Average(vec => vec.z));

                    //_teleportZoneVisual.transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
                    _teleportZoneVisual.transform.position = avgPos;

                    _lineRenderer.SetPosition(0, indexTip.JointPosition);
                    //_lineRenderer.SetPosition(1, hitInfo.point);
                    _lineRenderer.SetPosition(1, avgPos);
                    _lineRenderer.enabled = true;


                    if (Mathf.Abs(hitInfo.point.x - avgPos.x) <= TeleportTreshold ||
                        Mathf.Abs(hitInfo.point.z - avgPos.z) <= TeleportTreshold)
                    {


                        _timeLeft -= Time.deltaTime;
                        if (_timeLeft <= 0f)
                        {
                            _isCharging = false;

                            _lineRenderer.enabled = false;
                            Teleport(avgPos);
                        }
                        else
                        {
                            _lineRenderer.startColor = Color.Lerp(Color.blue, Color.green, _timeLeft / ChargingTime);
                            _lineRenderer.endColor = Color.Lerp(Color.blue, Color.green, _timeLeft / ChargingTime);
                            _lineRenderer.widthMultiplier = Mathf.Lerp(0.25f, 0f, _timeLeft / ChargingTime);//change magic numbers to vars
                        }
                    }
                    else
                    {
                        _timeLeft = ChargingTime;
                        _lineRenderer.startColor = Color.green;
                        _lineRenderer.endColor = Color.green;
                        _lineRenderer.widthMultiplier = 0f;
                    }
                }
                else
                {
                    _lineRenderer.enabled = false;
                }
            }




            #region OLDBADSTUFF
            //switch (_currentState)
            //{
            //    case TeleportationState.Nothing:
            //        break;
            //    case TeleportationState.StartedPoiting:
            //        if (_timeLeft > 0f)
            //            _timeLeft -= Time.deltaTime;

            //        if (_timeLeft <= 0f)
            //        {
            //            _showVisual = true;
            //            _currentState = TeleportationState.StartedCharging;
            //            _timeLeft = ChargingTime;
            //        }
            //        break;
            //    case TeleportationState.StoppedPointing:
            //        break;
            //    case TeleportationState.StartedCharging:
            //        if (_timeLeft > 0f)
            //        {
            //            _timeLeft -= Time.deltaTime;
            //            var lineColor = Color.Lerp(Color.green, Color.cyan, _timeLeft / ChargingTime);
            //            _lineRenderer.startColor = lineColor;
            //            _lineRenderer.endColor = lineColor;
            //        }

            //        if (_timeLeft <= 0f)
            //        {
            //            _lineRenderer.startColor = Color.white;
            //            _lineRenderer.endColor = Color.white;
            //            _currentState = TeleportationState.Charged;
            //            _canteleport = true;
            //        }
            //        break;
            //    case TeleportationState.Charged:
            //        break;
            //}

            //_previousState = _currentState;


            //if (_showVisual)
            //{
            //    if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, Hand.Right, out var indexTip))
            //        return;

            //    if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
            //    {
            //        Debug.DrawLine(indexTip.JointPosition, indexTip.JointPosition + (indexTip.Forward * 100f), Color.cyan);
            //        _teleportZoneVisual.transform.position = new Vector3(hitInfo.point.x, _teleportZoneVisual.transform.position.y, hitInfo.point.z);
            //        _lineRenderer.SetPosition(0, indexTip.JointPosition);
            //        _lineRenderer.SetPosition(1, hitInfo.point);
            //    }
            //}

            //if (_isTryingToTeleport)
            //{
            //    if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, Hand.Right, out var indexTip))
            //        return;

            //    if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
            //    {
            //        transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.y);
            //    }

            //    _isTryingToTeleport = false;
            //}
            #endregion
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
        }

        IEnumerator TryStopVisual()
        {
            yield return new WaitForSeconds(PointingStopDelay);

            _lineRenderer.enabled = false;
            _isCharging = false;
        }

        private Coroutine _startVisualCoroutine = null;
        private Coroutine _stopVisualCoroutine = null;

        void StartedPointing(Hand hand, string gestureName)
        {
            if ((_hand == Hand.Invalid || _hand == hand) && gestureName == "TeleportPoint")
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

        void StoppedPointing(Hand hand, string gestureName)
        {
            if (_hand == hand && gestureName == "TeleportPoint")
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
                    _hand = Hand.Invalid;
                }
            }
        }
    }
}