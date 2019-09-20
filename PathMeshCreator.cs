using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PathMeshCreator : MonoBehaviour {

    public enum OutputType {
        row,
        column,
        border,
        grid,
        hole
    }

    [Serializable]
    public struct Input {
        public PathMeshCreator path;
        public int index;
        [Range(0, 1)]
        public float edgeStart;
        [Range(0, 1)]
        public float edgeEnd;
        public bool reverse;

        public Vector3[] Edge { get { return GetRange(path.outputs[index].Edge, edgeStart, edgeEnd); } }
        public Vector3[] Norm { get { return ConstructNorm(); } }
        public Vector3 this[int i] { get { return path.outputs[index][i]; } }
        public int Count { get { return path.outputs[index].Count; } }


        public Vector3[] GetRange(Vector3[] edge, float start, float end) {
            List<Vector3> list = new List<Vector3>(edge);
            List<Vector3> wallRange;

            if (start < end) {
                wallRange = new List<Vector3>(list).GetRange(Mathf.FloorToInt(start * list.Count), Mathf.CeilToInt((end - start) * list.Count));
            }
            else {
                wallRange = new List<Vector3>(list).GetRange(Mathf.FloorToInt(start * list.Count), list.Count - Mathf.FloorToInt(start * list.Count));
                wallRange.AddRange(new List<Vector3>(list).GetRange(list[0] == list[list.Count - 1] ? 1 : 0, Mathf.FloorToInt(end * list.Count)));
            }

            if (reverse) {
                wallRange.Reverse();
            }
            return wallRange.ToArray();
        }

        public Vector3[] ConstructNorm() {
            if (path.pathCreator != null && path.outputs[index].Edge.Length == path.pathCreator.path.NumVertices) return GetRange(path.pathCreator.path.normals, edgeStart, edgeEnd);

            List<Vector3> pathVerts = path.pathCreator != null ? new List<Vector3>(path.pathCreator.path.vertices) : null;
            List<Vector3> fullEdge = new List<Vector3>(path.outputs[index].Edge);
            List<Vector3> minEdge = new List<Vector3>(Edge);
            Vector3[] norm = new Vector3[minEdge.Count];
            bool closed = minEdge[0] == minEdge[minEdge.Count - 1];

            for (int i = 0; i < minEdge.Count; i++) {
                if (path.pathCreator != null && pathVerts.Contains(Utility.NoY(minEdge[i]))) {
                    norm[i] = path.pathCreator.path.normals[pathVerts.IndexOf(Utility.NoY(minEdge[i]))].normalized;
                }
                else {
                    int index = fullEdge.IndexOf(minEdge[i]);
                    Vector3 dir = (!closed && i == minEdge.Count - 1 ? fullEdge[index] : fullEdge[Utility.Mod(index + 1, fullEdge.Count)]) - (!closed && i == 0 ? fullEdge[index] : fullEdge[Utility.Mod(index - 1, fullEdge.Count)]);
                    norm[i] = Vector3.Cross(Vector3.ProjectOnPlane(Vector3.up, dir).normalized, dir).normalized;
                }
            }

            return norm;
        }
    }

    [Serializable]
    public class Output {
        public OutputType type;
        public AnimationCurve delta;
        public Vector2 gridMin;
        public Vector2 gridMax;

        public Vector3[] Edge { get { return edge.ToArray(); } }
        public Vector3 this[int i] { get { return edge[i]; } }
        public int Count { get { return isSet ? edge.Count : -1; } }
        public bool IsSet { get { return isSet; } }

        List<Vector3> edge = new List<Vector3>();
        List<float> heights = new List<float>();
        bool isSet;

        public void Clear() {
            edge.Clear();
            heights.Clear();
            isSet = true;
        }

        public void Add(Vector3 item, float height) {
            if (height == -1) edge.Add(item);
            else if (!edge.Contains(item)) {

                for (int i = 0; i < edge.Count; i++) {
                    if (heights[i] < height) {
                        edge.Insert(i, item);
                        heights.Insert(i, height);
                        return;
                    }
                }
                edge.Add(item);
                heights.Add(height);
            }
        }

        public void AddRange(List<Vector3> edge) {
            this.edge.AddRange(edge);
        }

        public void OrganizeEdge() {
            if (edge.Count > 0) {
                List<Vector3> sorted = new List<Vector3> {
                    edge[0]
                };
                edge.RemoveAt(0);

                while (edge.Count > 0) {
                    Vector3 closest = Utility.GetClosest(edge.ToArray(), sorted[sorted.Count - 1]);
                    sorted.Add(closest);
                    edge.Remove(closest);
                }

                edge.AddRange(sorted);
                edge.Add(sorted[0]);
            }
        }

        public void Reverse() {
            edge.Reverse();
        }

        public void Edit(Vector3 from, Vector3 to) {
            if (edge.Contains(from)) {
                edge[edge.IndexOf(from)] = to;
            }
        }
    }

    [HideInInspector]
    public bool needsUpdate;
    [HideInInspector]
    public bool foldout;
    [HideInInspector]
    public bool autoUpdate;
    [HideInInspector]
    public bool hide;
    [HideInInspector]
    public bool debug;
    public PathCreator pathCreator;
    public Input[] inputs;
    public Output[] outputs;
    public float depthScale;
    public AnimationCurve[] depthCurves;
    public bool flipMesh;

    [Header("Material settings")]
    public Material roomMaterial;
    public Vector2 textureTiling = Vector2.one;

    private void OnDrawGizmos() {
        if (debug && outputs != null) {
            foreach (Output output in outputs) {
                if (output.Count > 0) {
                    for (int i = 0; i < output.Count - 1; i++) {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(output[i], output[i + 1]);
                        Gizmos.color = Color.cyan;
                        Vector3 dir = output[i + 1] - output[i];
                        if (Vector3.Angle(dir, Vector3.up) > 30 && Vector3.Angle(dir, Vector3.up) < 150) {
                            Vector3 norm = Vector3.ProjectOnPlane(dir, Vector3.up).normalized * dir.magnitude * .5f;
                            Gizmos.DrawLine(output[i + 1], output[i + 1] - Quaternion.AngleAxis(20, Vector3.up) * norm);
                            Gizmos.DrawLine(output[i + 1], output[i + 1] - Quaternion.AngleAxis(-20, Vector3.up) * norm);
                        }
                        else {
                            Vector3 norm = Vector3.ProjectOnPlane(dir, Vector3.forward).normalized * dir.magnitude * .5f;
                            Gizmos.DrawLine(output[i + 1], output[i + 1] - Quaternion.AngleAxis(20, Vector3.forward) * norm);
                            Gizmos.DrawLine(output[i + 1], output[i + 1] - Quaternion.AngleAxis(-20, Vector3.forward) * norm);
                        }
                    }
                }
            }
        }
    }

    public abstract Mesh CreateMesh();

    public bool CheckIfNeedUpdate() {
        if (outputs == null) return true;
        foreach (Output output in outputs) {
            if (!output.IsSet) return true;
        }
        return false;
    }

    protected float EvaluateDepthCurve(float w, float height, bool closed = false) {
        if (depthCurves == null || depthCurves.Length == 0) return 0;

        if (closed) w %= 1;
        float width = w * (depthCurves.Length - (closed ? 0 : 1));
        int curve = (int)width;
        float delta = width - curve;

        return Mathf.Lerp(depthCurves[curve].Evaluate(height), depthCurves[(curve + 1) % depthCurves.Length].Evaluate(height), delta) * depthScale;
    }

    protected bool CheckConnection(int p) {
        return inputs != null && inputs.Length > p && inputs[p].path != null && inputs[p].path.outputs != null && inputs[p].index >= 0 && inputs[p].path.outputs.Length > inputs[p].index;
    }

    protected void ClearOutput() {
        if (outputs != null) {
            for (int i = 0; i < outputs.Length; i++) {
                outputs[i].Clear();
            }
        }
    }
    
    protected Vector3 CheckAddOutput(Vector3 item, float width, float widthDelta, float height, float heightDelta) {
        for (int i = 0; i < outputs.Length; i++) {
            if (outputs[i].type == OutputType.row) {
                float delta = outputs[i].delta.Evaluate(width);
                if (delta >= height - Mathf.Abs(heightDelta) && delta <= height + heightDelta) {
                    outputs[i].Add(item, width);
                }
            }
            else if (outputs[i].type == OutputType.column) {
                float delta = outputs[i].delta.Evaluate(height);
                if (delta >= width - widthDelta && delta <= width + widthDelta) {
                    outputs[i].Add(item, height);
                }
            }
            else if (outputs[i].type == OutputType.grid) {
                Vector2 min = new Vector2(Mathf.Clamp01(outputs[i].gridMin.x) - widthDelta, Mathf.Clamp01(outputs[i].gridMin.y) - heightDelta);
                Vector2 max = new Vector2(Mathf.Clamp01(outputs[i].gridMax.x) + widthDelta, Mathf.Clamp01(outputs[i].gridMax.y) + heightDelta);
                if (width > min.x && width < max.x && height > min.y && height < max.y) {
                    outputs[i].Add(item, height);
                }
            }
            /*
            else if (outputs[i].type == OutputType.hole) {
                Vector2 min = new Vector2(Mathf.Clamp01(outputs[i].gridMin.x) - widthDelta * 3, Mathf.Clamp01(outputs[i].gridMin.y) - heightDelta * 1);
                Vector2 max = new Vector2(Mathf.Clamp01(outputs[i].gridMax.x) + widthDelta * 2, Mathf.Clamp01(outputs[i].gridMax.y) + heightDelta * 1);

                if (IsInBounds(min, max, width, height, widthDelta, heightDelta)) outputs[i].Add(item, height + width * widthDelta);
            }*/
        }

        return item;
    }

    public bool IsInBounds(Vector2 min, Vector2 max, float width, float height) {
        if (width >= min.x && width <= max.x && height >= min.y && height <= max.y) {
            return true;
        }
        
        return false;
    }

    public bool IsInBounds(Vector2 min, Vector2 max, float width, float height, float widthDelta, float heightDelta) {
        if (IsInBounds(min, max, width, height)) {
            min = new Vector2(min.x + widthDelta * 2, min.y + heightDelta * 2);
            max = new Vector2(max.x - widthDelta * 2, max.y - heightDelta * 2);
            if (!IsInBounds(min, max, width, height)) {
                return true;
            }
        }

        return false;
    }

    protected void FinishOutputs(List<Vector3> border) {
        foreach (Output output in outputs) {
            if (output.type == OutputType.border) {
                output.AddRange(border);
            }
            else if (output.type == OutputType.hole) {
                output.OrganizeEdge();
            }
            else if (output.type == OutputType.row || output.type == OutputType.column) {
                if (border != null && border[0] == border[border.Count - 1] && output.Edge != null && output.Count > 0) output.Add(output[0], -1);

                output.Reverse();
            }
        }
    }

    protected List<Vector3> GetRange(List<Vector3> list, float start, float end) {
        List<Vector3> wallRange;

        if (start < end) {
            wallRange = new List<Vector3>(list).GetRange(Mathf.FloorToInt(start * list.Count), Mathf.CeilToInt((end - start) * list.Count));
        }
        else {
            wallRange = new List<Vector3>(list).GetRange(Mathf.FloorToInt(start * list.Count), list.Count - Mathf.FloorToInt(start * list.Count));
            wallRange.AddRange(new List<Vector3>(list).GetRange(list[0] == list[list.Count - 1] ? 1 : 0, Mathf.FloorToInt(end * list.Count)));
        }

        return wallRange;
    }

    public Vector3[] GetRange(Vector3[] list, float start, float end) {
        return GetRange(new List<Vector3>(list), start, end).ToArray();
    }

    protected void EditOutput(Vector3 from, Vector3 to) {
        foreach (Output output in outputs) {
            output.Edit(from, to);
        }
    }

    bool CheckQuadInHole(Vector3 item1, Vector3 item2, Vector3 item3, Vector3 item4, float width1, float width2, float widthDelta, float height1, float height2, float heightDelta) {
        foreach (Output output in outputs) {
            if (output.type == OutputType.hole) {
                Vector2 min = new Vector2(Mathf.Clamp01(output.gridMin.x) - widthDelta * 3, Mathf.Clamp01(output.gridMin.y) - heightDelta * 0);
                Vector2 max = new Vector2(Mathf.Clamp01(output.gridMax.x) + widthDelta * 2, Mathf.Clamp01(output.gridMax.y) + heightDelta * 0);

                if (IsInBounds(min, max, width1, height1) && IsInBounds(min, max, width2, height2)) {
                    if (height2 >= max.y - heightDelta * 2) {
                        output.Add(item4, height1 + width1 * widthDelta);
                    }
                    if (height1 <= min.y + heightDelta * 2) {
                        output.Add(item1, height1 + width1 * widthDelta);
                    }
                    if (width2 >= max.x - widthDelta * 2) {
                        output.Add(item3, height1 + width1 * widthDelta);
                    }
                    if (width1 <= min.x + widthDelta * 2) {
                        output.Add(item2, height1 + width1 * widthDelta);
                    }
                    return true;
                }
            }
        }

        return false;
    }

    protected void AddToMesh(Utility.Polygon mesh, List<Vector3> row1, List<Vector3> row2, int rowI, float widthMax, bool end = false) {

        float widthDelta = 1f / widthMax * .5f;
        float heightDelta = 1f / row1.Count * .5f;
        float width1 = (rowI - 1) / widthMax;
        float width2 = (rowI) / widthMax;

        if (row1.Count == row2.Count) {
            for (int j = 0; j < row2.Count - 1 && j < row1.Count - 1; j++) {
                float height1 = (float)(j) / (row1.Count);
                float height2 = (float)(j + 1) / (row2.Count);
                if (!CheckQuadInHole(row1[j], row1[j + 1], row2[j], row2[j + 1], width1, width2, widthDelta, height2, height2, heightDelta)) {
                    if ((rowI + j) % 2 == 0) {
                        mesh.AddQuad(row1[j], row1[j + 1], row2[j], row2[j + 1], new Vector2(width1, height1), new Vector2(width1, height2), new Vector2(width2, height1), new Vector2(width2, height2), flipMesh);
                    }
                    else {
                        mesh.AddQuad(row1[j + 1], row2[j + 1], row1[j], row2[j], new Vector2(width1, height2), new Vector2(width2, height2), new Vector2(width1, height1), new Vector2(width2, height1), flipMesh);
                    }
                }
            }
        }
        else {
            if (end) {
                if (Vector3.Distance(row1[0], row2[0]) > Vector3.Distance(row1[0], row2[row2.Count - 1])) row2.Reverse();
            }
            else {
                if (Vector3.Distance(row1[0], row2[0]) > Vector3.Distance(row1[row1.Count - 1], row2[0])) row1.Reverse();
            }

            int i = 0, j = 0;
            Vector3 start1 = row1[0], start2 = row2[0];
            while (i < row1.Count - 1 && j < row2.Count - 1) {
                float height1 = (float)(i + 1) / (row1.Count);
                float height2 = (float)(j + 1) / (row2.Count);

                if (!CheckQuadInHole(row1[i], row1[i + 1], row2[j], row2[j + 1], width1, width2, widthDelta, height1, height2, heightDelta)) {
                    if (Vector3.Distance(row1[i], row2[j + 1]) < Vector3.Distance(row2[j], row1[i + 1])) {
                        ++j;
                        mesh.AddTri(start1, row2[j], start2, new Vector2(width1, height1), new Vector2(width2, height2), new Vector2(width2, height1), flipMesh);
                        start2 = row2[j];
                    }
                    else {
                        ++i;
                        mesh.AddTri(start1, row1[i], start2, new Vector2(width1, height1), new Vector2(width1, height2), new Vector2(width2, height1), flipMesh);
                        start1 = row1[i];
                    }
                }
                else {
                    ++j;
                    ++i;
                }
            }
            if (i < row1.Count - 1) {
                while (i < row1.Count - 1) {
                    ++i;
                    float height1 = (float)(i + 1) / (row1.Count);
                    float height2 = (float)(j + 1) / (row2.Count);
                    if (!CheckQuadInHole(row1[i - 1], row1[i], row2[j], row2[j], width1, width2, widthDelta, height1, height2, heightDelta)) {
                        mesh.AddTri(start1, row1[i], start2, new Vector2(width1, height1), new Vector2(width1, height2), new Vector2(width2, height1), flipMesh);
                    }
                    start1 = row1[i];
                }
            }
            else if (j < row2.Count - 1) {
                while (j < row2.Count - 1) {
                    ++j;
                    float height1 = (float)(i + 1) / (row1.Count);
                    float height2 = (float)(j + 1) / (row2.Count);
                    if (!CheckQuadInHole(row1[i], row1[i], row2[j - 1], row2[j], width1, width2, widthDelta, height1, height2, heightDelta)) {
                        mesh.AddTri(start1, row2[j], start2, new Vector2(width1, height1), new Vector2(width2, height2), new Vector2(width2, height1), flipMesh);
                    }
                    start2 = row2[j];
                }
            }
        }
    }
}
