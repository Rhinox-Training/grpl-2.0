using System;
using System.Diagnostics;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HideIfFieldFalseAttribute : PropertyAttribute
    {
        public string BoolFieldName;
        public float PropertyHeight = 0f;
        public bool HeightPassed = false;
        public HideIfFieldFalseAttribute(string fieldName)
        {
            BoolFieldName = fieldName;
        }

        public HideIfFieldFalseAttribute(string fieldName, float propertyHeight)
        {
            BoolFieldName = fieldName;
            PropertyHeight = propertyHeight;
            HeightPassed = true;
        }
    }
}