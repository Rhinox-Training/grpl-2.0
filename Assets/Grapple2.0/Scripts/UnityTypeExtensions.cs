using System;
using Rhinox.XR.Grapple;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

/// <summary>
/// Contains extensions for Unity types.
/// </summary>
public static class UnityTypeExtensions
{
    /// <summary>
    /// Determines if two Quaternions are approximately equal based on the acceptable range.
    /// </summary>
    /// <param name="q1">The first Quaternion.</param>
    /// <param name="q2">The second Quaternion.</param>
    /// <param name="acceptableRange">The acceptable range for the difference between the two Quaternions.</param>
    /// <returns>A boolean indicating if the two Quaternions are approximately equal.</returns>
    public static bool Approximately(this Quaternion q1, Quaternion q2, float acceptableRange)
    {
        return 1 - Mathf.Abs(Quaternion.Dot(q1, q2)) < acceptableRange;
    }

    /// <summary>
    /// Determines if two Vector3s are approximately equal based on the acceptable range.
    /// </summary>
    /// <param name="v1">The first Vector3.</param>
    /// <param name="v2">The second Vector3.</param>
    /// <param name="acceptableRange">The acceptable range for the difference between the two Vector3s.</param>
    /// <returns>A boolean indicating if the two Vector3s are approximately equal.</returns>
    public static bool Approximately(this Vector3 v1, Vector3 v2, float acceptableRange)
    {
        return Approximately(v1.x, v2.x, acceptableRange) &&
               Approximately(v1.y, v2.y, acceptableRange) &&
               Approximately(v1.z, v2.z, acceptableRange);
    }

    /// <summary>
    /// Determines if two floats are approximately equal based on the acceptable range.
    /// </summary>
    /// <param name="f1">The first float.</param>
    /// <param name="f2">The second float.</param>
    /// <param name="acceptableRange">The acceptable range for the difference between the two floats.</param>
    /// <returns>A boolean indicating if the two floats are approximately equal.</returns>
    public static bool Approximately(this float f1, float f2, float acceptableRange)
    {
        return Math.Abs(f1 - f2) < acceptableRange;
    }

    /// <summary>
    /// Converts a Unity InputSystem.Handedness value to a Rhinox.XR.Grapple.RhinoxHand value.
    /// </summary>
    /// <param name="hand">The Handedness value to convert.</param>
    /// <returns>The RhinoxHand value equivalent to the given Handedness value.</returns>
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

    /// <summary>
    /// Returns a new Vector3 with the X component flipped.
    /// </summary>
    /// <param name="v">The original Vector3.</param>
    /// <returns>A new Vector3 with the X component flipped.</returns>
    public static Vector3 FromFlippedXVector3f(this Vector3 v)
    {
        return new Vector3() { x = -v.x, y = v.y, z = v.z };
    }

    /// <summary>
    /// Returns a string prefix based on the given RhinoxHand value.
    /// </summary>
    /// <param name="hand">The RhinoxHand value to use.</param>
    /// <returns>A string prefix for the given RhinoxHand value.</returns>
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

    /// <summary>
    /// Subscribes to the given InputActionReference's events and activates its asset.
    /// </summary>
    /// <param name="reference">The InputActionReference to use.</param>
    /// <param name="performed">An optional callback to be called when the action is performed.</param>
    /// <param name="canceled">An optional callback to be called when the action is canceled.</param>
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

    /// <summary>
    /// Unsubscribes from the given InputActionReference's events.
    /// </summary>
    /// <param name="reference">The InputActionReference to use.</param>
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

    /// <summary>
    /// Retrieves the InputAction object associated with the given InputActionReference.
    /// </summary>
    /// <param name="actionReference">The InputActionReference to retrieve the InputAction from.</param>
    /// <returns>The InputAction object associated with the InputActionReference, or null if the InputActionReference is null.</returns>
    private static InputAction GetInputAction(InputActionReference actionReference)
    {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
        return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
    }
}