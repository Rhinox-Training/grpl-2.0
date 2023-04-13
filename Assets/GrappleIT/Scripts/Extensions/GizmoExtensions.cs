using System.Collections.Generic;
using UnityEngine;

public static class GizmoExtensions
{
    /// <summary>
    /// Draws a wire arc shape in 3D space.
    /// </summary>
    /// <param name="arcCenter"> The center point of the arc</param>
    /// <param name="direction">The direction in which the arc will be drawn.</param>
    /// <param name="arcNormal">The normal vector of the plane in which the arc lies.</param>
    /// <param name="arcRadius">The radius of the arc.</param>
    /// <param name="arcAngle">Optional parameter specifying the angle of the arc in degrees.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the arc.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="arcNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawWireArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius, float arcAngle, int segments = 10)
    {
        if (segments < 1)
            segments = 1;

        Vector3 initialPos = arcCenter + direction * arcRadius;
        Vector3 previousPos = initialPos;
        float anglePerSegment = arcAngle / segments;

        for (int index = 1; index <= segments; ++index)
        {
            var dir = direction;
            dir = Quaternion.AngleAxis(index * anglePerSegment, arcNormal) * dir;
            Vector3 newPos = arcCenter + dir * arcRadius;

            Gizmos.DrawLine(previousPos, newPos);

            previousPos = newPos;
        }
    }

