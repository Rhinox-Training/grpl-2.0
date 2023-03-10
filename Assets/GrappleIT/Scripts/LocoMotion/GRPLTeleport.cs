using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLTeleport : MonoBehaviour
    {
        private JointManager _jointManager = null;
        private GestureRecognizer _gestureRecognizer = null;

        public void Initialize(JointManager jointManager, GestureRecognizer gestureRecognizer)
        {
            _jointManager = jointManager;
            _gestureRecognizer = gestureRecognizer;


            _gestureRecognizer.OnGestureRecognized.AddListener(Teleport);
        }

        private void Update()
        {

        }

        void Teleport(Hand hand, string gestureName)
        {
            if (hand == Hand.Right && gestureName == "Teleport")
            {
                if (!_jointManager.TryGetJointFromHandById(UnityEngine.XR.Hands.XRHandJointID.IndexTip, Hand.Right, out var indexTip))
                    return;

                Debug.Log("\\o.o/");

                if (Physics.Raycast(indexTip.JointPosition, indexTip.Forward, out var hitInfo))
                {
                    transform.position = new Vector3(hitInfo.point.x, hitInfo.point.y, transform.position.z);

                }
            }
        }
    }
}