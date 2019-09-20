using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathWallCreator : PathMeshCreator {

    [Header("Wall Settings")]
    public bool closed;
    public float gridScale = 1;
    public float wallHeight;
    public AnimationCurve heightCurve;
    [Range(1, 8)]
    public int density = 1;
    public float edgeNormMult;
    public AnimationCurve slantMult;
    
    bool pathClosed = false;

    public override Mesh CreateMesh() {
        //print("Updating " + typeof(PathWallCreator));
        ClearOutput();

        List<Vector3[]> setWall = new List<Vector3[]>();
        Vector3[] setNorm = new Vector3[0];
        if (closed) {
            List<Vector3> edgeList = new List<Vector3>();
            List<Vector3> normList = new List<Vector3>();
            if (pathCreator != null) {
                edgeList.AddRange(pathCreator.path.vertices);
                normList.AddRange(pathCreator.path.normals);
            }
            for (int i = 0; i < inputs.Length; i++) {
                if (CheckConnection(i)) {
                    AddEdge(edgeList, normList, inputs[i].Edge, inputs[i].Norm);
                }
            }
            if (pathCreator != null && edgeList.Count > pathCreator.path.NumVertices) {
                edgeList.Add(edgeList[0]);
                normList.Add(normList[0]);
            }
            
            
            if (pathClosed = Vector3.Distance(edgeList[0], edgeList[edgeList.Count - 1]) <= .001f) {
                edgeList[edgeList.Count - 1] = edgeList[0];
            }

            setWall.Add(edgeList.ToArray());
            setNorm = normList.ToArray();
        }
        else {
            if (pathCreator != null) {
                setWall.Add(pathCreator.path.vertices);
                setNorm = pathCreator.path.normals;
                pathClosed = pathCreator.bezierPath.IsClosed;
            }
            for (int i = 0; i < inputs.Length; i++) {
                if (CheckConnection(i)) {
                    setWall.Add(inputs[i].Edge);
                    if (setWall.Count == 1) {
                        setNorm = inputs[i].Norm;
                        if (setWall[0].Length == setNorm.Length + 1) {
                            setWall[0] = new List<Vector3>(setWall[0]).GetRange(1, setNorm.Length).ToArray();
                        }
                    }
                }
                else {
                    setWall.Add(null);
                }
            }
        }

        if (wallHeight != 0 && gridScale > 0.01f && setWall.Count > 0 && setWall[0] != null) {
            if (pathClosed = setWall[0][0] == setWall[0][setWall[0].Length - 1]) {
                setNorm[0] = ((setNorm[0] + setNorm[setNorm.Length - 1]) * .5f).normalized;
                setNorm[setNorm.Length - 1] = setNorm[0];
            }
            
        }
        else {
            print("No path: " + typeof(PathWallCreator));
            return new Mesh();
        }

        Utility.Polygon poly = GetWall(setWall[0], setNorm, setWall.Count > 1 ? setWall[1] : null, setWall.Count > 2 ? setWall[2] : null, setWall.Count > 3 ? setWall[3] : null);
        
        Mesh mesh = new Mesh() {
            vertices = poly.verts.ToArray(),
            triangles = poly.tris.ToArray(),
            uv = poly.uvs.ToArray()
        };
        mesh.RecalculateNormals();

        FinishOutputs(new List<Vector3>(setWall[0]));

        return mesh;
    }

    void AddEdge(List<Vector3> curList, List<Vector3> curNorm, Vector3[] addList, Vector3[] addNorm) {
        if (curList.Count > 0 && curList[curList.Count - 1] == addList[0]) {
            curList.RemoveAt(curList.Count - 1);
            addNorm[0] = ((curNorm[curNorm.Count - 1] + addNorm[0]) * .5f).normalized;
            curNorm.RemoveAt(curNorm.Count - 1);
        }

        curList.AddRange(addList);
        curNorm.AddRange(addNorm);
    }

    Utility.Polygon GetWall(Vector3[] wallBase, Vector3[] normals, Vector3[] startEdge, Vector3[] endEdge, Vector3[] topEdge) {
        Utility.Polygon mesh = new Utility.Polygon();

        float maxVerts = (wallBase.Length - 1) * density;

        List<List<Vector3>> rows = new List<List<Vector3>> {
            GetWallRow(wallBase[0], Vector3.zero, normals[0], 0, 1 / maxVerts)
        };
        if (topEdge != null) {
            if (heightCurve.Evaluate(0) == 0) {
                rows[0].Add(CheckAddOutput(topEdge[0], 0, 1 / maxVerts, 1.1f, 0));
            }
            else {
                rows[0].Add(CheckAddOutput(Utility.GetClosest(topEdge, rows[0][rows[0].Count - 1]), 0, 1 / maxVerts, 1.1f, gridScale / Mathf.Abs(wallHeight * heightCurve.Evaluate(0)) * .5f));
            }
        }
        if (startEdge != null && startEdge.Length > 0) {
            if (startEdge[0] == rows[0][0]) {
                EditOutput(rows[0][rows[0].Count - 1], startEdge[startEdge.Length - 1]);
                rows[0][rows[0].Count - 1] = startEdge[startEdge.Length - 1];
            }
            AddToMesh(mesh, new List<Vector3>(startEdge), rows[0], 0, maxVerts);
        }
        for (int i = 0; i < wallBase.Length - 1; i++) {
            for (float d = 1; d <= density; d++) {
                int rowIndex = rows.Count;
                if (rowIndex == maxVerts && pathClosed) {
                    rows.Add(rows[0]);
                }
                else {
                    float rowDelta = d / density;
                    Vector3 edgeNorm = Vector3.Lerp(normals[i], normals[i + 1], rowDelta).normalized;
                    Vector3 normOffset = edgeNorm * (1 - Mathf.Pow(2 * (rowDelta - .5f), 2)) * Mathf.Clamp(Vector3.SignedAngle(edgeNorm, normals[i], Vector3.up), -10, 10) * Mathf.Deg2Rad * edgeNormMult;
                    Vector3 edgeVert = Vector3.Lerp(wallBase[i], wallBase[i + 1], rowDelta);
                    rows.Add(GetWallRow(edgeVert, normOffset, edgeNorm, rowIndex / maxVerts, 1 / maxVerts));
                    if (topEdge != null) {
                        rows[rowIndex].Add(CheckAddOutput(Utility.GetClosest(topEdge, rows[rowIndex][rows[rowIndex].Count - 1]), rowIndex / maxVerts, 1 / maxVerts, 1.1f, gridScale / Mathf.Abs(wallHeight * heightCurve.Evaluate(rowIndex / maxVerts)) * .5f));
                    }
                }

                if (rowIndex > 0) {
                    if (rowIndex == maxVerts && endEdge != null && endEdge.Length > 0 && endEdge[0] == rows[rows.Count - 1][0]) {
                        EditOutput(rows[rows.Count - 1][rows[rows.Count - 1].Count - 1], endEdge[endEdge.Length - 1]);
                        rows[rows.Count - 1][rows[rows.Count - 1].Count - 1] = endEdge[endEdge.Length - 1];
                    }
                    AddToMesh(mesh, rows[rowIndex - 1], rows[rowIndex], rowIndex, maxVerts);
                }
            }
        }
        if (endEdge != null && endEdge.Length > 0) {
            AddToMesh(mesh, rows[rows.Count - 1], new List<Vector3>(endEdge), (int)maxVerts + 1, maxVerts, true);
        }

        return mesh;
    }

    List<Vector3> GetWallRow(Vector3 baseEdge, Vector3 wallOffset, Vector3 normal, float widthDst, float widthDelta) {
        List<Vector3> verts = new List<Vector3>();
        float height = wallHeight * heightCurve.Evaluate(widthDst);
        float heightLeft = 0;
        float slant = slantMult.Evaluate(widthDst);
        while (height > 0 ? heightLeft < height : heightLeft > height) {
            float depth = EvaluateDepthCurve(widthDst, heightLeft / height, pathClosed) - slant * heightLeft;
            Vector3 offset = heightLeft == 0 ? Vector3.zero : (normal * depth + wallOffset);

            verts.Add(CheckAddOutput(baseEdge + offset + Vector3.up * heightLeft, widthDst, widthDelta * .5f, Mathf.Abs(heightLeft / height), gridScale / Mathf.Abs(height) * .5f));

            heightLeft += height > 0 ? gridScale : -gridScale;
        }
        verts.Add(CheckAddOutput(baseEdge + normal * (EvaluateDepthCurve(widthDst, 1, pathClosed) - slant * heightLeft) + Vector3.up * height, widthDst, widthDelta * .5f, 1, gridScale / Mathf.Abs(height) * .5f));

        return verts;
    }
}
