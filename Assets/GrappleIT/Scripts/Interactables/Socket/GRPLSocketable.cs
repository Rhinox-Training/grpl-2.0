using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This class inherits from the <see cref="GRPLGrabbableBase"/> class and adds the ability to have socket points for grabbing. 
    /// Each hand has its own socket, so the closest socket point get correctly orientated to the hands socket.
    /// </summary>
    /// <remarks>When the right hand grabs the object its orientation is mirrored compared to the left hand.</remarks>
    /// <dependencies />
    public class GRPLSocketable : GRPLGrabbableBase
    {
        /// <summary>
        /// The maximum distance from a socket point that a hand can grab the object.
        /// </summary>
        [Space(15f)]
        [SerializeField] private float _maxSocketDistance = .055f;

        /// <summary>
        /// A boolean toggle to show the grab range gizmo of each socket in the list of sockets.
        /// </summary>
#if UNITY_EDITOR
        [SerializeField] private bool _showSocketGrabRange = false;
        [Space(5f)]
#endif

        /// <summary>
        /// The list of socket points for the object.
        /// </summary>
        [SerializeField] private List<Transform> _sockets = null;

        /// <summary>
        /// The closest socket point to the left hand.
        /// </summary>
        private Transform _closestSocketL = null;

        /// <summary>
        /// The closest socket point to the right hand.
        /// </summary>
        private Transform _closestSocketR = null;

        /// <summary>
        /// The squared maximum distance from a socket point that a hand can grab the object.
        /// </summary>
        private float _maxSocketDistanceSqrd = 0f;

        /// <summary>
        /// Initializes the _maxSocketDistanceSqrd field and removes any null values from the _sockets list.
        /// </summary>
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
        /// <summary>
        /// Checks if a hand can interact with the object. If the hand is already holding the object, it returns
        /// whether the object is grabbed. Otherwise, it finds the closest socket point to the hand and sets a flag
        /// to allow the hand to grab the object if it is within range.
        /// </summary>
        /// <param name="joint">The interact joint.</param>
        /// <param name="hand">The hand on which the interact joint resides.</param>
        /// <returns></returns>
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

            float startingMaxDistanceSqrd = _maxSocketDistance * _maxSocketDistance;
            var closestSocket = _sockets.GetClosestTo(joint.JointPosition, null, ref startingMaxDistanceSqrd);

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

        /// <summary>
        /// Grabs the object with the specified hand. It finds the closest socket point to the hand and orients the
        /// object relative to the parent object.
        /// </summary>
        /// <param name="parent">The socket target in the hand.</param>
        /// <param name="rhinoxHand">The current hand.</param>
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

            transform.SetPositionAndRotation(Utility.GetMatrixPosition(targetMatrix),
                Utility.GetMatrixRotation(targetMatrix));
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