    /// <summary>
    /// Draws a solid arc shape in 3D space using triangle meshes. The drawn arc is culled from behind.
    /// </summary>
    /// <param name="arcCenter"> The center point of the arc</param>
    /// <param name="direction">The direction in which the arc will be drawn.</param>
    /// <param name="arcNormal">The normal vector of the plane in which the arc lies.</param>
    /// <param name="arcRadius">The radius of the arc.</param>
    /// <param name="arcAngle">The angle of the arc in degrees.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the arc.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="arcNormal"/> should be normalized otherwise the circle will be drawn incorrectly.<br />
    /// This function has not yet been optimized!</remarks>
    public static void DrawSolidArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius,
        float arcAngle, int segments = 10)
    {
        // TODO: Combine sub meshes into 1 mesh  and draw that mesh once
        // TODO: Force double sided meshes, if desired
        if (segments < 1)
            segments = 1;

        Vector3 initialPos = arcCenter + direction * arcRadius;
        Vector3 previousPos = initialPos;
        float anglePerSegment = arcAngle / segments;

        for (int index = 1; index <= segments; ++index)
        {
            var dir = direction;
            dir = Quaternion.AngleAxis(index * anglePerSegment, arcNormal) * dir;
            Vector3 newPos = arcCenter + dir * arcRadius;

            // Create triangle mesh
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    arcCenter,
                    previousPos,
                    newPos
                },
                triangles = new[]
                {
                    0, 1, 2
                },
                normals = new[]
                {
                    arcNormal,
                    arcNormal,
                    arcNormal
                }

            };

            Gizmos.DrawMesh(mesh);
            previousPos = newPos;
        }
    }

    /// <summary>
    /// Draws a wire circle gizmo in 3D space.
    /// </summary>
    /// <param name="circleCenter">The center point of the circle in worldspace.</param>
    /// <param name="direction">The direction of where the circle start point is.</param>
    /// <param name="circleNormal">The normal of the plane the circle is on.</param>
    /// <param name="circleRadius">The radius of the circle.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the circle border.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="circleNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawWireCircle(Vector3 circleCenter, Vector3 direction, Vector3 circleNormal, float circleRadius, int segments = 10)
    {
        DrawWireCircle(circleCenter, direction, circleNormal, circleRadius, segments);
    }

    /// <summary>
    /// Draw a filled circle gizmo in 3D space.
    /// </summary>
    /// <param name="circleCenter">The center point of the circle in worldspace.</param>
    /// <param name="direction">The direction of where the circle start point is.</param>
    /// <param name="circleNormal">The normal of the plane the circle is on.</param>
    /// <param name="circleRadius">The radius of the circle.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the circle border.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="circleNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawSolidCircle(Vector3 circleCenter, Vector3 direction, Vector3 circleNormal, float circleRadius, int segments = 10)
    {
        DrawSolidArc(circleCenter, direction, circleNormal, circleRadius, segments);
    }

    /// <summary>
    /// Draws a filled Annulus (2D flat donut) arc in 3D space.
    /// </summary>
    /// <param name="center">The center point of the annulus arc in worldspace.</param>
    /// <param name="direction">The direction of where the annulus arc start point is.</param>
    /// <param name="forwardVec">The normal of the plane the annulus is on.</param>
    /// <param name="innerR">The radius of the annulus's inner border.</param>
    /// <param name="outerR">The radius of the annulus's outer border.</param>
    /// <param name="angle">The angle of the annulus arc in degrees.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the inner and outer circle borders of the annulus.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawSolidAnnulusArc(Vector3 center, Vector3 direction, Vector3 forwardVec,
    float innerR, float outerR, float angle, int segments = 12)
    {
        if (segments < 4 || outerR <= innerR)
            return;

        float anglePerSegment = angle / segments;

        var mesh = new Mesh();

        List<Vector3> nrmls = new List<Vector3>();
        List<Vector3> vertices = new List<Vector3>();

        for (int index = 0; index < segments; index++)
        {
            var dir = direction;
            dir = Quaternion.AngleAxis(index * -anglePerSegment, forwardVec) * dir;

            vertices.Add(center + (dir * innerR));
            vertices.Add(center + (dir * outerR));

            nrmls.Add(forwardVec);
            nrmls.Add(forwardVec);
        }

        List<int> indices = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
            //beacuse of counter clockwise winding
            //every uneven triangle indices have to be in opposite direction
            indices.Add((i + (2 - (i % 2))) % vertices.Count);
            indices.Add((i + (1 + (i % 2))) % vertices.Count);

            //equivalant code but written with IF-Statement
            //if (i % 2 == 0)
            //{
            //    indices.Add((i + 2) % vertices.Count);
            //    indices.Add((i + 1) % vertices.Count);
            //}
            //else
            //{
            //    indices.Add((i + 1) % vertices.Count);
            //    indices.Add((i + 2) % vertices.Count);
            //}
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.normals = nrmls.ToArray();

        Gizmos.DrawMesh(mesh);
    }

    /// <summary>
    /// Draws a filled Annulus (2D flat donut) kdsin 3D space.
    /// </summary>
    /// <param name="center">The center point of the annulus in worldspace.</param>
    /// <param name="direction">The direction of where the annulus start point is.</param>
    /// <param name="forwardVec">The normal of the plane the annulus is on.</param>
    /// <param name="innerR">The radius of the annulus's inner border.</param>
    /// <param name="outerR">The radius of the annulus's outer border.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the inner and outer circle borders of the annulus.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawSolidAnnulus(Vector3 center, Vector3 direction, Vector3 forwardVec,
        float innerR, float outerR, int segments = 12)
    {
        DrawSolidAnnulusArc(center, direction, forwardVec, innerR, outerR, 360f, segments);
    }

    /// <summary>
    /// Draws a filled Annulus (2D flat donut) kdsin 3D space.
    /// </summary>
    /// <param name="center">The center point of the annulus in worldspace.</param>
    /// <param name="direction">The direction of where the annulus start point is.</param>
    /// <param name="forwardVec">The normal of the plane the annulus is on.</param>
    /// <param name="innerR">The radius of the annulus's inner border.</param>
    /// <param name="outerR">The radius of the annulus's outer border.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the inner and outer circle borders of the annulus.</param>
    /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
    public static void DrawSolidAnnulusWidth(Vector3 center, Vector3 direction, Vector3 forwardVec,
        float centerR, float haldWidth, int segments = 12)
    {
        DrawSolidAnnulusArc(center, direction, forwardVec, centerR - haldWidth, centerR + haldWidth, 360f, segments);
    }
}