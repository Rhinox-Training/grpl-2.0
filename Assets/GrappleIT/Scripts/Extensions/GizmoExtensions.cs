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
    /// <param name="arcAngle">The angle of the arc in degrees.</param>
    /// <param name="arcRadius">The radius of the arc.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the arc.</param>
    /// <remarks>Direction vector and arcNormal vector should be normalized! If they aren't the radius of the arc will be incorrect.</remarks>
    public static void DrawWireArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcAngle,
        float arcRadius, int segments = 10)
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
    /// <param name="arcAngle">The angle of the arc in degrees.</param>
    /// <param name="arcRadius">The radius of the arc.</param>
    /// <param name="segments">Optional parameter specifying the number of line segments that will be used to draw the arc.</param>
    /// <remarks>This function has not yet been optimized!<br />
    /// Direction vector and arcNormal vector should be normalized! If they aren't the radius of the arc will be incorrect.</remarks>
    public static void DrawSolidArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcAngle,
        float arcRadius, int segments = 10)
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
}