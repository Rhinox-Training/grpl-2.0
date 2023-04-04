using Codice.ThemeImages;
using Rhinox.Lightspeed;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
    public class GRPLGrabbableInteractable : GRPLInteractable
    {

        public bool IsGrabbed { get; protected set; }

        protected Transform _previousParentTransform = null;
        protected Rigidbody _rigidBody = null;

        protected bool _wasKinematic;
        protected bool _hadGravity;

        protected bool _isValid = true;

        private Bounds _bounds;
        private RhinoxGesture _grabGesture;

        //protected override void Start()
        //{
        //    base.Start();

        //    _bounds = gameObject.GetObjectBounds();

        //    if (TryGetComponent(out _rigidBody))
        //    {
        //        _wasKinematic = _rigidBody.isKinematic;
        //        _hadGravity = _rigidBody.useGravity;
        //        _previousParentTransform = transform.parent;
        //    }
        //    else
        //        _isValid = false;
        //}

        protected override void OnEnable()
        {
            base.OnEnable();


        }

        protected override void OnDisable()
        {
            base.OnDisable();


        }

        protected override void Initialize()
        {
            base.Initialize();

            _bounds = gameObject.GetObjectBounds();

            _bounds.extents *= 1.1f;//increase bouds a bit with magic number

            if (TryGetComponent(out _rigidBody))
            {
                _wasKinematic = _rigidBody.isKinematic;
                _hadGravity = _rigidBody.useGravity;
                _previousParentTransform = transform.parent;
            }
            else
                _isValid = false;
        }

        private void GetGesture()
        {

            //getting the grab gesture and linking events
            if (_grabGesture == null)
            {
                _grabGesture = _gestureRecognizer.Gestures.Find(x => x.Name == "Grab");
                if (_grabGesture != null)
                {
                    _grabGesture.AddListenerOnRecognized(TryGrab);// .OnRecognized.AddListener(TryGrab);
                    _grabGesture.AddListenerOnUnRecognized(TryDrop);// .OnUnrecognized.AddListener(TryDrop);
                }
            }
        }

        private void Update()
        {
            _bounds.center = transform.position;
        }

        //save and change the rigidbody settings so it can properly move along with the handand it is now attached to
        public virtual void Grabbed(GameObject parent, RhinoxHand rhinoxHand = RhinoxHand.Invalid)
        {
            if (!_isValid)
                return;

            _wasKinematic = _rigidBody.isKinematic;
            _hadGravity = _rigidBody.useGravity;

            _rigidBody.isKinematic = true;
            _rigidBody.useGravity = false;
            _previousParentTransform = transform.parent;

            gameObject.transform.parent = parent.transform;
        }

        //reinstate the changed rigidbody settings
        public virtual void Dropped()
        {
            if (!_isValid)
                return;

            _rigidBody.isKinematic = _wasKinematic;
            _rigidBody.useGravity = _hadGravity;

            gameObject.transform.parent = _previousParentTransform;
        }

        public override bool CheckForInteraction(RhinoxJoint joint)
        {
            if (_bounds.Contains(joint.JointPosition))
            {
                //Grabbed(joint.JointPosition.);

                _wasKinematic = _rigidBody.isKinematic;
                _hadGravity = _rigidBody.useGravity;

                _rigidBody.isKinematic = true;
                _rigidBody.useGravity = false;
                _previousParentTransform = transform.parent;

                return true;
            }
            else
            {
                Dropped();
                return false;
            }
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint outJoint)
        {
            outJoint = joints.FirstOrDefault(x => x.JointID == XRHandJointID.MiddleMetacarpal);
            return outJoint != null;





            //throw new System.NotImplementedException();
            //joint = null;
            //return false;

            //outJoint = null;
            //float closestDist = float.MaxValue;


            //foreach (var joint in joints)
            //{
            //    if (joint.JointID == XRHandJointID.Palm)
            //    {
            //        outJoint = joint;
            //        return true;
            //    }
            //}

            //var normalPos = ButtonBaseTransform.position;
            //var normal = ButtonBaseTransform.forward;
            //foreach (var joint in joints)
            //{
            //    if (!InteractableMathUtils.IsPlaneProjectedPointInBounds(joint.JointPosition,
            //            normalPos, normal, PressBounds))
            //        continue;

            //    var distance =


            //        InteractableMathUtils.GetProjectedDistanceFromPointOnNormal(joint.JointPosition, normalPos, normal);
            //    if (distance < closestDist)
            //    {
            //        outJoint = joint;
            //        closestDist = distance;
            //    }
            //}

            //return outJoint != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
        }
    }
}
