using System.Collections.Generic;
using UnityEngine.XR.Hands;
using Rhinox.XR.Grapple.It;
using Rhinox.XR.Grapple;
using System.Linq;
using UnityEngine;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

public class GRPLLever : GRPLInteractable
{
    [Space(5)] [Header("Lever parameters")] [SerializeField]
    private Transform _baseTransform;

    [SerializeField] private Transform _stemTransform;
    [SerializeField] private Transform _handleTransform;

    [Header("Grab parameters")] [SerializeField]
    private string _grabGestureName = "Grab";

    [SerializeField] private float _grabRadius = .1f;

    private GRPLGestureRecognizer _gestureRecognizer;
    private RhinoxJoint _currentInteractJoint;

    private Vector3 _baseToHandle;
    private Vector3 _baseToProjJoint;

    private Vector3 _initialHandlePos;
    private Vector3 _initialHandleRot;

    //DEBUG
    private Vector3 _projectedPos;

    private void Awake()
    {
        //Force the ForcedInteractJoint
        ForceInteractibleJoint = true;
        ForcedInteractJointID = XRHandJointID.Palm;

        // Link to gesture recognizer
        GRPLGestureRecognizer.GestureRecognizerGlobalInitialized += OnGestureRecognizerGlobalInitialized;

        // Set initial state
        _initialHandlePos = _handleTransform.position;
        _initialHandleRot = _handleTransform.rotation.eulerAngles;
    }

    /// <summary>
    /// Event reaction to the globalInitialized event of the Gesture Recognizer.<br />
    /// Saves the recognizer in a private field.
    /// </summary>
    /// <param name="obj"></param>
    private void OnGestureRecognizerGlobalInitialized(GRPLGestureRecognizer obj)
    {
        _gestureRecognizer = obj;
    }

    private void Update()
    {
        if (State != GRPLInteractionState.Interacted)
            return;

        if (_currentInteractJoint == null)
            return;

        Vector3 basePos = _baseTransform.position;

        // Project the joint on the lever plane
        Vector3 projectedPos =
            _currentInteractJoint.JointPosition.ProjectOnPlaneAndTranslate(basePos, _baseTransform.right);
        _projectedPos = projectedPos;
        
        // If the projected position is behind the lever, return
        if(!InteractableMathUtils.IsPositionInFrontOfPlane(projectedPos, _baseTransform.position, _baseTransform.forward))
            return;
        
        // Calculate vector from the base to the projected joint
        Vector3 baseToJoint = projectedPos - basePos;
        _baseToProjJoint = baseToJoint;

        // Calculate the vector from the base to the handle
        Vector3 baseToHandle = _handleTransform.position - basePos;
        _baseToHandle = baseToHandle;

        // Calculate the vector from the base to the initial handle pos
        Vector3 baseToInitialHandle = _initialHandlePos - basePos;

        // Calculate the angle between the two vectors
        // Vector3.Angle return an angle in [0;180] degrees
        float angle = Vector3.Angle(baseToInitialHandle, baseToJoint);
        
        // Set the final rotation
        Quaternion newRot = Quaternion.identity;
        newRot.eulerAngles = _initialHandleRot + new Vector3(angle, 0, 0);

        _stemTransform.rotation = newRot;
    }
    
    public override Vector3 GetReferencePoint()
    {
        return _handleTransform.position;
    }

    public override bool CheckForInteraction(RhinoxJoint joint, RhinoxHand hand)
    {
        // Get the current gesture from the target hand
        RhinoxGesture gestureOnHand = _gestureRecognizer.GetGestureOnHand(hand);

        // If there is currently no gesture on the target hand
        if (gestureOnHand == null)
            return false;

        //If the gesture does not have the target name, return false
        if (!gestureOnHand.Name.Equals(_grabGestureName))
            return false;

        if (State == GRPLInteractionState.Interacted)
            return true;

        if (!_gestureRecognizer.WasRecognizedGestureStartedThisFrame(hand))
            return false;

        // Return whether the interact joint is in range
        float jointDistSq = joint.JointPosition.SqrDistanceTo(_handleTransform.position);

        if (jointDistSq < _grabRadius * _grabRadius)
        {
            _currentInteractJoint = joint;
            return true;
        }

        return false;
    }

    public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint)
    {
        joint = joints.FirstOrDefault(x => x.JointID == ForcedInteractJointID);

        return joint != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform transform1 = transform;
        Vector3 basePos = _baseTransform.position;
        Vector3 handlePos = _handleTransform.position;

        Vector3 direction = handlePos - basePos;
        direction.Normalize();
        Gizmos.DrawSphere(basePos, .01f);

        // Calculate the distance
        float distance = Vector3.Distance(basePos, handlePos);

        Handles.DrawWireArc(basePos, transform1.right, direction, 180, distance);

        using (new eUtility.GizmoColor(Color.green))
        {
            // Draw lever begin
            Vector3 beginPos = basePos + direction * distance;
            Handles.Label(beginPos, "Arc begin");
            Gizmos.DrawSphere(beginPos, .01f);
        }

        using (new eUtility.GizmoColor(Color.red))
        {
            // Draw lever end
            Vector3 endPos = basePos - direction * distance;
            Handles.Label(endPos, "Arc end");
            Gizmos.DrawSphere(endPos, .005f);
        }

        using (new eUtility.GizmoColor(Color.black))
        {
            // Draw the direction
            Gizmos.DrawRay(basePos, _baseToHandle);
        }

        using (new eUtility.GizmoColor(Color.blue))
        {
            // Draw the direction
            Gizmos.DrawRay(basePos, _baseToProjJoint);
        }
    }
#endif
}