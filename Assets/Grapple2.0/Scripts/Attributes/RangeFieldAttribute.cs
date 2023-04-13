using System;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// The RangeFieldAttribute is an attribute that can be applied to a field to restrict the minimum value of that
    /// field to the specified value and maximum value to the value of a sibling field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RangeFieldAttribute : PropertyAttribute
    {
        /// <summary>
        /// A float holding the minimum value for the field.
        /// </summary>
        public float Min { get; set; }
        
        /// <summary>
        /// A string holding the name of the sibling field to compare for the maximum value.
        /// </summary>
        public string MaxFieldName { get; set; }
        
        /// <summary>
        /// Creates a new RangeFieldAttribute object with the specified minimum value and maximum field name.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="maxFieldName">The name of the sibling, that holds the maximum value.</param>
        public RangeFieldAttribute(float min, string maxFieldName)
        {
            Min = min;
            MaxFieldName = maxFieldName;
        }
    }
}