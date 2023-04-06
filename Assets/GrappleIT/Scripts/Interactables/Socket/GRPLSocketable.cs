using Rhinox.Lightspeed;
using Rhinox.Perceptor;
//using Rhinox.Perceptor;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using static PlasticPipe.PlasticProtocol.Client.ConnectionCreator.PlasticProtoSocketConnection;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLSocketable : GRPLGrabbableInteractable
    {

        [Space(15f)]
        [SerializeField] private float _maxSocketDistance = .025f;
        [SerializeField] private List<Transform> _sockets = null;

        private Transform _closestSocket = null;
        private float _maxSocketDistanceSqrd = 0f;

        protected override void Initialize()
        {
            base.Initialize();

            int tempCnt = _sockets.Count;
            _sockets.RemoveAll(s => s == null);
            if (_sockets.Count != tempCnt)
                PLog.Warn<GRPLITLogger>($"[GRPLSocketable:Initialize], " +
                    $"Socket list had empties and have been purged", this);

            _maxSocketDistanceSqrd = _maxSocketDistance * _maxSocketDistance;
        }

        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            //TODO: maybe bounding box optimazition for early return?
            _closestSocket = _sockets.GetClosestTo(joint.JointPosition, null, ref _maxSocketDistanceSqrd);

            switch (hand)
            {
                case RhinoxHand.Left:
                    _canHandGrabL = _closestSocket != null;
                    break;
                case RhinoxHand.Right:
                    _canHandGrabR = _closestSocket != null;
                    break;
                default:
                    break;
            }

            return IsGrabbed;
        }

        protected override void GrabInternal(GameObject parent, RhinoxHand rhinoxHand)
        {
            if (_closestSocket == null)
                return;

            base.GrabInternal(parent, rhinoxHand);

            Matrix4x4 relativeMatrix;

            //this extra code is only needed for right hand, otherwise the item will be flipped upside down.
            if (rhinoxHand == RhinoxHand.Right)
            {
                //because the right rhinoxHand is mirror of the left one.
                //the object needs to be flip around the X-axis to mirror it. 
                _closestSocket.localRotation = new Quaternion(_closestSocket.localRotation.x * -1.0f,
                                                              _closestSocket.localRotation.y,
                                                              _closestSocket.localRotation.z,
                                                              _closestSocket.localRotation.w * -1.0f);

                relativeMatrix = _closestSocket.GetMatrixRelativeTo(this.GetWorldMatrix());

                //is a component, so i can't take a copy of it, meaning i have to reset the orriginal back.
                _closestSocket.localRotation = new Quaternion(_closestSocket.localRotation.x * -1.0f,
                                                              _closestSocket.localRotation.y,
                                                              _closestSocket.localRotation.z,
                                                              _closestSocket.localRotation.w * -1.0f);
            }
            else
            {
                relativeMatrix = _closestSocket.GetMatrixRelativeTo(this.GetWorldMatrix());
            }

            var relativeInverse = relativeMatrix.inverse;
            var targetMatrix = parent.GetWorldMatrix() * relativeInverse;

            transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix), Utility.GetMatrixRotation(targetMatrix));
        }
    }
}