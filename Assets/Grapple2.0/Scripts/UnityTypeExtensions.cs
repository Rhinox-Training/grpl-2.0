using System;
using Rhinox.XR.Grapple;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

public static class UnityTypeExtensions
{
    public static bool Approximately(this Quaternion q1, Quaternion q2, float acceptableRange)
    {
        return 1 - Mathf.Abs(Quaternion.Dot(q1, q2)) < acceptableRange;
    }

    public static bool Approximately(this Vector3 v1, Vector3 v2, float acceptableRange)
    {
        return Approximately(v1.x, v2.x, acceptableRange) &&
               Approximately(v1.y, v2.y, acceptableRange) &&
               Approximately(v1.z, v2.z, acceptableRange);
    }

    public static bool Approximately(this float f1, float f2, float acceptableRange)
    {
        return Math.Abs(f1 - f2) < acceptableRange;
    }

    public static RhinoxHand ToRhinoxHand(this Handedness hand)
    {
        switch (hand)
        {
            case Handedness.Left:
                return RhinoxHand.Left;
            case Handedness.Right:
                return RhinoxHand.Right;
            default:
                return RhinoxHand.Invalid;
        }
    }

    public static Vector3 FromFlippedXVector3f(this Vector3 v)
    {
        return new Vector3() { x = -v.x, y = v.y, z = v.z };
    }

    public static string ToPrefix(this RhinoxHand hand)
    {
        switch (hand)
        {
            case RhinoxHand.Left:
                return "L_";
            case RhinoxHand.Right:
                return "R_";
            case RhinoxHand.Invalid:
            default:
                return "";
        }
    }

    public static void SubscribeAndActivateAsset(InputActionReference reference,
        Action<InputAction.CallbackContext> performed = null,
        Action<InputAction.CallbackContext> canceled = null)
    {
        if (reference == null)
            return;

        reference.asset.Enable();

        var action = GetInputAction(reference);
        if (action != null)
        {
            if (performed != null)
                action.performed += performed;
            if (canceled != null)
                action.canceled += canceled;
        }
    }

    public static void Unsubscribe(InputActionReference reference,
        Action<InputAction.CallbackContext> performed = null,
        Action<InputAction.CallbackContext> canceled = null)
    {
        var action = GetInputAction(reference);
        if (action != null)
        {
            if (performed != null)
                action.performed -= performed;
            if (canceled != null)
                action.canceled -= canceled;
        }
    }

    private static InputAction GetInputAction(InputActionReference actionReference)
    {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
        return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
    }
}
