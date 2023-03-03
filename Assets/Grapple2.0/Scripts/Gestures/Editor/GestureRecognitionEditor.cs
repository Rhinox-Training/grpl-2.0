using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [CustomEditor(typeof(GestureRecognizer))]
    public class GestureRecognitionEditor : Editor
    {
        #region Serialized properties

        private SerializedProperty _importPath;
        private SerializedProperty _exportPath;
        private SerializedProperty _exportFileName;
        private SerializedProperty _newGestureName;
        private SerializedProperty _handToRecord;
        #endregion

        private void OnEnable()
        {
            _importPath = serializedObject.FindProperty("ImportFilePath");
            _exportPath = serializedObject.FindProperty("ExportFilePath");
            _exportFileName = serializedObject.FindProperty("ExportFileName");
            _newGestureName = serializedObject.FindProperty("SavedGestureName");
            _handToRecord = serializedObject.FindProperty("HandToRecord");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var script = (GestureRecognizer)target;

            //--------------------------
            // Import
            //--------------------------

            GUILayout.Label("Gesture import settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            if (GUILayout.Button("Import gesture file"))
            {
                var chosenFilePath = EditorUtility.OpenFilePanel("Choose target folder", 
                    script.ImportFilePath != "" ? script.ImportFilePath : Application.dataPath, "json");
                
                if (chosenFilePath.Length != 0)
                    _importPath.stringValue = chosenFilePath;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.LabelField("Current import directory:", EditorStyles.largeLabel);
            GUILayout.Label(_importPath.stringValue);
            
            GUILayout.Space(10);

            //--------------------------
            // Export
            //--------------------------
            GUILayout.Label("Gesture export settings", EditorStyles.boldLabel);
            if (GUILayout.Button("Choose target folder"))
            {
                var chosenFolder = EditorUtility.OpenFolderPanel("Choose target folder",
                    script.ImportFilePath != "" ? script.ExportFilePath : Application.dataPath, "");
                if (chosenFolder.Length != 0)
                {
                    _exportPath.stringValue = chosenFolder;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.LabelField("Current export directory:", EditorStyles.largeLabel);
            GUILayout.Label(_exportPath.stringValue);
            GUILayout.Space(5);

            _exportFileName.stringValue = EditorGUILayout.TextField("Export file name", _exportFileName.stringValue);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Name from file in folder:");
            if (EditorGUILayout.DropdownButton(new GUIContent(_exportFileName.stringValue), FocusType.Passive))
            {
                ShowSelector(script);
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);

            //--------------------------
            // Recording
            //--------------------------
            GUILayout.Label("Gesture recording settings", EditorStyles.boldLabel);
            _newGestureName.stringValue = EditorGUILayout.TextField("New gesture name", _newGestureName.stringValue);
            EditorGUILayout.PropertyField(_handToRecord);

            GUILayout.Space(10);

            //--------------------------
            // Recognition
            //--------------------------
            GUILayout.Label("Gesture recognition settings", EditorStyles.boldLabel);

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowSelector(GestureRecognizer gestureRecognizer)
        {
            if (string.IsNullOrWhiteSpace(gestureRecognizer.ExportFilePath))
                return;
            var absPath = gestureRecognizer.ExportFilePath;
            var dirInfo = new DirectoryInfo(absPath);
            if (!dirInfo.Exists)
                return;

            var sequenceMenu = new GenericMenu();
            var files = dirInfo.GetFiles();
            foreach (var file in files)
            {
                sequenceMenu.AddItem(new GUIContent(file.Name), false, OnSequenceSelected, file.Name);
            }

            sequenceMenu.ShowAsContext();
        }

        private void OnSequenceSelected(object userData)
        {
            if (!(userData is string dir))
                return;
            var result = dir.Split('.');
            _exportFileName.stringValue = result[0];
            serializedObject.ApplyModifiedProperties();
        }
    }
}