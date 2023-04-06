using System;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RangeFieldAttribute : PropertyAttribute
    {
        public float Min { get; set; }
        
        public string MaxFieldName { get; set; }
        
        
        public RangeFieldAttribute(float min, string maxFieldName)
        {
            Min = min;
            MaxFieldName = maxFieldName;
        }
    }
}