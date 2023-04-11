using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var infoBoxAttribute = (InfoBoxAttribute)attribute;
            EditorGUILayout.HelpBox(infoBoxAttribute.Message, infoBoxAttribute.MessageType);
        }
    }
}