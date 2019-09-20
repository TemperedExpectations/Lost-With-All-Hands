using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathMeshCreator), true)]
public class PathMeshCreatorEditor : Editor {

    protected PathMeshCreator meshCreator;

    public override void OnInspectorGUI() {
        using (var check = new EditorGUI.ChangeCheckScope()) {
            DrawDefaultInspector();

            if (check.changed && meshCreator.autoUpdate) {
                meshCreator.needsUpdate = true;
            }
        }
    }

    protected virtual void OnEnable() {
        meshCreator = (PathMeshCreator)target;
    }
}
