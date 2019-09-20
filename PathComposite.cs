using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineSegment {
    public Vector3 v1;
    public Vector3 v2;

    public LineSegment(Vector3 v1, Vector3 v2) {
        this.v1 = v1;
        this.v2 = v2;
    }

    public bool GetOpposite(Vector3 vIn, out Vector3 vOut) {
        if (vIn == v1) {
            vOut = v2;
            return true;
        }
        if (vIn == v2) {
            vOut = v1;
            return true;
        }
        vOut = Vector3.zero;
        return false;
    }

    public override bool Equals(object obj) {
        if (!(obj is LineSegment)) {
            return false;
        }

        var segment = (LineSegment)obj;
        return (EqualityComparer<Vector3>.Default.Equals(v1, segment.v1) && EqualityComparer<Vector3>.Default.Equals(v2, segment.v2)) ||
            (EqualityComparer<Vector3>.Default.Equals(v1, segment.v2) && EqualityComparer<Vector3>.Default.Equals(v2, segment.v1));
    }

    public override int GetHashCode() {
        var hashCode = 1763187145;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(v1);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(v2);
        return hashCode;
    }
}

[System.Serializable]
public class MeshEdgeGroup {
    public bool foldout;
    public List<MeshFilter> edgeGroup;

    List<Vector3> vertexList;

    public MeshEdgeGroup() {
        foldout = true;
        edgeGroup = new List<MeshFilter>();
    }

    public void RenormalizeMeshes() {
        vertexList = new List<Vector3>();
        List<List<Vector3>> vertsList = new List<List<Vector3>>();
        List<List<Vector3>> normsList = new List<List<Vector3>>();

        for (int v1 = 0; v1 < edgeGroup.Count; v1++) {
            if (edgeGroup[v1] != null) {
                vertsList.Add(new List<Vector3>(edgeGroup[v1].sharedMesh.vertices));
                normsList.Add(new List<Vector3>(edgeGroup[v1].sharedMesh.normals));
            }
            else {
                vertsList.Add(new List<Vector3>());
                normsList.Add(new List<Vector3>());
            }
        }

        for (int v1 = 0; v1 < vertsList.Count - 1; v1++) {
            for (int i = 0; i < vertsList[v1].Count; i++) {
                if (!vertexList.Contains(vertsList[v1][i])) {
                    Vector3 newNormal = normsList[v1][i];
                    List<int> indices = new List<int>() {
                        i
                    };
                    int count = 0;

                    for (int v2 = v1 + 1; v2 < edgeGroup.Count; v2++) {
                        ++count;
                        indices.Add(-1);
                        if (vertsList[v2].Contains(vertsList[v1][i])) {
                            indices[count] = (vertsList[v2].IndexOf(vertsList[v1][i]));
                            newNormal += normsList[v2][indices[count]];
                        }
                    }

                    if (count > 0) {
                        newNormal.Normalize();

                        for (int j = 0; j < indices.Count; j++) {
                            if (indices[j] != -1) {
                                normsList[j + v1][indices[j]] = newNormal;
                            }
                        }

                        vertexList.Add(vertsList[v1][i]);
                    }
                }
            }
        }

        for (int v1 = 0; v1 < edgeGroup.Count; v1++) {
            if (edgeGroup[v1] != null) {
                edgeGroup[v1].sharedMesh.SetNormals(normsList[v1]);
            }
        }
    }
}

public class PathComposite : PathSceneTool {

    [HideInInspector]
    public bool creatorsFoldout;
    [HideInInspector]
    public PathMeshCreator[] meshCreators;
    [HideInInspector]
    public bool edgeGroupsFoldout;
    [HideInInspector]
    public List<MeshEdgeGroup> meshEdgeGroups = new List<MeshEdgeGroup>();
    [HideInInspector]
    public int toUpdate = -1;

    List<MeshFilter> meshFilters = new List<MeshFilter>();
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    List<MeshCollider> meshColliders = new List<MeshCollider>();

    Vector3[] borderVerts;

    public override int GetMaxIndex(int curveNum) {
        if (meshCreators[curveNum] != null && meshCreators[curveNum].depthCurves != null) return meshCreators[curveNum].depthCurves.Length;
        else return 0;
    }

    public override int GetMaxCurve() {
        if (meshCreators == null) return 0;
        return meshCreators.Length;
    }

    public override void RandomizeCurve(int index = -1) {
        if (curveSettings.minKeys > 0 && meshCreators[curveSettings.curveNum] != null) {
            if (index != -1) {
                meshCreators[curveSettings.curveNum].depthCurves[index] = GetCurve();
            }
            else {
                for (int i = 0; i < meshCreators[curveSettings.curveNum].depthCurves.Length; i++) {
                    meshCreators[curveSettings.curveNum].depthCurves[i] = GetCurve();
                }
            }
        }
    }

    AnimationCurve GetCurve() {
        int numKeys = Random.Range(curveSettings.minKeys, curveSettings.maxKeys + 1);

        AnimationCurve curveFrames = new AnimationCurve();
        for (int i = 0; i < numKeys; i++) {
            if (i == 0 && curveSettings.useStartValue) curveFrames.AddKey(new Keyframe(0, curveSettings.startValue));
            else if (i == numKeys - 1 && curveSettings.useEndValue) curveFrames.AddKey(new Keyframe(1, curveSettings.endValue));
            else {
                float time = i / (numKeys - 1f);
                float value = Random.Range(curveSettings.min, curveSettings.max) + curveSettings.offset.Evaluate(time);
                curveFrames.AddKey(new Keyframe(time, value));
            }
        }

        return curveFrames;
    }

