using System;
using System.Diagnostics;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HideIfFieldAttribute : PropertyAttribute
    {
        public string BoolFieldName;
        public bool BoolState = false;
        public float PropertyHeight = 0f;
        public bool HeightPassed = false;

        public HideIfFieldAttribute(bool boolState, string fieldName)
        {
            BoolState = boolState;
            BoolFieldName = fieldName;
        }

        public HideIfFieldAttribute(bool boolState, string fieldName, float propertyHeight)
        {
            BoolState = boolState;
            BoolFieldName = fieldName;
            PropertyHeight = propertyHeight;
            HeightPassed = true;
        }
    }
}