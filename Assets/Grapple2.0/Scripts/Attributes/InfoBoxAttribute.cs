using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// Attribute to display an infobox above a field with optional MessageType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public string Message { get; set; }
        public MessageType MessageType { get; set; }

        public InfoBoxAttribute(string message)
        {
            Message = message;
            MessageType = MessageType.Info;
        }

        public InfoBoxAttribute(string message, MessageType messageType)
        {
            Message = message;
            MessageType = messageType;
        }
    }
}
#endif