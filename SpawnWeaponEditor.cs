using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpawnWeapon))]
public class SpawnWeaponEditor : Editor {

    SpawnWeapon spawner;

    private void OnEnable() {
        spawner = (SpawnWeapon)target;
    }

    public override void OnInspectorGUI() {
        if (GUILayout.Button("Refresh")) {
            spawner.Refresh();
        }

        DrawDefaultInspector();
    }

}
