using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [CustomPropertyDrawer(typeof(HideIfFieldAttribute))]
    public class HideIfFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hideIfFieldAttribute = (HideIfFieldAttribute)attribute;
            var val = property.serializedObject.FindProperty(hideIfFieldAttribute.BoolFieldName).boolValue;

           // var fieldname = property.name;
            //label.text

            if (hideIfFieldAttribute.BoolState)
            {
                if (!val)
                    EditorGUILayout.PropertyField(property, label);
            }
            else
            {
                if (val)
                    EditorGUILayout.PropertyField(property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideIfFieldAttribute = (HideIfFieldAttribute)attribute;
            bool heightPassed = hideIfFieldAttribute.HeightPassed;
            if (heightPassed)
                return hideIfFieldAttribute.PropertyHeight;
            else
                return 0f;//EditorGUI.GetPropertyHeight(property, label);// base.GetPropertyHeight(property, label);
        }
    }
}