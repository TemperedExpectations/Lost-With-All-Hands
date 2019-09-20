using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathComposite), true)]
public class PathCompositeEditor : Editor {

    protected PathComposite pathTool;
    bool isSubscribed;

    public override void OnInspectorGUI() {
        DrawPathInspector();

        float width = EditorGUIUtility.currentViewWidth;
        EditorGUILayout.BeginHorizontal();
        pathTool.creatorsFoldout = EditorGUILayout.Foldout(pathTool.creatorsFoldout, "Mesh Creators", true);
        if (GUILayout.Button("Update All Creators", GUILayout.MaxWidth(width * .67f))) {
            if (TryFindPathCreator()) {
                pathTool.CreatePath();
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();
        if (pathTool.creatorsFoldout && pathTool.meshCreators != null) {
            int size = EditorGUILayout.DelayedIntField("    Size", pathTool.meshCreators != null ? pathTool.meshCreators.Length : 0);
            if (size != pathTool.meshCreators.Length) {
                List<PathMeshCreator> temp = new List<PathMeshCreator>(pathTool.meshCreators);
                if (size < pathTool.meshCreators.Length) {
                    temp.RemoveRange(size, pathTool.meshCreators.Length - size);
                }
                else if (size > pathTool.meshCreators.Length) {
                    temp.AddRange(new PathMeshCreator[size - pathTool.meshCreators.Length]);
                }

                pathTool.meshCreators = temp.ToArray();
            }
            for (int i = 0; i < size; i++) {
                if (pathTool.meshCreators[i] != null) {
                    EditorGUILayout.BeginHorizontal();
                    pathTool.meshCreators[i].foldout = EditorGUILayout.Foldout(pathTool.meshCreators[i].foldout, "    Creator " + (i + 1), true);
                    pathTool.meshCreators[i] = (PathMeshCreator)EditorGUILayout.ObjectField("", pathTool.meshCreators[i], typeof(PathMeshCreator), true, GUILayout.MaxWidth(width * (pathTool.meshCreators[i].foldout ? .67f : .41f)));
                    if (!pathTool.meshCreators[i].foldout) {
                        if (GUILayout.Button("Update Single", GUILayout.MaxWidth(width * .25f))) {
                            pathTool.toUpdate = i;
                            pathTool.CreatePath();
                            SceneView.RepaintAll();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else {
                    pathTool.meshCreators[i] = (PathMeshCreator)EditorGUILayout.ObjectField("    Creator " + (i + 1), pathTool.meshCreators[i], typeof(PathMeshCreator), true);
                }
                if (pathTool.meshCreators[i] != null) {
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        bool manualUpdate = false;
                        if (pathTool.meshCreators[i].foldout) {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(" ", GUILayout.MaxWidth(width * .25f));
                            pathTool.meshCreators[i].autoUpdate = EditorGUILayout.ToggleLeft("Auto Update", pathTool.meshCreators[i].autoUpdate);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(" ", GUILayout.MaxWidth(width * .25f));
                            pathTool.meshCreators[i].hide = EditorGUILayout.ToggleLeft("Hide", pathTool.meshCreators[i].hide);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(" ", GUILayout.MaxWidth(width * .25f));
                            pathTool.meshCreators[i].debug = EditorGUILayout.ToggleLeft("Debug", pathTool.meshCreators[i].debug);
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(" ", GUILayout.MaxWidth(width * .25f));
                            if (GUILayout.Button("Update Single", GUILayout.MaxWidth(width * .75f))) {
                                pathTool.toUpdate = i;
                                pathTool.CreatePath();
                                SceneView.RepaintAll();
                                manualUpdate = true;
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        if (check.changed && !manualUpdate) {
                            if (!isSubscribed) {
                                TryFindPathCreator();
                                Subscribe();
                            }

                            if (pathTool.meshCreators[i].autoUpdate || pathTool.meshCreators[i].hide) {
                                pathTool.toUpdate = i;
                                pathTool.CreatePath();
                                SceneView.RepaintAll();
                            }
                        }
                    }
                }

                

                if (pathTool.meshCreators[i] != null && pathTool.meshCreators[i].needsUpdate) {
                    pathTool.toUpdate = i;
                    pathTool.CreatePath();
                    pathTool.meshCreators[i].needsUpdate = false;
                    SceneView.RepaintAll();
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        pathTool.edgeGroupsFoldout = EditorGUILayout.Foldout(pathTool.edgeGroupsFoldout, "Mesh Edge Groups", true);
        if (GUILayout.Button("Update All Edge Groups", GUILayout.MaxWidth(width * .67f))) {
            if (TryFindPathCreator()) {
                pathTool.UpdateEdgeGroups(-1);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (pathTool.edgeGroupsFoldout && pathTool.meshEdgeGroups != null) {
            int size = EditorGUILayout.DelayedIntField("    Size", pathTool.meshEdgeGroups.Count);
            if (size != pathTool.meshEdgeGroups.Count) {
                if (size < pathTool.meshEdgeGroups.Count) {
                    pathTool.meshEdgeGroups.RemoveRange(size, pathTool.meshEdgeGroups.Count - size);
                }
                else {
                    while (size > pathTool.meshEdgeGroups.Count) {
                        pathTool.meshEdgeGroups.Add(new MeshEdgeGroup());
                    }
                }
            }

            for (int i = 0; i < size; i++) {
                if (pathTool.meshEdgeGroups[i].edgeGroup.Count == 0) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("    Edge Group " + (i + 1), GUILayout.MaxWidth(width * .345f));
                    int groupSize = EditorGUILayout.DelayedIntField(pathTool.meshEdgeGroups[i].edgeGroup.Count);
                    if (groupSize != pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                        if (groupSize < pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                            pathTool.meshEdgeGroups[i].edgeGroup.RemoveRange(groupSize, pathTool.meshEdgeGroups[i].edgeGroup.Count - groupSize);
                        }
                        else {
                            while (groupSize > pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                                pathTool.meshEdgeGroups[i].edgeGroup.Add(null);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else {
                    EditorGUILayout.BeginHorizontal();
                    pathTool.meshEdgeGroups[i].foldout = EditorGUILayout.Foldout(pathTool.meshEdgeGroups[i].foldout, "    Edge Group " + (i + 1), true);
                    int groupSize = EditorGUILayout.DelayedIntField(pathTool.meshEdgeGroups[i].edgeGroup.Count, GUILayout.MaxWidth(width * .3f));
                    if (groupSize != pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                        if (groupSize < pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                            pathTool.meshEdgeGroups[i].edgeGroup.RemoveRange(groupSize, pathTool.meshEdgeGroups[i].edgeGroup.Count - groupSize);
                        }
                        else {
                            while (groupSize > pathTool.meshEdgeGroups[i].edgeGroup.Count) {
                                pathTool.meshEdgeGroups[i].edgeGroup.Add(null);
                            }
                        }
                    }

                    if (GUILayout.Button("Update Single", GUILayout.MaxWidth(width * .27f))) {
                        pathTool.UpdateEdgeGroups(i);
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (pathTool.meshEdgeGroups[i].foldout) {
                        for (int j = 0; j < pathTool.meshEdgeGroups[i].edgeGroup.Count; j++) {
                            pathTool.meshEdgeGroups[i].edgeGroup[j] = (MeshFilter)EditorGUILayout.ObjectField("        Mesh Edge " + (j + 1), pathTool.meshEdgeGroups[i].edgeGroup[j], typeof(MeshFilter), true);
                        }
                    }
                }
            }
        }
    }

    void DrawPathInspector() {
        if (TryFindPathCreator() && pathTool.GetMaxCurve() > 0) {
            pathTool.curveCreatorFoldout = EditorGUILayout.Foldout(pathTool.curveCreatorFoldout, "Curve Creator", true);
            if (pathTool.curveCreatorFoldout) {
                EditorGUILayout.BeginHorizontal();
                pathTool.curveSettings.useStartValue = EditorGUILayout.Toggle("Start Value", pathTool.curveSettings.useStartValue);
                if (pathTool.curveSettings.useStartValue) {
                    pathTool.curveSettings.startValue = EditorGUILayout.FloatField("", pathTool.curveSettings.startValue);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                pathTool.curveSettings.useEndValue = EditorGUILayout.Toggle("End Value", pathTool.curveSettings.useEndValue);
                if (pathTool.curveSettings.useEndValue) {
                    pathTool.curveSettings.endValue = EditorGUILayout.FloatField("", pathTool.curveSettings.endValue);
                }
                EditorGUILayout.EndHorizontal();
                pathTool.curveSettings.min = EditorGUILayout.FloatField("Min", pathTool.curveSettings.min);
                pathTool.curveSettings.max = EditorGUILayout.FloatField("Max", pathTool.curveSettings.max);
                pathTool.curveSettings.minKeys = EditorGUILayout.IntField("Min Keys", pathTool.curveSettings.minKeys);
                pathTool.curveSettings.maxKeys = EditorGUILayout.IntField("Max Keys", pathTool.curveSettings.maxKeys);
                pathTool.curveSettings.offset = EditorGUILayout.CurveField("Offset", pathTool.curveSettings.offset);

                pathTool.curveSettings.curveNum = EditorGUILayout.IntSlider("Curve Num", pathTool.curveSettings.curveNum, 0, pathTool.GetMaxCurve() - 1);
                int maxIndex = pathTool.GetMaxIndex(pathTool.curveSettings.curveNum);
                if (pathTool.curveSettings.index > maxIndex) pathTool.curveSettings.index = maxIndex;
                if (maxIndex > 0) {
                    EditorGUILayout.BeginHorizontal();
                    pathTool.curveSettings.index = EditorGUILayout.IntSlider("Index", pathTool.curveSettings.index, 0, maxIndex - 1);
                    if (GUILayout.Button("Randomize Curve " + pathTool.curveSettings.index)) {
                        if (TryFindPathCreator()) {
                            pathTool.RandomizeCurve(pathTool.curveSettings.index);
                            if (pathTool.autoUpdateAll) {
                                pathTool.CreatePath();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Randomize All Curves")) {
                    if (TryFindPathCreator()) {
                        pathTool.RandomizeCurve();
                        pathTool.toUpdate = pathTool.curveSettings.curveNum;
                        pathTool.CreatePath();
                    }
                }
            }
        }

        using (var check = new EditorGUI.ChangeCheckScope()) {
            DrawDefaultInspector();

            if (check.changed) {
                if (!isSubscribed) {
                    TryFindPathCreator();
                    Subscribe();
                }

                if (pathTool.autoUpdateAll) {
                    pathTool.CreatePath();

                }
            }
        }

    }


    protected virtual void OnPathModified() {
        if (pathTool.autoUpdateAll) {
            pathTool.CreatePath();
        }
    }

    protected virtual void OnEnable() {
        pathTool = (PathComposite)target;
        pathTool.onDestroyed += OnToolDestroyed;

        if (TryFindPathCreator()) {
            Subscribe();
            OnPathModified();
        }
    }

    void OnToolDestroyed() {
        if (pathTool != null && pathTool.pathCreator != null) {
            pathTool.pathCreator.pathUpdated -= OnPathModified;
        }
    }


    protected virtual void Subscribe() {
        if (pathTool.pathCreator != null) {
            isSubscribed = true;
            pathTool.pathCreator.pathUpdated -= OnPathModified;
            pathTool.pathCreator.pathUpdated += OnPathModified;
        }
    }

    bool TryFindPathCreator() {
        // Try find a path creator in the scene, if one is not already assigned
        if (pathTool.pathCreator == null) {
            if (pathTool.GetComponent<PathCreator>() != null) {
                pathTool.pathCreator = pathTool.GetComponent<PathCreator>();
            }
            else if (FindObjectOfType<PathCreator>()) {
                pathTool.pathCreator = FindObjectOfType<PathCreator>();
            }
        }
        return pathTool.pathCreator != null;
    }
}
