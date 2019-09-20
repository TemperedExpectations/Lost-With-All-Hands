using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor {

    WeaponData data;

    private void OnEnable() {
        data = (WeaponData)target;
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (!data.magReload) {
            data.reloadReadyTime = EditorGUILayout.FloatField("Reload Ready Time", data.reloadReadyTime);
        }

        if (data.weaponType == WeaponData.WeaponType.projectile) {
            data.ammo = EditorGUILayout.IntField("Ammo", data.ammo);
        }
        else if (data.weaponType == WeaponData.WeaponType.battery) {
            data.ammo = EditorGUILayout.IntField("Shots Per Battery", data.ammo);
            data.heatPerShot = EditorGUILayout.Slider("Heat Per Shot", data.heatPerShot, 0, 1);
            data.heatDissipationRate = EditorGUILayout.Slider("Heat Dissipation Rate", data.heatDissipationRate, 0, 1);
        }
        else if (data.weaponType == WeaponData.WeaponType.energy) {
            data.ammo = EditorGUILayout.IntField("Shots Per Canister", data.ammo);
            data.numCanisters = EditorGUILayout.IntField("Num Canisters", data.numCanisters);
            data.canisterReloadTime = EditorGUILayout.FloatField("Canister Reload Time", data.canisterReloadTime);
        }
    }
}
