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
        private SerializedProperty _overwriteOnImport;
        private SerializedProperty _exportPath;
        private SerializedProperty _exportFileName;
        private SerializedProperty _newGestureName;
        private SerializedProperty _handToRecord;
        private SerializedProperty _recognitionThreshold;
        private SerializedProperty _gestures;
        #endregion

        private void OnEnable()
        {
            _importPath = serializedObject.FindProperty("ImportFilePath");
            _overwriteOnImport = serializedObject.FindProperty("OverwriteGesturesOnImport");
            _exportPath = serializedObject.FindProperty("ExportFilePath");
            _exportFileName = serializedObject.FindProperty("ExportFileName");
            _newGestureName = serializedObject.FindProperty("SavedGestureName");
            _handToRecord = serializedObject.FindProperty("HandToRecord");
            _recognitionThreshold = serializedObject.FindProperty("RecognitionThreshold");
            _gestures = serializedObject.FindProperty("Gestures");
            _gestures = serializedObject.FindProperty("Gestures");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var script = (GestureRecognizer)target;

            //--------------------------
            // Import
            //--------------------------
            GUILayout.Space(10);
            GUILayout.Label("Gesture import settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            _overwriteOnImport.boolValue =
                EditorGUILayout.Toggle("Overwrite gestures on import", _overwriteOnImport.boolValue);
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
            
            GUILayout.Space(20);

            //--------------------------
            // Export
            //--------------------------
            GUILayout.Label("Gesture export settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
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
            
            GUILayout.Space(20);

            //--------------------------
            // Recording
            //--------------------------
            GUILayout.Label("Gesture recording settings", EditorStyles.boldLabel);
            GUILayout.Space(10);

            _newGestureName.stringValue = EditorGUILayout.TextField("New gesture name", _newGestureName.stringValue);
            EditorGUILayout.PropertyField(_handToRecord);

            GUILayout.Space(20);

            //--------------------------
            // Recognition
            //--------------------------
            GUILayout.Label("Gesture recognition settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            _recognitionThreshold.floatValue = EditorGUILayout.FloatField("Recognition threshold", _recognitionThreshold.floatValue);

            //--------------------------
            // Gestures
            //--------------------------
            GUILayout.Space(20);
            var warningStyle = new GUIStyle()
            {
                normal =
                {
                    textColor = Color.yellow
                }
            };
            GUILayout.Label("!!!  Warning: each gesture should contain 26 joints  !!!", warningStyle);
            EditorGUILayout.PropertyField(_gestures,new GUIContent("Current Gestures"));

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