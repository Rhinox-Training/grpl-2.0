using System.Collections.Generic;
using UnityEngine.XR.Hands;
using Rhinox.XR.Grapple.It;
using Rhinox.XR.Grapple;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Rhinox.Lightspeed;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

public class GRPLLever : GRPLInteractable
{
    [Space(5)] [Header("Lever parameters")] [SerializeField]
    private Transform _stemTransform;

    [SerializeField] private Transform _handleTransform;

    [Header("Grab parameters")] [SerializeField]
    private string _grabGestureName = "Grab";

    [SerializeField] private float _grabRadius = .1f;

    private GRPLGestureRecognizer _gestureRecognizer;

    private void Awake()
    {
        //Force the ForcedInteractJoint
        ForceInteractibleJoint = true;
        ForcedInteractJointID = XRHandJointID.Palm;

        // Link to gesture recognizer
        GRPLGestureRecognizer.GestureRecognizerGlobalInitialized += OnGestureRecognizerGlobalInitialized;
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
        
        return (jointDistSq < _grabRadius * _grabRadius);
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
        Vector3 position = transform1.position;
        Vector3 position1 = _handleTransform.position;

        Vector3 direction = position - position1;
        direction.Normalize();

        // Calculate the distance
        float distance = Vector3.Distance(position, position1);

        // Debug.Log(transform1.up);
        Handles.DrawWireArc(position, transform1.forward, direction, 180, distance);

        using (new eUtility.GizmoColor(Color.green))
        {
            // Draw lever begin
            Vector3 beginPos = position - direction * distance;
            Gizmos.DrawSphere(beginPos, .01f);
        }

        using (new eUtility.GizmoColor(Color.red))
        {
            // Draw lever end
            Vector3 endPos = position + direction * distance;
            Gizmos.DrawSphere(endPos, .01f);
        }
    }
#endif
}