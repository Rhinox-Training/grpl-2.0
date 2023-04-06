using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    [CustomPropertyDrawer(typeof(RangeFieldAttribute))]
    public class RangeFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rangeFieldAttribute = (RangeFieldAttribute)attribute;
            float max = property.serializedObject.FindProperty(rangeFieldAttribute.MaxFieldName).floatValue;
            property.floatValue = Mathf.Clamp(property.floatValue, rangeFieldAttribute.Min, max);
            EditorGUILayout.Slider(property, rangeFieldAttribute.Min, max, label);
        }
    }
}