    protected override void PathUpdated() {
        PathUpdated(toUpdate);
        toUpdate = -1;
    }

    void PathUpdated(int index) {
        if (pathCreator != null && meshCreators != null) {
            AssignComponents();

            if (index == -1) {
                for (int i = 0; i < meshCreators.Length; i++) {
                    if (meshCreators[i] != null) {
                        if (meshCreators[i].hide) {
                            meshRenderers[i].enabled = false;
                            if (meshColliders.Count > i && meshColliders[i] != null) meshColliders[i].enabled = false;
                        }
                        else {
                            if (meshRenderers[i] != null) meshRenderers[i].enabled = true;
                            if (meshFilters[Mathf.Min(i, meshFilters.Count - 1)] != null) meshFilters[Mathf.Min(i, meshFilters.Count - 1)].sharedMesh = meshCreators[i].CreateMesh();
                            else meshCreators[i].CreateMesh();
                            if (meshColliders.Count > i && meshColliders[i] != null) {
                                meshColliders[i].enabled = true;
                                meshColliders[i].sharedMesh = meshFilters[Mathf.Min(i, meshFilters.Count - 1)].sharedMesh;
                            }
                        }
                    }
                }
            }
            else if (meshCreators[index] != null) {
                if (meshCreators[index].hide) {
                    if (meshRenderers[index] != null) meshRenderers[index].enabled = false;
                    if (meshColliders.Count > index && meshColliders[index] != null) meshColliders[index].enabled = false;
                }
                else {
                    if (meshRenderers[index] != null) meshRenderers[index].enabled = true;

                    if (meshCreators[index].inputs != null) {
                        foreach (PathMeshCreator.Input pathMesh in meshCreators[index].inputs) {
                            if (pathMesh.path != null && pathMesh.path.CheckIfNeedUpdate()) PathUpdated(new List<PathMeshCreator>(meshCreators).IndexOf(pathMesh.path));
                        }
                    }
                    if (meshFilters[Mathf.Min(index, meshFilters.Count - 1)] != null) meshFilters[Mathf.Min(index, meshFilters.Count - 1)].sharedMesh = meshCreators[index].CreateMesh();
                    else meshCreators[index].CreateMesh();
                    if (createCollider && meshColliders.Count > index && meshColliders[index] != null) {
                        meshColliders[index].enabled = true;
                        meshColliders[index].sharedMesh = meshFilters[Mathf.Min(index, meshFilters.Count - 1)].sharedMesh;
                    }
                }
            }
        }
    }

    public void UpdateEdgeGroups(int toUpdate) {
        if (toUpdate == -1) {
            for (int i = 0; i < meshEdgeGroups.Count; i++) {
                meshEdgeGroups[i].RenormalizeMeshes();
            }
        }
        else {
            meshEdgeGroups[toUpdate].RenormalizeMeshes();
        }
    }

    void AssignComponents() {
        meshFilters.Clear();
        meshRenderers.Clear();
        meshColliders.Clear();

        if (meshCreators != null) {
            for (int i = 0; i < meshCreators.Length; i++) {
                if (meshCreators[i] != null) {
                    AssignMeshComponents(i);
                    AssignMaterials(i);
                }
            }
        }
    }

    // Add MeshRenderer and MeshFilter components to this gameobject if not already attached
    void AssignMeshComponents(int index) {
        if (meshCreators[index].GetType() == typeof(PathOutputter)) {
            meshRenderers.Add(null);
            meshFilters.Add(null);
            meshColliders.Add(null);
            return;
        }

        // Find/creator mesh holder object in children
        string meshHolderName = "Mesh Holder " + (index + 1) + ": " + meshCreators[index].GetType();
        Transform meshHolder = transform.Find(meshHolderName);
        if (meshHolder == null) {
            meshHolder = new GameObject(meshHolderName).transform;
            meshHolder.transform.parent = transform;
            meshHolder.transform.localPosition = Vector3.zero;
        }

        //meshHolder.transform.position = Vector3.zero;
        meshHolder.transform.rotation = Quaternion.identity;

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter>()) {
            meshHolder.gameObject.AddComponent<MeshFilter>();
        }
        if (!meshHolder.GetComponent<MeshRenderer>()) {
            meshHolder.gameObject.AddComponent<MeshRenderer>();
        }
        if (createCollider && !meshHolder.GetComponent<MeshCollider>()) {
            meshHolder.gameObject.AddComponent<MeshCollider>();
        }

        meshHolder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        meshRenderers.Add(meshHolder.GetComponent<MeshRenderer>());
        meshFilters.Add(meshHolder.GetComponent<MeshFilter>());
        meshColliders.Add(meshHolder.GetComponent<MeshCollider>());
    }

    void AssignMaterials(int index) {
        if (meshCreators[index].roomMaterial != null && meshCreators[index].GetType() != typeof(PathOutputter)) {
            meshRenderers[Mathf.Min(index, meshRenderers.Count - 1)].sharedMaterials = new Material[] { meshCreators[index].roomMaterial };
            meshRenderers[Mathf.Min(index, meshRenderers.Count - 1)].sharedMaterials[0].mainTextureScale = meshCreators[index].textureTiling;
        }
    }
}
