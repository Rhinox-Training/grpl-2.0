using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.XR;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLTeleport : MonoBehaviour
    {
        [Header("Pointing Visual")]
        public float PointingStartDelay = 0.5f;
        public float PointingStopDelay = 0.5f;
        public float ChargingTime = 2.0f;

        //[Space(2)]
        [Header("Fade Settings")]
        public float FadeDuration = .15f;
        //[Space(2)]
        //[Header("Snapping")]
        //public float SnapAmount = .15f;

        private float _timeLeft = 0f;

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;

        private LineRenderer _lineRenderer = null;

        private Hand _hand = Hand.Invalid;

        GameObject _teleportZoneVisual = null;

        private bool _isPointing = false;
        private bool _isCharging = false;

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            _gestureRecognizer = gestureRecognizer;

            _gestureRecognizer.OnGestureRecognized.AddListener(StartedPointing);
            _gestureRecognizer.OnGestureUnrecognized.AddListener(StoppedPointing);

            _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _teleportZoneVisual.transform.localScale = new(3f, .1f, 3f);

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.positionCount = 2;
            _lineRenderer.widthMultiplier = .05f;

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
                    _teleportZoneVisual.transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
                    _lineRenderer.enabled = true;
                    _lineRenderer.SetPosition(0, indexTip.JointPosition);
                    _lineRenderer.SetPosition(1, hitInfo.point);
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


        IEnumerator TryStartVisual()
        {
            yield return new WaitForSeconds(PointingStartDelay);

            _isCharging = true;
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
            if (/*_hand == Hand.Invalid &&*/ gestureName == "TeleportPoint")
            {
                _isPointing = true;
                _hand = hand;
                _startVisualCoroutine = StartCoroutine(nameof(TryStartVisual));
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