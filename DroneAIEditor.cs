using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DroneAI))]
public class DroneAIEditor : Editor {
    
    DroneAI drone;
    List<Editor> editors;

    private void OnEnable() {
        drone = (DroneAI)target;
        editors = new List<Editor>();
        for (int i = 0; i < drone.behaviorData.Count; i++) {
            editors.Add(null);
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        for (int i = 0; i < drone.behaviorData.Count; i++) {
            Editor editor = editors[i];
            DrawSettingsEditor(drone.behaviorData[i], ref drone.behaviorData[i].foldout, ref editor);
            editors[i] = editor;
        }
    }

    void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor) {
        if (settings != null) {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
            
            if (foldout) {
                CreateCachedEditor(settings, null, ref editor);
                editor.OnInspectorGUI();
            }
        }
    }
}
