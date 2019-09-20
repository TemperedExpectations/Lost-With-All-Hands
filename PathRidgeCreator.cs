using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PathRidgeCreator : PathMeshCreator {

    [Header("Ridge Settings")]
    public int gridCount = 1;
    public float widthScale = 1;
    public AnimationCurve widthCurve;
    public float heighOffset;
    public AnimationCurve heightCurve;

    List<LineSegment> debugList1;

    public override Mesh CreateMesh() {
        if (gridCount < 1) gridCount = 1;

        debugList1 = new List<LineSegment>();
        ClearOutput();

        if (pathCreator != null) {

            Utility.Polygon poly = GetRidgeWalkway(pathCreator.path.vertices, pathCreator.path.normals, CheckConnection(0) ? inputs[0].Edge : null, CheckConnection(1) ? inputs[1].Edge : null);

            Mesh mesh = new Mesh() {
                vertices = poly.verts.ToArray(),
                triangles = poly.tris.ToArray(),
                uv = poly.uvs.Select(u => new Vector2(u.y, u.x)).ToArray()
            };
            mesh.RecalculateNormals();

            FinishOutputs(null);

            return mesh;
        }
        else if (inputs != null) {
            List<List<Vector3>> edges = new List<List<Vector3>>();
            for (int i = 0; i < inputs.Length; i++) {
                if (CheckConnection(i)) {
                    edges.Add(new List<Vector3>(inputs[i].Edge));
                }
                else {
                    edges.Add(null);
                }
            }

            if (edges.Count < 2 || edges[0] == null || edges[0].Count < 2 || edges[1] == null || edges[1].Count < 2) return new Mesh();
            
            int minLength = Mathf.Min(edges[0].Count, edges[1].Count);
            edges[0] = edges[0].GetRange(0, minLength);
            edges[1] = edges[1].GetRange(0, minLength);

            Utility.Polygon poly = GetRidgeBounded(edges[0].ToArray(), edges[1].ToArray(), edges.Count > 2 && edges[2] != null ? edges[2].ToArray() : null, edges.Count > 3 && edges[3] != null ? edges[3].ToArray() : null);

            Mesh mesh = new Mesh() {
                vertices = poly.verts.ToArray(),
                triangles = poly.tris.ToArray(),
                uv = poly.uvs.ToArray()
            };
            mesh.RecalculateNormals();

            FinishOutputs(null);

            return mesh;
        }

        return new Mesh();
    }

    Utility.Polygon GetRidgeBounded(Vector3[] edge1, Vector3[] edge2, Vector3[] startEdge, Vector3[] endEdge) {
        Utility.Polygon mesh = new Utility.Polygon();

        List<List<Vector3>> rows = new List<List<Vector3>>();
        if (startEdge != null) {
            rows.Add(new List<Vector3>(startEdge));
        }
        for (int i = 0; i <= edge1.Length - 1; i++) {
            int rowI = rows.Count;
            rows.Add(GetBoundedRow(edge1[i], i != edge1.Length - 1 ? edge1[i + 1] : edge1[i] * 2 - edge1[i - 1], edge2[i], i / (edge1.Length - 1f), 1 / (edge1.Length - 1f)));

            if (rowI > 0) {
                AddToMesh(mesh, rows[rowI - 1], rows[rowI], rowI, edge1.Length);
            }
        }
        if (endEdge != null) {
            AddToMesh(mesh, rows[rows.Count - 1], new List<Vector3>(endEdge), rows.Count, (rows.Count + .5f) / (rows.Count + 1), true);
        }

        return mesh;
    }

    List<Vector3> GetBoundedRow(Vector3 edge1, Vector3 next, Vector3 edge2, float dis, float delta) {
        Vector3 upAxis = Vector3.Cross(next - edge1, edge2 - edge1).normalized;
        debugList1.Add(new LineSegment(edge1, edge1 + upAxis * 5));

        List<Vector3> row = new List<Vector3>();
        for (float i = 0; i <= gridCount; i++) {
            row.Add(Vector3.Lerp(edge1, edge2, i / gridCount) + upAxis * EvaluateDepthCurve(dis, i / gridCount, false));
        }

        for (int i = 0; i < row.Count; i++) {
            CheckAddOutput(row[i], dis, delta * .5f, (float)i / (gridCount), 1f / (gridCount) * .5f);
        }

        return row;
    }

    Utility.Polygon GetRidgeWalkway(Vector3[] center, Vector3[] normals, Vector3[] startEdge, Vector3[] endEdge) {
        Utility.Polygon mesh = new Utility.Polygon();

        List<List<Vector3>> rows = new List<List<Vector3>>();
        if (startEdge != null) {
            rows.Add(new List<Vector3>(startEdge));
        }
        for (int i = 0; i < center.Length; i++) {
            int rowI = rows.Count;
            rows.Add(GetWalkwayRow(center[i], i != center.Length - 1 ? center[i + 1] : center[i] * 2 - center[i - 1], normals[i], i / (center.Length - 1f), 1 / (center.Length - 1f)));

            if (rowI > 0 && rows[rowI - 1].Count > 0) {
                AddToMesh(mesh, rows[rowI - 1], rows[rowI], rowI, center.Length);
            }
        }
        if (endEdge != null && endEdge.Length > 0) {
            AddToMesh(mesh, rows[rows.Count - 1], new List<Vector3>(endEdge), rows.Count, (rows.Count + .5f) / (rows.Count + 1), true);
        }

        return mesh;
    }

    List<Vector3> GetWalkwayRow(Vector3 centerPoint, Vector3 nextPoint, Vector3 normal, float dis, float delta) {
        Vector3 upAxis = Vector3.Cross(nextPoint - centerPoint, normal).normalized;
        debugList1.Add(new LineSegment(centerPoint, centerPoint + upAxis * 5));
        Vector3 height = Vector3.up * (pathCreator != null ? heighOffset * heightCurve.Evaluate(dis) : 0);

        List<Vector3> row = new List<Vector3>() {
            centerPoint + upAxis * EvaluateDepthCurve(dis, .5f, false) + height
        };

        float width = Mathf.Max((widthCurve != null ? widthCurve.Evaluate(dis) : 1) * widthScale, .1f);
        if (width == 0) width = 1;

        for (float i = width / gridCount; i <= width; i += width / gridCount) {
            row.Add(centerPoint + normal * i + upAxis * EvaluateDepthCurve(dis, i / width * .5f + .5f, false) + height);
        }
        row.Reverse();
        for (float i = width / gridCount; i <= width; i += width / gridCount) {
            row.Add(centerPoint - normal * i + upAxis * EvaluateDepthCurve(dis, .5f - i / width * .5f, false) + height);
        }
        
        for (int i = 0; i < row.Count; i++) {
            CheckAddOutput(row[i], dis, delta * .5f, i / (gridCount * 2f), 1 / (gridCount * 2f) *.5f);
        }

        return row;
    }
}
