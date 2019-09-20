using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProtectorBehaviorData))]
public class ProtectorBehaviorDataEditor : Editor {

    ProtectorBehaviorData behavior;

    private void OnEnable() {
        behavior = (ProtectorBehaviorData)target;
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        switch (behavior.behavior) {
            case BehaviorType.alert:
                behavior.f1 = Mathf.Max(0, EditorGUILayout.FloatField("Investigate Distance", behavior.f1));
                behavior.f2 = EditorGUILayout.Slider("Investigate Chance", behavior.f2, 0, 1);
                behavior.f3 = Mathf.Max(0, EditorGUILayout.FloatField("Alert Time", behavior.f3));
                behavior.f4 = Mathf.Max(0, EditorGUILayout.FloatField("Protect Distance", behavior.f4));
                break;
            case BehaviorType.combat:
                behavior.f1 = Mathf.Max(0, EditorGUILayout.FloatField("Combat Distance", behavior.f1));
                behavior.f2 = Mathf.Max(0, EditorGUILayout.FloatField("Evade Time", behavior.f2));
                behavior.f3 = Mathf.Max(0, EditorGUILayout.FloatField("Evade Distance", behavior.f3));
                behavior.f4 = Mathf.Max(0, EditorGUILayout.FloatField("Protect Distance", behavior.f4));
                behavior.f5 = Mathf.Max(0, EditorGUILayout.FloatField("Deploy Distance", behavior.f5));
                behavior.f6 = Mathf.Max(0, EditorGUILayout.FloatField("Deploy Recharge Time", behavior.f6));
                behavior.o1 = (GameObject)EditorGUILayout.ObjectField("Shield", behavior.o1, typeof(GameObject), false);
                behavior.o2 = (GameObject)EditorGUILayout.ObjectField("Indicator", behavior.o2, typeof(GameObject), false);
                break;
        }
    }
}
