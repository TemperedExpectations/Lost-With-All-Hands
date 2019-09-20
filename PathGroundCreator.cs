using Geometry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PathGroundCreator : PathMeshCreator {

    [Header("Floor Settings")]
    public bool flipBorder;
    public bool closed;
    public float gridScale = 1;
    public float heightOffset;

    float pathHeight;
    Bounds bounds;
    List<Vector3> outerRing;
    List<Vector3> innerRing;
    List<LineSegment> lineRing;
    List<Vector3> debugList;
    List<LineSegment> debugSegList;
    List<Vector3> leaveHeightsUnchanged;

    /*
    private void OnDrawGizmos() {
        if (debug) {
            if (debugSegList != null) {
                for (int i = 0; i < debugSegList.Count; i++) {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(debugSegList[i].v1, debugSegList[i].v2);
                }
            }
        }
    }
    */

    public override Mesh CreateMesh() {
        //print("Updating " + typeof(PathGroundCreator));
        debugSegList = new List<LineSegment>();
        leaveHeightsUnchanged = new List<Vector3>();
        if (gridScale <= 0) gridScale = 1f;
        ClearOutput();
        if (pathCreator != null) {
            pathHeight = pathCreator.bezierPath.HeightOffset;
            if (!pathCreator.bezierPath.IsClosed && CheckConnection(0)) {
                outerRing = new List<Vector3>(pathCreator.path.vertices);
                for (int i = 0; i < inputs.Length; i++) {
                    if (CheckConnection(i)) {
                        List<Vector3> edgeList = new List<Vector3>(inputs[i].Edge);
                        leaveHeightsUnchanged.AddRange(edgeList);
                        AddEdge(outerRing, edgeList);
                    }
                }
                ClosePath(outerRing, gridScale, pathHeight);
                bounds = GetBounds(outerRing);
            }
            else {
                outerRing = new List<Vector3>(pathCreator.path.vertices);
                bounds = pathCreator.path.bounds;
            }
        }
        else if (CheckConnection(0)) {
            outerRing = new List<Vector3>();
            for (int i = 0; i < inputs.Length; i++) {
                if (CheckConnection(i)) {
                    if (outerRing.Count == 0) {
                        outerRing.AddRange(new List<Vector3>(inputs[i].Edge));
                    }
                    else {
                        AddEdge(outerRing, new List<Vector3>(inputs[i].Edge));
                    }
                }
            }
            if (outerRing.Count == 0) {
                print("Failed Ground Build");
                return new Mesh();
            }

            leaveHeightsUnchanged.AddRange(outerRing);
            if (closed)
                outerRing.Add(outerRing[0]);
            else
                ClosePath(outerRing, gridScale, 0);
            
            bounds = GetBounds(outerRing);
        }
        else {
            return new Mesh();
        }

        if (flipBorder) outerRing.Reverse();

        List<Vector3> validQuads = new List<Vector3>();
        List<Vector3> validTris = new List<Vector3>();
        SetInnerRing(outerRing.ToArray(), validQuads, validTris);
        
        lineRing = SetLineSegmentRing(outerRing, innerRing);
        Utility.Polygon poly = SetOuterQuadsAndTris(lineRing);
        poly.Combine(AddInnerQuadsAndTris(validQuads, validTris));
        poly.verts = ModifyGroundHeights(poly.verts);

        Mesh mesh = new Mesh() {
            vertices = poly.verts.ToArray(),
            triangles = poly.tris.ToArray(),
            uv = poly.uvs.ToArray()
        };
        mesh.RecalculateNormals();

        FinishOutputs(ModifyGroundHeights(outerRing));

        return mesh;
    }

    void ClosePath(List<Vector3> path, float scale, float height) {
        Vector3 startPoint = Utility.NoY(path[0]) + Vector3.up * height;
        Vector3 endPoint = Utility.NoY(path[path.Count - 1]) + Vector3.up * height;
        if (startPoint != endPoint && Vector3.Distance(startPoint, endPoint) > scale * 1.5f) {
            float dis = Vector3.Distance(startPoint, endPoint);
            for (float i = scale; i < dis; i += scale) {
                Vector3 midPoint = Vector3.Lerp(endPoint, startPoint, i / dis);
                if (closed && leaveHeightsUnchanged.Contains(path[0])) {
                    midPoint += Vector3.up * path[0].y;
                    leaveHeightsUnchanged.Add(midPoint);
                }
                else if (closed && leaveHeightsUnchanged.Contains(path[path.Count - 1])) {
                    midPoint += Vector3.up * path[path.Count - 1].y;
                    leaveHeightsUnchanged.Add(midPoint);
                }
                path.Add(midPoint);
            }
            path.Add(path[0]);
        }
    }

    void AddEdge(List<Vector3> curList, List<Vector3> addList) {
        if (addList[0] == addList[addList.Count - 1]) addList.RemoveAt(addList.Count - 1);
        Vector3 c1 = curList[0], c2 = curList[curList.Count - 1], e1 = addList[0], e2 = addList[addList.Count - 1];
        if (c2 == e1) {
            addList.RemoveAt(0);
            curList.AddRange(addList);
        }
        else if (c2 == e2) {
            addList.Reverse();
            addList.RemoveAt(0);
            curList.AddRange(addList);
        }
        else if (c1 == e1) {
            curList.Reverse();
            addList.RemoveAt(0);
            curList.AddRange(addList);
        }
        else if (c1 == e2) {
            curList.Reverse();
            addList.Reverse();
            addList.RemoveAt(0);
            curList.AddRange(addList);
        }
        else {
            List<float> dists = new List<float>() { Vector3.Distance(c1, e1), Vector3.Distance(c1, e2), Vector3.Distance(c2, e1), Vector3.Distance(c2, e2) };
            float min = Mathf.Min(dists.ToArray());
            switch (dists.IndexOf(min)) {
                case 0:
                    curList.Reverse();
                    curList.AddRange(addList);
                    break;
                case 1:
                    curList.Reverse();
                    addList.Reverse();
                    curList.AddRange(addList);
                    break;
                case 2:
                    curList.AddRange(addList);
                    break;
                case 3:
                    addList.Reverse();
                    curList.AddRange(addList);
                    break;
            }
        }
    }

    Bounds GetBounds(List<Vector3> list) {
        Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0, float.MinValue);
        foreach (Vector3 vert in list) {
            if (vert.x < min.x) min.x = vert.x;
            if (vert.z < min.z) min.z = vert.z;
            if (vert.x > max.x) max.x = vert.x;
            if (vert.z > max.z) max.z = vert.z;
        }

        return new Bounds((min + max) * .5f, max - min);
    }

    List<LineSegment> SetLineSegmentRing(List<Vector3> ring1, List<Vector3> ring2) {
        List<LineSegment> lines = new List<LineSegment>();
        List<Vector3> outerList = new List<Vector3>(ring1.Select(v => new Vector3(v.x, 0, v.z)));
        List<Vector3> innerList = new List<Vector3>(ring2.Select(v => new Vector3(v.x, 0, v.z)));
        
        foreach (Vector3 outer in outerList) {
            float minDist = float.MaxValue;
            Vector3 minPoint = new Vector3(float.MaxValue, 0, 0);
            foreach (Vector3 inner in innerList) {
                if (Vector3.Distance(outer, inner) < minDist) {
                    minDist = Vector3.Distance(outer, inner);
                    minPoint = inner;
                }
            }
            lines.Add(new LineSegment(outer, minPoint));
        }

        foreach (Vector3 inner in innerList) {
            float minDist = float.MaxValue;
            Vector3 minPoint = new Vector3(float.MaxValue, 0, 0);
            foreach (Vector3 outer in outerList) {
                if (Vector3.Distance(outer, inner) < minDist) {
                    minDist = Vector3.Distance(outer, inner);
                    minPoint = outer;
                }
            }
            if (!lines.Contains(new LineSegment(minPoint, inner))) {
                for (int i = 0; i < lines.Count; i++) {
                    if (lines[i].v1 == minPoint) {

                        Vector3 intercept1 = Utility.Intercept(lines[i].v2, lines[Utility.Mod(i - 1, lines.Count)].v2, minPoint, inner);
                        bool firstCrosses = Utility.PointOnLine(lines[i].v2, lines[Utility.Mod(i - 1, lines.Count)].v2, intercept1);

                        if (firstCrosses) {
                            lines.Insert(i, new LineSegment(minPoint, inner));
                        }
                        else {
                            lines.Insert(i + 1, new LineSegment(minPoint, inner));
                        }
                        break;
                    }
                }
            }
        }

        foreach (LineSegment line in lines) {
            line.v1 = ring1[outerList.IndexOf(line.v1)];
            line.v2 = ring2[innerList.IndexOf(line.v2)];
        }

        return lines;
    }

    Utility.Polygon SetOuterQuadsAndTris(List<LineSegment> lines) {
        Utility.Polygon mesh = new Utility.Polygon();

        for (int i = 0; i < lines.Count - 1; i++) {
            if (lines[i].v1 == lines[i + 1].v1) {
                mesh.AddTri(lines[i].v1, lines[i + 1].v2, lines[i].v2, ToUV(lines[i].v1), ToUV(lines[i + 1].v2), ToUV(lines[i].v2), flipMesh);
            }
            else if (lines[i].v2 == lines[i + 1].v2) {
                mesh.AddTri(lines[i].v1, lines[i + 1].v1, lines[i].v2, ToUV(lines[i].v1), ToUV(lines[i + 1].v1), ToUV(lines[i].v2), flipMesh);
            }
            else {
                mesh.AddQuad(lines[i].v1, lines[i + 1].v1, lines[i].v2, lines[i + 1].v2, ToUV(lines[i].v1), ToUV(lines[i + 1].v1), ToUV(lines[i].v2), ToUV(lines[i + 1].v2), flipMesh);
            }
        }
        if (lines[lines.Count - 1].v1 == lines[0].v1 && lines[lines.Count - 1].v2 != lines[0].v2) {
            mesh.AddTri(lines[lines.Count - 1].v1, lines[0].v2, lines[lines.Count - 1].v2, ToUV(lines[lines.Count - 1].v1), ToUV(lines[0].v2), ToUV(lines[lines.Count - 1].v2), flipMesh);
        }
        else if (lines[lines.Count - 1].v2 == lines[0].v2 && lines[lines.Count - 1].v1 != lines[0].v1) {
            mesh.AddTri(lines[lines.Count - 1].v1, lines[0].v1, lines[lines.Count - 1].v2, ToUV(lines[lines.Count - 1].v1), ToUV(lines[0].v1), ToUV(lines[lines.Count - 1].v2), flipMesh);
        }

        return mesh;
    }

    Utility.Polygon AddInnerQuadsAndTris(List<Vector3> validQuads, List<Vector3> validTris) {
        Utility.Polygon poly = new Utility.Polygon();

        foreach (Vector3 quad in validQuads) {
            bool xEven = Mathf.Round(quad.x * gridScale) % 2 == 0;
            bool zEven = Mathf.Round(quad.z * gridScale) % 2 == 0;
            Vector3 quadR = Vector3.right * gridScale;
            Vector3 quadF = Vector3.forward * gridScale;



            if (!IsHole(quad, quad + quadF, quad + quadR, quad + quadR + quadF)) {
                if (xEven == zEven) {
                    poly.AddQuad(quad, quad + quadF, quad + quadR, quad + quadR + quadF, ToUV(quad), ToUV(quad + quadF), ToUV(quad + quadR), ToUV(quad + quadR + quadF), flipMesh);
                }
                else {
                    poly.AddQuad(quad + quadF, quad + quadR + quadF, quad, quad + quadR, ToUV(quad + quadF), ToUV(quad + quadR + quadF), ToUV(quad), ToUV(quad + quadR), flipMesh);
                }
            }
        }
        for (int t = 0; t < validTris.Count; t += 3) {
            if (Vector3.Cross(validTris[t + 1] - validTris[t], validTris[t + 2] - validTris[t]).normalized == Vector3.up) {
                poly.AddTri(validTris[t], validTris[t + 1], validTris[t + 2], ToUV(validTris[t]), ToUV(validTris[t + 1]), ToUV(validTris[t + 2]), flipMesh);
            }
            else {
                poly.AddTri(validTris[t], validTris[t + 2], validTris[t + 1], ToUV(validTris[t]), ToUV(validTris[t + 2]), ToUV(validTris[t + 1]), flipMesh);
            }
        }

        return poly;
    }

    Vector2 ToUV(Vector3 point) {
        Vector3 vertTransformed = point - bounds.min;
        vertTransformed.x /= bounds.size.x;
        vertTransformed.z /= bounds.size.z;

        return vertTransformed;
    }

    bool IsHole(Vector3 corner1, Vector3 corner2, Vector3 corner3, Vector3 corner4) {
        float widthDelta = gridScale / bounds.size.x * .5f;
        float heightDelta = gridScale / bounds.size.z * .5f;

        Vector3 vertTransformed1 = corner1 - bounds.min;
        vertTransformed1.x /= bounds.size.x;
        vertTransformed1.z /= bounds.size.z;
        Vector3 vertTransformed2 = corner2 - bounds.min;
        vertTransformed2.x /= bounds.size.x;
        vertTransformed2.z /= bounds.size.z;
        Vector3 vertTransformed3 = corner3 - bounds.min;
        vertTransformed3.x /= bounds.size.x;
        vertTransformed3.z /= bounds.size.z;
        Vector3 vertTransformed4 = corner4 - bounds.min;
        vertTransformed4.x /= bounds.size.x;
        vertTransformed4.z /= bounds.size.z;

        foreach (Output output in outputs) {
            if (output.type == OutputType.hole) {
                Vector2 min = new Vector2(Mathf.Clamp01(output.gridMin.x) - widthDelta, Mathf.Clamp01(output.gridMin.y) - heightDelta);
                Vector2 max = new Vector2(Mathf.Clamp01(output.gridMax.x) + widthDelta, Mathf.Clamp01(output.gridMax.y) + heightDelta);


                if (vertTransformed1.x > min.x && vertTransformed1.x < max.x && vertTransformed1.z > min.y && vertTransformed1.z < max.y &&
                    vertTransformed2.x > min.x && vertTransformed2.x < max.x && vertTransformed2.z > min.y && vertTransformed2.z < max.y &&
                    vertTransformed3.x > min.x && vertTransformed3.x < max.x && vertTransformed3.z > min.y && vertTransformed3.z < max.y &&
                    vertTransformed4.x > min.x && vertTransformed4.x < max.x && vertTransformed4.z > min.y && vertTransformed4.z < max.y) {
                    return true;
                }
            }
        }

        return false;
    }

    List<Vector3> ModifyGroundHeights(List<Vector3> verts) {
        for (int v = 0; v < verts.Count; v++) {
            Vector3 newVert = verts[v];
            Vector3 vertTransformed = verts[v] - bounds.min;
            vertTransformed.x /= bounds.size.x;
            vertTransformed.z /= bounds.size.z;

            float height = EvaluateDepthCurve(vertTransformed.x % 1, vertTransformed.z % 1, false) * depthScale + heightOffset;
            if (!leaveHeightsUnchanged.Contains(verts[v])) newVert += Vector3.up * height;

            if (!outerRing.Contains(verts[v])) CheckAddOutput(newVert, vertTransformed.x, gridScale / bounds.size.x * .5f, vertTransformed.z, gridScale / bounds.size.z * .5f);

            verts[v] = newVert;
        }
        return verts;
    }

    void SetInnerRing(Vector3[] verts, List<Vector3> validQuads, List<Vector3> validTris) {
        bool isClosed = verts[0] == verts[verts.Length - 1];
        Vector2[] points = new List<Vector2>(verts.Select(v => new Vector2(v.x, v.z))).GetRange(0, verts.Count() - (isClosed ? 1 : 0)).ToArray();

        debugList = new List<Vector3>();
        List<Vector3> pathVerts = new List<Vector3>();
        if (points.Length >= 3) {
            Polygon polygon = new Polygon(points);
            int[] triangles = new Triangulator(polygon).Triangulate();

            Vector3 min = GetScaledMin();
            for (float i = min.x; i < bounds.max.x; i += gridScale) {
                for (float j = min.z; j < bounds.max.z; j += gridScale) {
                    bool intersects1 = false;
                    bool intersects2 = false;
                    bool intersects34 = false;
                    for (int k = 0; k < outerRing.Count - 1; k++) {
                        if (Maths2D.LineSegmentsIntersect(new Vector2(i, j), new Vector2(i + gridScale, j), new Vector2(outerRing[k].x, outerRing[k].z), new Vector2(outerRing[k + 1].x, outerRing[k + 1].z))) {
                            intersects1 = true;
                            break;
                        }
                        if (Maths2D.LineSegmentsIntersect(new Vector2(i, j), new Vector2(i, j + gridScale), new Vector2(outerRing[k].x, outerRing[k].z), new Vector2(outerRing[k + 1].x, outerRing[k + 1].z))) {
                            intersects2 = true;
                            break;
                        }
                        if (Maths2D.LineSegmentsIntersect(new Vector2(i + gridScale, j), new Vector2(i + gridScale, j + gridScale), new Vector2(outerRing[k].x, outerRing[k].z), new Vector2(outerRing[k + 1].x, outerRing[k + 1].z)) ||
                            Maths2D.LineSegmentsIntersect(new Vector2(i, j + gridScale), new Vector2(i + gridScale, j + gridScale), new Vector2(outerRing[k].x, outerRing[k].z), new Vector2(outerRing[k + 1].x, outerRing[k + 1].z))) {
                            intersects34 = true;
                        }
                    }
                    if (intersects1) {
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i, j))) {
                            pathVerts.Add(new Vector3(i, pathHeight, j));
                        }
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i + gridScale, j))) {
                            pathVerts.Add(new Vector3(i + gridScale, pathHeight, j));
                        }
                    }
                    else if (intersects2) {
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i, j))) {
                            pathVerts.Add(new Vector3(i, pathHeight, j));
                        }
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i, j + gridScale))) {
                            pathVerts.Add(new Vector3(i, pathHeight, j + gridScale));
                        }
                    }
                    else if (!intersects34 && PointInTriangles(polygon.points, triangles, new Vector2(i, j))) {
                        validQuads.Add(new Vector3(i, pathHeight, j));
                    }
                    if (intersects1 || intersects2 || intersects34) {
                        List<Vector3> possibleTris = new List<Vector3>();

                        if (PointInTriangles(polygon.points, triangles, new Vector2(i, j))) possibleTris.Add(new Vector3(i, pathHeight, j));
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i + gridScale, j))) possibleTris.Add(new Vector3(i + gridScale, pathHeight, j));
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i, j + gridScale))) possibleTris.Add(new Vector3(i, pathHeight, j + gridScale));
                        if (PointInTriangles(polygon.points, triangles, new Vector2(i + gridScale, j + gridScale))) possibleTris.Add(new Vector3(i + gridScale, pathHeight, j + gridScale));

                        if (possibleTris.Count == 3) {
                            validTris.AddRange(possibleTris);
                        }
                        if (possibleTris.Count == 2) {
                            if (!possibleTris.Contains(new Vector3(i, pathHeight, j)) && !possibleTris.Contains(new Vector3(i + gridScale, pathHeight, j)) && PointInTriangles(polygon.points, triangles, new Vector2(i + gridScale + gridScale, j))) {
                                pathVerts.Add(possibleTris[1]);
                                //validTris.AddRange(new Vector3[] { possibleTris[0], possibleTris[1], new Vector3(i + gridScale + gridScale, 0, j) });
                            }
                            /*
                            if (!possibleTris.Contains(new Vector3(i + gridScale, 0, j)) && !possibleTris.Contains(new Vector3(i + gridScale, 0, j + gridScale)) && 
                                PointInTriangles(polygon.points, triangles, new Vector2(i + gridScale, j - gridScale)) && !PointInTriangles(polygon.points, triangles, new Vector2(i, j + gridScale + gridScale))) {
                                //validTris.AddRange(new Vector3[] { possibleTris[0], possibleTris[1], new Vector3(i + gridScale, 0, j - gridScale) });
                            }*/
                        }
                    }
                }
            }

            pathVerts = Utility.TrimDuplicates(pathVerts);

            innerRing = new List<Vector3> { pathVerts[0] };

            Vector3 previous = pathVerts[0];
            int reverseCount = pathVerts.Count;
            //if (debug) debugList.Add(pathVerts[0]);
            pathVerts.RemoveAt(0);

            while (pathVerts.Count > 0 && reverseCount > 0) {
                Vector3 closest = previous;
                foreach (Vector3 vert in pathVerts) {
                    if (Vector3.Distance(previous, vert) < gridScale * 1.1f) {
                        closest = vert;
                        break;
                    }
                }
                if (closest == previous) {
                    foreach (Vector3 vert in pathVerts) {
                        if (Vector3.Distance(previous, vert) < gridScale * 1.5f) {
                            closest = vert;
                            break;
                        }
                    }
                }
                if (closest != previous) {
                    innerRing.Add(closest);
                    pathVerts.Remove(closest);
                    if (debug) debugList.Add(closest);
                    previous = closest;
                }
                else {
                    --reverseCount;
                    innerRing.Reverse();
                    previous = innerRing[innerRing.Count - 1];
                }
            }

            if (pathVerts.Count > 0) {
                for (int i = 0; i < pathVerts.Count; i++) {
                    debugSegList.Add(new LineSegment(pathVerts[i], pathVerts[i] + Vector3.up * 10));
                }
                innerRing.AddRange(pathVerts);
                print("Verts Left: " + pathVerts.Count);
            }
            if (Vector3.Distance(innerRing[0], Utility.NoY(outerRing[0])) > gridScale * 1.5f && !isClosed) {
                innerRing.Reverse();
            }
        }
    }

    bool PointInTriangles(Vector2[] p, int[] tris, Vector2 v) {
        for (int t = 0; t < tris.Length; t += 3) {
            if (Maths2D.PointInTriangle(p[tris[t]], p[tris[t + 1]], p[tris[t + 2]], v)) {
                return true;
            }
        }
        return false;
    }

    Vector3 GetScaledMin() {
        Vector3 offset = bounds.extents * gridScale;
        offset.x = Mathf.Round(offset.x) + gridScale;
        offset.z = Mathf.Round(offset.z) + gridScale;
        return bounds.center - offset / gridScale;
    }
}
