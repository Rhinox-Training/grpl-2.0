using System;
using System.Diagnostics;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This attribute class is used in Unity C# scripts to hide a serialized field in the inspector window if a sibling
    /// boolean field with the specified name has a certain value. This can be useful for creating conditional fields
    /// that are only visible when a certain condition is met.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HideIfFieldAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of the sibling boolean field that should be compared to.
        /// </summary>
        public string BoolFieldName;

        /// <summary>
        /// The boolean value that the sibling field should have to hide the serialized field.
        /// </summary>
        public bool BoolState = false;

        /// <summary>
        /// The height of the serialized field in the inspector window when it is visible.
        /// </summary>
        public float PropertyHeight = 0f;

        /// <summary>
        /// A boolean value indicating whether the PropertyHeight property was set in the constructor. .
        /// </summary>
        public bool HeightPassed = false;

        /// <summary>
        /// Creates the attribute and fills the BoolFieldName and BoolState fields.
        /// </summary>
        /// <param name="boolState"></param>
        /// <param name="fieldName"></param>
        public HideIfFieldAttribute(bool boolState, string fieldName)
        {
            BoolState = boolState;
            BoolFieldName = fieldName;
        }

        /// <summary>
        /// Creates the attribute and fills the BoolFieldName, BoolState and PropertyHeight fields. Also sets HeightPassed to true.
        /// </summary>
        /// <param name="boolState"></param>
        /// <param name="fieldName"></param>
        /// <param name="propertyHeight"></param>
        public HideIfFieldAttribute(bool boolState, string fieldName, float propertyHeight)
        {
            BoolState = boolState;
            BoolFieldName = fieldName;
            PropertyHeight = propertyHeight;
            HeightPassed = true;
        }
    }
}