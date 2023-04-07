using System.Collections.Generic;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Rhinox.XR.Grapple.It
{
     /// <summary>
     /// The GRPLLeverBase class is an abstract class that extends from GRPLInteractable. <br /> <br />
     ///  The class is used to derive lever-type interactable objects, and it includes additional methods and properties for the purpose of that functionality.
     /// </summary>
    public abstract class GRPLLeverBase : GRPLInteractable
    {
        [Space(5)] [Header("Lever parameters")] [SerializeField]
        protected Transform _baseTransform;

        [SerializeField] protected Transform _stemTransform;
        [SerializeField] protected Transform _handleTransform;

        [SerializeField]
        protected float _interactMinAngle = 90f;

        [SerializeField] [Range(0, 360f)] protected float _leverMaxAngle = 180f;
        protected bool _ignoreDistanceOnGrab = true;

        [Header("Grab parameters")] [SerializeField]
        protected string _grabGestureName = "Grab";

        [SerializeField] protected float _grabRadius = .1f;

        protected GRPLGestureRecognizer _gestureRecognizer;
        protected RhinoxJoint _previousInteractJoint;

        protected Vector3 _initialHandlePos;
        protected Vector3 _initialHandleRot;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------

        private void OnValidate()
        {
            _interactMinAngle = Mathf.Clamp(_interactMinAngle, 0, _leverMaxAngle);
        }

        protected void Awake()
        {
            //Force the ForcedInteractJoint
            _forceInteractibleJoint = true;
            _forcedInteractJointID = XRHandJointID.Palm;

            // Link to gesture recognizer
            GRPLGestureRecognizer.GlobalInitialized += OnGestureRecognizerGlobalInitialized;
            
            Initialize();
        }

        //-----------------------
        // EVENT REACTIONS
        //-----------------------
        /// <summary>
        /// Event reaction to the globalInitialized event of the Gesture Recognizer.<br />
        /// Saves the recognizer in a private field.
        /// </summary>
        /// <param name="obj"></param>
        private void OnGestureRecognizerGlobalInitialized(GRPLGestureRecognizer obj)
        {
            _gestureRecognizer = obj;
        }
        //-----------------------
        // INHERITABLE METHODS
        //-----------------------
        /// <summary>
        /// Calculates the current rotation of the lever.
        /// </summary>
        /// <param name="projectedPos">The reference point projected on the plane defined by the lever base position and forward.</param>
        /// <returns>The angle in degrees.</returns>
        protected abstract float GetLeverRotation(Vector3 projectedPos);

        public override Vector3 GetReferencePoint()
        {
            return _handleTransform.position;
        }
        
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            PLog.Info<GRPLITLogger>("[GRPLLeverBase:CheckForInteraction], This is a GRPLLeverBase, which does not implement the functionality of an interactable. " +
                                    "Please derive from this class.");
            return false;
        }

        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint)
        {
            PLog.Info<GRPLITLogger>(
                "[GRPLLeverBase:CheckForInteraction], This is a GRPLLeverBase, which does not implement the functionality of an interactable. " +
                "Please derive from this class.");
            joint = null;
            return false;
        }

        //-----------------------
        // EDITOR ONLY METHODS
        //-----------------------
        #if UNITY_EDITOR
        private void Reset()
        {
            _baseTransform = new GameObject("Base").transform;
            _baseTransform.SetParent(this.transform);

            _baseTransform.localPosition = Vector3.zero;

            Transform baseVis = new GameObject("Base_Visuals").transform;
            baseVis.SetParent(_baseTransform.transform, false);

            _stemTransform = new GameObject("Stem").transform;
            _stemTransform.SetParent(_baseTransform, false);

            Transform stemVis = new GameObject("Stem_Visuals").transform;
            stemVis.SetParent(_stemTransform.transform, false);

            _handleTransform = new GameObject("Handle").transform;
            _handleTransform.SetParent(_stemTransform, false);

            var newLocalPos = Vector3.zero;
            newLocalPos.y += .5f;
            _handleTransform.localPosition = newLocalPos;

            Transform handleVis = new GameObject("Handle_Visuals").transform;
            handleVis.SetParent(_handleTransform.transform, false);
        }

        protected void DrawLeverTransforms()
        {
            Handles.Label(_baseTransform.position, "Base");
            Gizmos.DrawSphere(_baseTransform.position, .01f);
            Handles.Label(_stemTransform.position, "Stem");
            Gizmos.DrawSphere(_stemTransform.position, .01f);
            Handles.Label(_handleTransform.position, "Handle");
            Gizmos.DrawSphere(_handleTransform.position, .01f);
        }
        
        #endif
    }
}