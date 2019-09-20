using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AIAttack))]
public class AIAttackEditor : Editor {

    AIAttack creator;

    private void OnEnable() {
        creator = target as AIAttack;
    }

    private void OnSceneGUI() {
        Handles.DrawLine(creator.firePoints[0].position, creator.firePoints[0].position + creator.firePoints[0].forward * 200);
    }
}
