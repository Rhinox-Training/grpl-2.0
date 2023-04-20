using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// Socktables objects inhert from <see cref="GRPLGrabbableBase"/> and extend this by adding sockets points.<br />
    /// Each hand has its own socket, so the closest socket point get correctly orientated to the hands socket.
    /// </summary>
    /// <remarks>When the right hand grabs the object its orientation is mirrored compared to the left hand.</remarks>
    /// <dependencies />
    public class GRPLSocketable : GRPLGrabbableBase
    {
        [Header("Socket Settings")]
        [SerializeField] private float _maxSocketDistance = .055f;
        [SerializeField] private bool _showSocketGrabRange = false;
        [Space(5f)]
        [SerializeField] private List<Transform> _sockets = null;

        private Transform _closestSocketL = null;
        private Transform _closestSocketR = null;
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

        //TODO: maybe bounding box optimazition for early return?
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            if (hand == _currentHandHolding)
            {
                switch (hand)
                {
                    case RhinoxHand.Left:
                        _canHandGrabL = false;
                        break;
                    case RhinoxHand.Right:
                        _canHandGrabR = false;
                        break;
                    default:
                        break;
                }

                return IsGrabbed;
            }


            //Predicate<Transform> predicate = (trans) => { return (joint.JointPosition - trans.position).sqrMagnitude <= _maxSocketDistanceSqrd; };
            //_closestSocket = _sockets.GetClosestTo(joint.JointPosition, predicate);

            //ask Jorian/Gaetan what to do with this
            var closestSocket = _sockets.GetClosestTo(joint.JointPosition, null, ref _maxSocketDistanceSqrd);
            _maxSocketDistanceSqrd = _maxSocketDistance * _maxSocketDistance;

            switch (hand)
            {
                case RhinoxHand.Left:
                    if (closestSocket != null)
                    {
                        _canHandGrabL = true;
                        _closestSocketL = closestSocket;
                    }
                    else
                    {
                        _canHandGrabL = false;
                        _closestSocketL = null;
                    }
                    break;
                case RhinoxHand.Right:
                    if (closestSocket != null)
                    {
                        _canHandGrabR = true;
                        _closestSocketR = closestSocket;
                    }
                    else
                    {
                        _canHandGrabR = false;
                        _closestSocketR = null;
                    }
                    break;
                default:
                    break;
            }

            return IsGrabbed;
        }

        protected override void GrabInternal(GameObject parent, RhinoxHand rhinoxHand)
        {
            Transform _closestSocket = null;

            switch (rhinoxHand)
            {
                case RhinoxHand.Left:
                    _closestSocket = _closestSocketL;
                    break;
                case RhinoxHand.Right:
                    _closestSocket = _closestSocketR;
                    break;
            }

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

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (_showSocketGrabRange)
            {
                foreach (var socket in _sockets)
                {
                    Gizmos.DrawWireSphere(socket.transform.position, _maxSocketDistance);
                }
            }
        }
#endif
    }
}