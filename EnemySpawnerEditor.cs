using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor {

    EnemySpawner spawner;
    Color discAlpha = Color.black * .8f;
    Color leaderColor = Color.yellow;
    Color minionColor = Color.yellow * .5f + Color.red * .5f;
    Color lineColor = Color.red;

    private void OnEnable() {
        spawner = (EnemySpawner)target;
    }

    public override void OnInspectorGUI() {
        using (var check = new EditorGUI.ChangeCheckScope()) {
            DrawDefaultInspector();

            spawner.radius = Mathf.Max(0, EditorGUILayout.FloatField("Minion Radius", spawner.radius));
            spawner.minionSpawnCount = Mathf.Max(spawner.minions != null ? spawner.minions.Count : 0, EditorGUILayout.IntField("Minions Spawn Count", spawner.minionSpawnCount));
            spawner.minionOffsetRotation = EditorGUILayout.Slider("Minion Offset Angle", spawner.minionOffsetRotation, 0, 360f / spawner.minionSpawnCount);

            if (GUILayout.Button("Randomize Seed")) {
                spawner.RandomizeSeed();
            }

            if (check.changed) {
                SceneView.RepaintAll();
            }
        }
    }

    private void OnSceneGUI() {
        if (spawner.leader != null) {
            Handles.color = leaderColor - discAlpha;
            Handles.DrawSolidDisc(spawner.transform.position, Vector3.up, 1.25f);
            Handles.color = leaderColor;
            Handles.DrawWireDisc(spawner.transform.position, Vector3.up, 1.25f);
        }

        List<Vector3> minionSpawnLocations = spawner.GetMinionSpawnLocations();
        List<int> minionSpawnIndices = spawner.GetRandomMinionSpawns();

        for (int i = 0; i < spawner.minionSpawnCount; i++) {
            if (spawner.leader != null) {
                Handles.color = lineColor;
                Handles.DrawLine(spawner.transform.position, minionSpawnLocations[i]);
            }

            Handles.color = minionColor - discAlpha;
            Handles.DrawSolidDisc(minionSpawnLocations[i], Vector3.up, 1);
            Handles.color = minionColor;
            Handles.DrawWireDisc(minionSpawnLocations[i] + Vector3.up * .25f * minionSpawnIndices[i], Vector3.up, 1);
        }
        
        Handles.color = lineColor;
        Handles.DrawWireArc(spawner.transform.position, Vector3.up, -spawner.transform.right, 180, spawner.radius);
    }
}
