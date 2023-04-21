using System.Collections.Generic;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// The GRPLLeverBase class is an abstract class that extends from GRPLInteractable. <br />
    /// The class is used to derive lever-type interactable objects, and it includes additional methods and properties
    /// for that functionality.
    /// </summary>
    public abstract class GRPLLeverBase : GRPLInteractable
    {
        /// <summary>
        /// The transform of the base of the lever.
        /// </summary>
        [Space(5)]
        [Header("Lever parameters")]
        [SerializeField] protected Transform _baseTransform;
        /// <summary>
        /// The transform of the stem of the lever.
        /// </summary>
        [SerializeField] protected Transform _stemTransform;

        /// <summary>
        /// The transform of the handle of the lever.
        /// </summary>
        [SerializeField] protected Transform _handleTransform;
        /// <summary>
        /// The minimum angle at which the lever can be interacted with.
        /// </summary>
        [SerializeField] protected float _interactMinAngle = 90f;

        /// <summary>
        /// The maximum angle that the lever can be rotated to.
        /// </summary>
        [SerializeField] [Range(0, 360f)] protected float _leverMaxAngle = 180f;

        /// <summary>
        /// Determines whether the distance between the hand and the handle is considered when grabbing the lever.
        /// </summary>
        protected bool _ignoreDistanceOnGrab = true;

        /// <summary>
        /// The name of the gesture used for grabbing the lever.
        /// </summary>
        [Header("Grab parameters")] [SerializeField]
        protected string _grabGestureName = "Grab";

        /// <summary>
        /// The radius of the sphere used for detecting when the hand is close enough to the handle to grab it.
        /// </summary>
        [SerializeField] protected float _grabRadius = .1f;

        /// <summary>
        /// A reference to the global gesture recognizer.
        /// </summary>
        protected GRPLGestureRecognizer _gestureRecognizer;

        /// <summary>
        /// A reference to the joint that was last used to interact with the lever.
        /// </summary>
        protected RhinoxJoint _previousInteractJoint;

        /// <summary>
        /// The initial position of the handle.
        /// </summary>
        protected Vector3 _initialHandlePos;

        /// <summary>
        /// The initial rotation of the handle.
        /// </summary>
        protected Vector3 _initialHandleRot;

        //-----------------------
        // MONO BEHAVIOUR METHODS
        //-----------------------
        /// <summary>
        /// Clamps the interactMinAngle value to be within the range of 0 to the leverMaxAngle.
        /// </summary>
        private void OnValidate()
        {
            _interactMinAngle = Mathf.Clamp(_interactMinAngle, 0, _leverMaxAngle);
        }

        /// <summary>
        /// Initializes the class by setting the forced interactible joint and linking to the gesture recognizer.
        /// </summary>
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
        /// Saves a reference to the global gesture recognizer.
        /// </summary>
        /// <param name="gestureRecognizer"></param>
        private void OnGestureRecognizerGlobalInitialized(GRPLGestureRecognizer gestureRecognizer)
        {
            _gestureRecognizer = gestureRecognizer;
        }

        //-----------------------
        // INHERITABLE METHODS
        //-----------------------
        /// <summary>
        /// Calculates and returns the current rotation of the lever based on the projected position of the hand onto
        /// the plane defined by the base position and forward direction of the lever.
        /// </summary>
        /// <param name="projectedPos">The reference point projected on the plane defined by the lever base position and forward.</param>
        /// <returns>The angle in degrees.</returns>
        protected abstract float GetLeverRotation(Vector3 projectedPos);

        /// <summary>
        /// Returns the transform of the handle as the reference transform for the interactable object.
        /// </summary>
        /// <returns></returns>
        public override Transform GetReferenceTransform()
        {
            return _handleTransform;
        }

        /// <summary>
        /// Returns false and logs an error message indicating that this class does not implement the functionality of an interactable.
        /// This method should be overridden in derived classes to provide specific interaction logic.
        /// </summary>
        /// <param name="joint"></param>
        /// <param name="hand"></param>
        /// <returns></returns>
        public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
        {
            PLog.Info<GRPLITLogger>(
                "[GRPLLeverBase:CheckForInteraction], This is a GRPLLeverBase, which does not implement the functionality of an interactable. " +
                "Please derive from this class.");
            return false;
        }

        /// <summary>
        /// Returns false and logs an error message indicating that this class does not implement the functionality of an interactable.
        /// This method should be overridden in derived classes to provide specific interaction logic.
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="joint"></param>
        /// <param name="hand"></param>
        /// <returns></returns>
        public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint,
            RhinoxHand hand)
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
        /// <summary>
        /// Resets the transforms of the base, stem, and handle of the lever to default values.
        /// </summary>
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

        /// <summary>
        /// Draws the transforms of the lever in the Unity editor.
        /// </summary>
        protected void DrawLeverTransforms()
        {
            Handles.Label(_baseTransform.position, "Base");
            Gizmos.DrawSphere(_baseTransform.position, .01f);
            Handles.Label(_stemTransform.position, "Stem");
            Gizmos.DrawSphere(_stemTransform.position, .01f);
            Handles.Label(_handleTransform.position, "Handle");
            Gizmos.DrawSphere(_handleTransform.position, .01f);
        }
        /// <summary>
        /// Draws the grab range of the lever in the Unity editor.
        /// </summary>
        protected void DrawGrabRange()
        {
            using (new eUtility.GizmoColor(Color.magenta))
            {
                Gizmos.DrawSphere(GetReferenceTransform().position, _grabRadius);
            }
        }
#endif
    }
}