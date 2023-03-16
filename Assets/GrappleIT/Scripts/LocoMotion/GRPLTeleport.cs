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
        public float PointingVisualStartDelay = 1.5f;
        public float PointingVisualStopDelay = 1.5f;
        public float ChargingTime = 2.0f;

        //[Space(2)]
        [Header("Fade Settings")]
        public float FadeDuration = .5f;
        //[Space(2)]
        [Header("Snapping")]
        public float SnapAmount = .5f;

        private float _timeLeft = 0f;

        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;

        private LineRenderer _lineRenderer = null;


        GameObject _teleportZoneVisual = null;

        private bool _isPointing = false;
        private bool _showVisual = false;
        private bool _canteleport = false;
        private bool _isTryingToTeleport = false;

        enum TeleportationState
        {
            Nothing,
            StartedPoiting,
            StoppedPointing,
            ResumedPointing,
            StartedCharging,
            Charged,
        }

        private TeleportationState _currentState = TeleportationState.Nothing;
        private TeleportationState _previousState = TeleportationState.Nothing;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            _gestureRecognizer = gestureRecognizer;

            _gestureRecognizer.OnGestureRecognized.AddListener(IsStartingWithPointing);
            _gestureRecognizer.OnGestureUnrecognized.AddListener(IsStoppingWithPointing);

            _teleportZoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _teleportZoneVisual.transform.localScale = new(3f, .1f, 3f);

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.positionCount = 2;
            _lineRenderer.widthMultiplier = .05f;
        }

        private void Update()
        {
            switch (_currentState)
            {
                case TeleportationState.Nothing:
                    break;
                case TeleportationState.StartedPoiting:
                    if (_timeLeft > 0f)
                        _timeLeft -= Time.deltaTime;

                    if (_timeLeft <= 0f)
                    {
                        _showVisual = true;
                        _currentState = TeleportationState.StartedCharging;
                        _timeLeft = ChargingTime;
                    }
                    break;
                case TeleportationState.StoppedPointing:
                    break;
                case TeleportationState.StartedCharging:
                    if (_timeLeft > 0f)
                    {
                        _timeLeft -= Time.deltaTime;
                        var lineColor = Color.Lerp(Color.green, Color.cyan, _timeLeft / ChargingTime);
                        _lineRenderer.startColor = lineColor;
                        _lineRenderer.endColor = lineColor;
                    }

                    if (_timeLeft <= 0f)
                    {
                        _lineRenderer.startColor = Color.white;
                        _lineRenderer.endColor = Color.white;
                        _currentState = TeleportationState.Charged;
                        _canteleport = true;
                    }
                    break;
                case TeleportationState.Charged:
                    break;
            }

            _previousState = _currentState;


            if (_showVisual)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, RhinoxHand.Right, out var indexTip))
                    return;

                if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
                {
                    Debug.DrawLine(indexTip.JointPosition, indexTip.JointPosition + (indexTip.Forward * 100f), Color.cyan);
                    _teleportZoneVisual.transform.position = new Vector3(hitInfo.point.x, _teleportZoneVisual.transform.position.y, hitInfo.point.z);
                    _lineRenderer.SetPosition(0, indexTip.JointPosition);
                    _lineRenderer.SetPosition(1, hitInfo.point);
                }
            }

            if (_isTryingToTeleport)
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, RhinoxHand.Right, out var indexTip))
                    return;

                if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
                {
                    transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.y);
                }

                _isTryingToTeleport = false;
            }
        }

        void IsStartingWithPointing(RhinoxHand rhinoxHand, string gestureName)
        {
            if (rhinoxHand == RhinoxHand.Right && gestureName == "Pointing")
            {
                _isPointing = true;
                if (_previousState is TeleportationState.StartedCharging or TeleportationState.Charged)
                    _currentState = TeleportationState.ResumedPointing;
                else
                    _currentState = TeleportationState.StartedPoiting;
            }
            else if (rhinoxHand == RhinoxHand.Right && _currentState == TeleportationState.Charged
                && gestureName == "TeleportConfirm")
            {
                _isTryingToTeleport = true;
            }
        }


        void IsStoppingWithPointing(RhinoxHand rhinoxHand, string gestureName)
        {
            if (rhinoxHand == RhinoxHand.Right && gestureName == "Pointing")
            {
                _isPointing = false;
                _currentState = TeleportationState.StoppedPointing;
            }
        }

        void Teleport()
        {

        }
        //    if (rhinoxHand == RhinoxHand.Right && gestureName == "Teleport")
        //    {
        //        if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, RhinoxHand.Right, out var indexTip))
        //            return;

        //        if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
        //        {
        //            //transform.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.y);

        //        }
        //    }


        //}
    }
}