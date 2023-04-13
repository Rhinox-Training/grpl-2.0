using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Rhinox.XR.Grapple
{
    /// <summary>
    /// This attribute class is used in Unity C# scripts to display an information box above a field in the inspector
    /// window. The information box can contain a message and a message type, such as info, warning, or error.
    /// This can be useful for providing additional context or warnings about a field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        /// <summary>
        /// A string containing the message to display in the information box. 
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// A MessageType enum value specifying the type of message to display in the information box.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// This constructor creates an instance of the InfoBoxAttribute class with the given message and sets the message type to MessageType.Info. 
        /// </summary>
        /// <param name="message">The message to display.</param>
        public InfoBoxAttribute(string message)
        {
            Message = message;
            MessageType = MessageType.Info;
        }

        /// <summary>
        /// This constructor creates an instance of the InfoBoxAttribute class with the given message and message type.
        /// </summary>
        /// <param name="message"> The message to display.</param>
        /// <param name="messageType">The desired message type.</param>
        public InfoBoxAttribute(string message, MessageType messageType)
        {
            Message = message;
            MessageType = messageType;
        }
    }
}
#endif