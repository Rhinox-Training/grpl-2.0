using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    [CustomPropertyDrawer(typeof(HideIfFieldFalseAttribute))]
    public class HideIfFieldFalseAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hideIfFieldFalseAttribute = (HideIfFieldFalseAttribute)attribute;
            var val = property.serializedObject.FindProperty(hideIfFieldFalseAttribute.BoolFieldName).boolValue;
            if (val)
            {
                EditorGUILayout.PropertyField(property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideIfFieldFalseAttribute = (HideIfFieldFalseAttribute)attribute;
            bool heightPassed = hideIfFieldFalseAttribute.HeightPassed;
            if (heightPassed)
                return hideIfFieldFalseAttribute.PropertyHeight;
            else
                return base.GetPropertyHeight(property, label);
        }
    }
}