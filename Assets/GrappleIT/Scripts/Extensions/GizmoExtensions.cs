using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    
    /// <summary>
    /// This utility class provides extension methods for drawing various geometrical shapes as gizmos.
    /// </summary>
    public static class GizmoExtensions
    {
        /// <summary>
        /// This method draws a wireframe arc using Gizmos in the scene view
        /// </summary>
        /// <param name="arcCenter"> The center point of the arc</param>
        /// <param name="direction">The initial direction in which the arc will be drawn.</param>
        /// <param name="arcNormal">A perpendicular vector to the plane in which the arc lies. </param>
        /// <param name="arcRadius">The radius of the arc.</param>
        /// <param name="arcAngle">Specifies the angle, in degrees, that the arc spans.</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 10).</param>
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
        /// Draws a solid arc using Gizmos in the scene view.
        /// </summary>
        /// <param name="arcCenter"> The center point of the arc</param>
        /// <param name="direction">The initial direction in which the arc will be drawn.</param>
        /// <param name="arcNormal">A perpendicular vector to the plane in which the arc lies. </param>
        /// <param name="arcRadius">The radius of the arc.</param>
        /// <param name="arcAngle">Specifies the angle, in degrees, that the arc spans.</param>
        /// <param name="force2Sided">Optional parameter specifying if the arc will be visible from both sides of the normal (basically no culling).</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 10).</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="arcNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
        public static void DrawSolidArc(Vector3 arcCenter, Vector3 direction, Vector3 arcNormal, float arcRadius,
            float arcAngle, bool force2Sided = false, int segments = 10)
        {
            if (segments < 2)
                segments = 2;

            float anglePerSegment = arcAngle / segments;
            var mesh = new Mesh();

            int vertexCount = segments + 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];

            //
            //if 2 side than there are
            int[] indices = force2Sided ? new int[(segments * 6)] : new int[(segments * 3)];


            vertices[0] = arcCenter;
            normals[0] = arcNormal;
            var dir = Quaternion.AngleAxis(0f, arcNormal) * direction;
            vertices[1] = arcCenter + dir * arcRadius;
            normals[1] = arcNormal;

            int idx = 0;
            for (int index = 0; index < segments; ++index)
            {
                //Vertex and normal
                dir = Quaternion.AngleAxis((index + 1) * anglePerSegment, arcNormal) * direction;
                Vector3 newPos = arcCenter + dir * arcRadius;

                vertices[index + 2] = newPos;
                normals[index + 2] = arcNormal;

                //Indices
                //all triangles start from center point,
                //which is at index 0 of the vertices array
                indices[idx++] = 0;
                indices[idx++] = index + 1;
                indices[idx++] = index + 2;
                if (force2Sided)
                {
                    //reverse winding so that it is visible from other side.
                    indices[idx++] = 0;
                    indices[idx++] = index + 2;
                    indices[idx++] = index + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.normals = normals;

            Gizmos.DrawMesh(mesh);
        }

        /// <summary>
        /// Draws a wireframe circle using Gizmos in the scene view.
        /// </summary>
        /// <param name="circleCenter">The center point of the circle in world space.</param>
        /// <param name="direction">Specifies the initial direction in which the circle is drawn.</param>
        /// <param name="circleNormal"> A perpendicular vector to the plane in which the circle lies.</param>
        /// <param name="circleRadius">The radius of the circle.</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 10).</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="circleNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
        public static void DrawWireCircle(Vector3 circleCenter, Vector3 direction, Vector3 circleNormal, float circleRadius, int segments = 10)
        {
            DrawWireCircle(circleCenter, direction, circleNormal, circleRadius, segments);
        }

        /// <summary>
        /// Draw a filled circle gizmo in 3D space.
        /// </summary>
        /// <param name="circleCenter">The center point of the circle in world space.</param>
        /// <param name="direction">Specifies the initial direction in which the circle is drawn.</param>
        /// <param name="circleNormal"> A perpendicular vector to the plane in which the circle lies.</param>
        /// <param name="circleRadius">The radius of the circle.</param>
        /// <param name="force2Sided">Optional parameter specifying if the circle will be visible from both sides of the normal (basically no culling).</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 10).</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="circleNormal"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
        public static void DrawSolidCircle(Vector3 circleCenter, Vector3 direction, Vector3 circleNormal, float circleRadius, bool force2Sided = false, int segments = 10)
        {
            DrawSolidArc(circleCenter, direction, circleNormal, circleRadius, 360f, force2Sided, segments);
        }

        /// <summary>
        /// Draws a filled Annulus (2D flat donut) arc in 3D space.
        /// </summary>
        /// <param name="center">The center point of the annulus arc in world space.</param>
        /// <param name="direction">The direction of where the annulus arc start point is.</param>
        /// <param name="forwardVec">The normal of the plane the annulus is on.</param>
        /// <param name="innerR">The radius of the annulus's inner border.</param>
        /// <param name="outerR">The radius of the annulus's outer border.</param>
        /// <param name="angle">The angle of the annulus arc in degrees.</param>
        /// <param name="force2Sided">Optional parameter specifying if the annulus will be visible from both sides of the normal (basically no culling).</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 12).</param>
        /// <remarks>DOES NOT DO ARC, STILL CLOSES The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.<br />
        /// <paramref name="segments"/> cannot go below 3, as 3 is the minimum to make a triangle.</remarks>
        public static void DrawSolidAnnulusArc(Vector3 center, Vector3 direction, Vector3 forwardVec,
        float innerR, float outerR, float angle, bool force2Sided = false, int segments = 12)
        {
            if (segments < 3 || outerR <= innerR)
                return;

            float anglePerSegment = angle / segments;

            var mesh = new Mesh();

            int vertexCount = (segments + 1) * 2;
            Vector3[] nrmls = new Vector3[vertexCount];
            Vector3[] vertices = new Vector3[vertexCount];
            //triagles need 3 vertices so thats why index buffer is 3x vertex count.
            //2Side means double the the triangles
            int[] indices = force2Sided ? new int[vertexCount * 6] : new int[vertexCount * 3];

            int idx = 0;
            for (int index = 0; index < segments + 1; index++)
            {
                var dir = Quaternion.AngleAxis(index * -anglePerSegment, forwardVec) * direction;

                //vertex 0
                vertices[idx] = (center + (dir * innerR));
                nrmls[idx] = (forwardVec);

                //vertex 1
                vertices[++idx] = (center + (dir * outerR));
                nrmls[idx++] = (forwardVec);
            }

            idx = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                //if it's not a full cicle the last 2 triangles
                //that connect back to the beginning don't need to be drawn.
                if (angle >= 360f)
                {
                    indices[idx++] = i;
                    indices[idx++] = (i + (2 - (i % 2))) % vertexCount;
                    //beacuse of counter clockwise winding
                    //every uneven triangle indices have to be in opposite direction
                    indices[idx++] = (i + (1 + (i % 2))) % vertexCount;

                    if (force2Sided)
                    {
                        indices[idx++] = i;
                        //beacuse of counter clockwise winding
                        //every uneven triangle indices have to be in opposite direction
                        indices[idx++] = (i + (1 + (i % 2))) % vertexCount;
                        indices[idx++] = (i + (2 - (i % 2))) % vertexCount;
                    }
                }
                else if (i < vertexCount - 2)
                {
                    indices[idx++] = i;
                    //beacuse of counter clockwise winding
                    //every uneven triangle indices have to be in opposite direction
                    indices[idx++] = i + (2 - (i % 2));
                    indices[idx++] = i + 1 + (i % 2);

                    if (force2Sided)
                    {
                        indices[idx++] = i;
                        //beacuse of counter clockwise winding
                        //every uneven triangle indices have to be in opposite direction
                        indices[idx++] = i + 1 + (i % 2);
                        indices[idx++] = i + (2 - (i % 2));
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.normals = nrmls;

            Gizmos.DrawMesh(mesh);
        }

        /// <summary>
        /// Draws a solid annulus arc (a solid ring segment) using Gizmos in the scene view. 
        /// </summary>
        /// <param name="center">The center point of the annulus in world space.</param>
        /// <param name="direction">The initial direction in which the annulus is drawn. </param>
        /// <param name="forwardVec">A vector that defines the plane in which the annulus lies.</param>
        /// <param name="innerR">The radius of the annulus's inner border.</param>
        /// <param name="outerR">The radius of the annulus's outer border.</param>
        /// <param name="force2Sided">Optional parameter specifying if the annulus will be visible from both sides of the normal (basically no culling).</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 12).</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
        public static void DrawSolidAnnulus(Vector3 center, Vector3 direction, Vector3 forwardVec,
            float innerR, float outerR, bool force2Sided = false, int segments = 12)
        {
            DrawSolidAnnulusArc(center, direction, forwardVec, innerR, outerR, 360f, force2Sided, segments);
        }

        /// <summary>
        /// Draws a filled Annulus (2D flat donut) in 3D space.
        /// </summary>
        /// <param name="center">The center point of the annulus in worldspace.</param>
        /// <param name="direction">The initial direction in which the annulus is drawn. </param>
        /// <param name="forwardVec">A vector that defines the plane in which the annulus lies.</param>
        /// <param name="centerR">The radius of the annulus's.</param>
        /// <param name="halfWidth">The half width of the filled part of annulus.</param>
        /// <param name="force2Sided">Optional parameter specifying if the annulus will be visible from both sides of the normal (basically no culling).</param>
        /// <param name="segments">Specifies the number of line segments used to draw the arc (default is 12).</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.</remarks>
        public static void DrawSolidAnnulusWidth(Vector3 center, Vector3 direction, Vector3 forwardVec,
            float centerR, float halfWidth, bool force2Sided = false, int segments = 12)
        {
            DrawSolidAnnulusArc(center, direction, forwardVec, centerR - halfWidth, centerR + halfWidth, 360f, force2Sided, segments);
        }

        //Potential extra, wire annulus

        /// <summary>
        /// Draws a solid torus using Gizmos in the scene view. 
        /// </summary>
        /// <param name="center">The center point of the torus in worlds pace.</param>
        /// <param name="direction">The direction of where the torus start point is.</param>
        /// <param name="forwardVec">The normal of the plane the torus is on.</param>
        /// <param name="innerR">The radius of the annulus's inner border.</param>
        /// <param name="outerR">The radius of the annulus's outer border.</param>
        /// <param name="segmentsInner">Optional parameter specifying the number of line segments that will be used to draw 
        /// the loop around the given normal.</param>
        /// <param name="segmentsOuter">Optional parameter specifying the number of line segments that will be used to draw 
        /// the loop of each segment.</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.<br />
        /// <paramref name="segmentsInner"/> and <paramref name="segmentsOuter"/> cannot go below 3, as 3 is the minimum to make a triangle.</remarks>
        public static void DrawSolidTorus(Vector3 center, Vector3 direction, Vector3 forwardVec,
            float innerR, float outerR, int segmentsInner = 12, int segmentsOuter = 8)
        {
            if (segmentsInner < 3 || segmentsOuter < 3)
                return;

            float anglePerSegmentIn = 360f / segmentsInner;
            float anglePerSegmentOut = 360f / segmentsOuter;

            var mesh = new Mesh();

            Vector3[] nrmls = new Vector3[segmentsInner * segmentsOuter];
            Vector3[] vertices = new Vector3[segmentsInner * segmentsOuter];

            //calculate vertices and vertex normals
            int index = 0;
            for (int indexIn = 0; indexIn < segmentsInner; indexIn++)
            {
                var dirInner = Quaternion.AngleAxis(indexIn * -anglePerSegmentIn, forwardVec) * direction;

                var localCent = center + (dirInner * innerR);
                var localPlaneNrml = Vector3.Cross(forwardVec, (localCent - center).normalized);

                for (int indexOut = 0; indexOut < segmentsOuter; indexOut++)
                {
                    var dirOuter = Quaternion.AngleAxis(indexOut * -anglePerSegmentOut, localPlaneNrml) * forwardVec;
                    var vertex = localCent + (dirOuter * outerR);

                    vertices[index] = vertex;
                    nrmls[index++] = (vertex - localCent).normalized;
                }
            }

            //calculate indices
            int[] indices = new int[segmentsInner * segmentsOuter * 6];
            index = 0;
            for (int i = 0; i < segmentsInner; i++)
            {
                for (int j = 0; j < segmentsOuter; j++)
                {
                    int current = (i * segmentsOuter) + j;
                    int next = (i * segmentsOuter) + (j + 1) % segmentsOuter;
                    int bottom = ((i + 1) % segmentsInner * segmentsOuter) + j;
                    int bottomNext = ((i + 1) % segmentsInner * segmentsOuter) + (j + 1) % segmentsOuter;

                    indices[index++] = current;
                    indices[index++] = next;
                    indices[index++] = bottom;

                    indices[index++] = bottom;
                    indices[index++] = next;
                    indices[index++] = bottomNext;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = nrmls;
            mesh.triangles = indices;

            Gizmos.DrawMesh(mesh);
        }

        //Needs re-work or maybe deprecated...

        /// <summary>
        /// Draws a wire torus using Gizmos in the scene view. 
        /// </summary>
        /// <param name="center">The center point of the torus in worlds pace.</param>
        /// <param name="direction">The direction of where the torus start point is.</param>
        /// <param name="forwardVec">The normal of the plane the torus is on.</param>
        /// <param name="innerR">The radius of the annulus's inner border.</param>
        /// <param name="outerR">The radius of the annulus's outer border.</param>
        /// <param name="segmentsInner">Optional parameter specifying the number of line segments that will be used to draw 
        /// the loop around the given normal.</param>
        /// <param name="segmentsOuter">Optional parameter specifying the number of line segments that will be used to draw 
        /// the loop of each segment.</param>
        /// <remarks>The <paramref name="direction"/> and <paramref name="forwardVec"/> should be normalized otherwise the circle will be drawn incorrectly.<br />
        /// <paramref name="segmentsInner"/> and <paramref name="segmentsOuter"/> cannot go below 3, as 3 is the minimum to make a triangle.</remarks>
        public static void DrawWireTorus(Vector3 center, Vector3 direction, Vector3 forwardVec,
            float innerR, float outerR, int segmentsInner = 12, int segmentsOuter = 8)
        {
            if (segmentsInner < 4 || segmentsOuter < 4)
                return;

            float anglePerSegmentIn = 360f / segmentsInner;
            float anglePerSegmentOut = 360f / segmentsOuter;

            var mesh = new Mesh();

            Vector3[] nrmls = new Vector3[segmentsInner * segmentsOuter];
            Vector3[] vertices = new Vector3[segmentsInner * segmentsOuter];

            //calculate vertices and vertex normals
            int index = 0;
            for (int indexIn = 0; indexIn < segmentsInner; indexIn++)
            {
                var dirInner = Quaternion.AngleAxis(indexIn * -anglePerSegmentIn, forwardVec) * direction;

                var localCent = center + (dirInner * innerR);
                var localPlaneNrml = Vector3.Cross(forwardVec, (localCent - center).normalized);

                for (int indexOut = 0; indexOut < segmentsOuter; indexOut++)
                {
                    var dirOuter = Quaternion.AngleAxis(indexOut * -anglePerSegmentOut, localPlaneNrml) * forwardVec;
                    var vertex = localCent + (dirOuter * outerR);

                    vertices[index] = vertex;
                    nrmls[index++] = (vertex - localCent).normalized;
                }
            }

            //lines
            int[] indices = new int[segmentsInner * segmentsOuter * 6];
            index = 0;
            for (int i = 0; i < segmentsInner; i++)
            {
                for (int j = 0; j < segmentsOuter; j++)
                {
                    int current = (i * segmentsOuter) + j;
                    int next = (i * segmentsOuter) + (j + 1) % segmentsOuter;
                    int bottom = ((i + 1) % segmentsInner * segmentsOuter) + j;
                    int bottomNext = ((i + 1) % segmentsInner * segmentsOuter) + (j + 1) % segmentsOuter;

                    indices[index++] = current;
                    indices[index++] = next;
                    indices[index++] = bottom;

                    indices[index++] = bottom;
                    indices[index++] = next;
                    indices[index++] = bottomNext;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = nrmls;
            mesh.triangles = indices;

            Gizmos.DrawWireMesh(mesh);
        }
    }
}