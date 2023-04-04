using System.Collections.Generic;
using UnityEngine.XR.Hands;
using Rhinox.XR.Grapple.It;
using Rhinox.XR.Grapple;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

public class GRPLLever : GRPLInteractable
{
    [Space(5)] [Header("Lever parameters")] [SerializeField]
    private Transform _stemTransform;

    [SerializeField] private Transform _handleTransform;


    private void Awake()
    {
        //Force the ForcedInteractJoint
        ForceInteractibleJoint = true;
        ForcedInteractJointID = XRHandJointID.Palm;

        // Link to gesture recognizer
    }

    public override Vector3 GetReferencePoint()
    {
        return _handleTransform.position;
    }

    public override bool CheckForInteraction(RhinoxJoint joint)
    {
        
        // 1. Check if there is currently a grab gesture for this hand
        // a. If there is not return false and clean up the interacted state, if needed
        // 2. Check if the gesture was started this frame
        // a. If not, return true
        // 3. Check if the hand is in interactible range
        // Start the interaction
        
        return false;
    }

    public override bool TryGetCurrentInteractJoint(ICollection<RhinoxJoint> joints, out RhinoxJoint joint)
    {
        joint = joints.FirstOrDefault(x => x.JointID == ForcedInteractJointID);

        return joint == null;
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