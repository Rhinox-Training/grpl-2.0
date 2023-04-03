using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rhinox.XR.Grapple
{
    [CustomEditor(typeof(GRPLGestureRecognizer))]
    public class GestureRecognitionEditor : Editor
    {
        private SerializedProperty _importPath;
        private SerializedProperty _importOnPlay;
        private SerializedProperty _overwriteOnImport;

        private SerializedProperty _exportOnDestroy;
        private SerializedProperty _exportPath;
        private SerializedProperty _exportFileName;

        private SerializedProperty _recordActionReference;
        private SerializedProperty _newGestureName;
        private SerializedProperty _handToRecord;
        private SerializedProperty _recognitionDistanceThreshold;
        private SerializedProperty _recognitionForwardThreshold;

        private SerializedProperty _useJointForward;
        private SerializedProperty _forwardJoint;
        
        private SerializedProperty _gestures;


        private bool _importInEditor = false;        
        private bool _showImportSettings = false;
        private bool _showExportSettings = false;
        private bool _showRecordingSettings = false;

        
        private void OnEnable()
        {
            _importOnPlay = serializedObject.FindProperty("ImportOnPlay");
            _importPath = serializedObject.FindProperty("ImportFilePath");
            _overwriteOnImport = serializedObject.FindProperty("OverwriteGesturesOnImport");

            _exportOnDestroy = serializedObject.FindProperty("ExportOnDestroy");
            _exportPath = serializedObject.FindProperty("ExportFilePath");
            _exportFileName = serializedObject.FindProperty("ExportFileName");
            
            _recordActionReference = serializedObject.FindProperty("RecordActionReference");
            _newGestureName = serializedObject.FindProperty("SavedGestureName");
            _handToRecord = serializedObject.FindProperty("RhinoxHandToRecord");
            
            _recognitionDistanceThreshold = serializedObject.FindProperty("GestureDistanceThreshold");
            _recognitionForwardThreshold = serializedObject.FindProperty("GestureForwardThreshold");
            _gestures = serializedObject.FindProperty("Gestures");
            _useJointForward = serializedObject.FindProperty("UseJointForward");
            _forwardJoint = serializedObject.FindProperty("ForwardJoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var script = (GRPLGestureRecognizer)target;

            //--------------------------
            // Import
            //--------------------------
            _showImportSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showImportSettings, "Import settings");
            
            if (_showImportSettings)
                ShowImportSettings(script);

            EditorGUILayout.EndFoldoutHeaderGroup();
            
            InsertSeparation();
            
            //--------------------------
            // Export
            //--------------------------
            _showExportSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showExportSettings, "Export settings");

            if (_showExportSettings)
                ShowExportSettings(script);
            
            EditorGUILayout.EndFoldoutHeaderGroup();
            InsertSeparation();

            //--------------------------
            // Recording
            //--------------------------
            _showRecordingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showRecordingSettings, "Recording settings");

            if (_showRecordingSettings)
                ShowRecordingSettings();

            EditorGUILayout.EndFoldoutHeaderGroup();
            InsertSeparation();

            //--------------------------
            // Gestures
            //--------------------------
           
            EditorGUILayout.PropertyField(_gestures,new GUIContent("Current Gestures"));
            
            var warningStyle = new GUIStyle()
            {
                normal =
                {
                    textColor = Color.yellow
                }
            };
            GUILayout.Label("!!!  Warning: each gesture should contain 26 joints  !!!", warningStyle);
            
            serializedObject.ApplyModifiedProperties();
        }


        private void InsertSeparation()
        {
            EditorGUILayout.Space(2.5f);
            EditorGUILayout.Separator();
            EditorGUILayout.Space(2.5f);
        }

        private void ShowSelector(GRPLGestureRecognizer gestureRecognizer)
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

        private void ShowImportSettings(GRPLGestureRecognizer script)
        {
            _overwriteOnImport.boolValue =
                EditorGUILayout.Toggle("Overwrite gestures on import", _overwriteOnImport.boolValue);
            
            _importOnPlay.boolValue = EditorGUILayout.Toggle("Import on play", _importOnPlay.boolValue);
            if (_importOnPlay.boolValue)
            {
                if (GUILayout.Button("Set gesture file path"))
                {
                    var chosenFilePath = EditorUtility.OpenFilePanel("Choose target folder",
                        script.ImportFilePath != "" ? script.ImportFilePath : Application.dataPath, "json");

                    if (chosenFilePath.Length != 0)
                        _importPath.stringValue = chosenFilePath;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.LabelField("Current import directory:", EditorStyles.largeLabel);
                EditorGUILayout.LabelField(_importPath.stringValue);
                EditorGUILayout.Separator();
            }

            _importInEditor = EditorGUILayout.Toggle("Import gestures in editor", _importInEditor);
            if (_importInEditor)
            {
                if (GUILayout.Button("Import gestures"))
                {
                    var chosenFilePath = EditorUtility.OpenFilePanel("Choose target folder",
                        script.ImportFilePath != "" ? script.ImportFilePath : Application.dataPath, "json");

                    script.ReadGesturesFromJson(chosenFilePath);
                }
            }
        }

        private void ShowExportSettings(GRPLGestureRecognizer script)
        {
            EditorGUILayout.PropertyField(_exportOnDestroy);
            EditorGUILayout.Separator();
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
            EditorGUILayout.Separator();

            _exportFileName.stringValue =
                EditorGUILayout.TextField("Export file name", _exportFileName.stringValue);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Name from file in folder:");
            if (EditorGUILayout.DropdownButton(new GUIContent(_exportFileName.stringValue), FocusType.Passive))
            {
                ShowSelector(script);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            if (GUILayout.Button("Export Gestures"))
            {
                script.WriteGesturesToJson();
                
            }
        }

        private void ShowRecordingSettings()
        {
            EditorGUILayout.PropertyField(_recordActionReference);
            
            _newGestureName.stringValue =
                EditorGUILayout.TextField("New gesture name", _newGestureName.stringValue);
            EditorGUILayout.PropertyField(_handToRecord);
            _useJointForward.boolValue =
                EditorGUILayout.Toggle("Use joint forward", _useJointForward.boolValue);
            if(_useJointForward.boolValue)
                EditorGUILayout.PropertyField(_forwardJoint);

            _recognitionDistanceThreshold.floatValue = EditorGUILayout.FloatField("Recognition distance threshold",
                _recognitionDistanceThreshold.floatValue);
            _recognitionForwardThreshold.floatValue = EditorGUILayout.FloatField("Recognition forward threshold",
                _recognitionForwardThreshold.floatValue);
        }        
    }
}