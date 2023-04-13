using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    /// <summary>
    /// This class contains some useful 3D math functions/
    /// </summary>
    public static class InteractableMathUtils
    {
        /// <summary>
        /// Projects the given point onto a plane defined by a position and normal vector, and then translates the result by the position of the plane.
        /// </summary>
        /// <param name="point">The point to project.</param>
        /// <param name="planePosition">A point on the desired plane.</param>
        /// <param name="planeNormal">The normal vector of the desired plane.</param>
        /// <returns>The projected point, translated by the position of the plane.</returns>
        public static Vector3 ProjectOnPlaneAndTranslate(this Vector3 point, Vector3 planePosition, Vector3 planeNormal)
        {
            // Project the position on the plane defined by the given position and forward
            var projectedPos = Vector3.ProjectOnPlane(point, planeNormal) +
                               Vector3.Dot(planePosition, planeNormal) *
                               planeNormal;
            return projectedPos;
        }
        
        /// <summary>
        /// Checks if the given position is in front of given plane. The plane is defined with a point and forward vector.
        /// </summary>
        /// <param name="pos">The position to check</param>
        /// <param name="planePosition">A point on the desired plane</param>
        /// <param name="planeNormal">The normal of the desired plane</param>
        /// <returns></returns>
        public static bool IsPositionInFrontOfPlane(Vector3 pos, Vector3 planePosition, Vector3 planeNormal)
        {
            // Check if the finger pos is in front of the button
            // Check the dot product of the forward of the base and the vector from the base to the joint pos
            Vector3 toPos = pos - planePosition;
            float dotValue = Vector3.Dot(planeNormal, toPos);
            return dotValue >= 0;
        }

        /// <summary>
        /// Checks whether the projected position of the given point on the plane (defined by
        /// the planePosition and PlaneForward) is within the given bounds.
        /// </summary>
        /// <param name="point">The point to project.</param>
        /// <param name="planePosition">A point on the desired plane</param>
        /// <param name="planeNormal">The normal of the desired plane</param>
        /// <param name="bounds">The bounds to check</param>
        /// <returns></returns>
        public static bool IsPlaneProjectedPointInBounds(Vector3 point, Vector3 planePosition, Vector3 planeNormal,
            Bounds bounds)
        {
            // Project the position on the plane defined by the given position and forward
            var projectedPos = Vector3.ProjectOnPlane(point, planeNormal) +
                               Vector3.Dot(planePosition, planeNormal) *
                               planeNormal;

            return bounds.Contains(projectedPos);
        }

        
        /// <summary>
        /// Calculates the projected distance of point "projectPoint" and "normalPoint" along the normal vector "normal".
        /// </summary>
        /// <param name="projectPoint">The point to project</param>
        /// <param name="normalPoint">The reference point on the normal vector</param>
        /// <param name="normal">The normal vector to project on</param>
        /// <returns>A scalar float value representing the projected distance</returns>
        public static float GetProjectedDistanceFromPointOnNormal(Vector3 projectPoint, Vector3 normalPoint,
            Vector3 normal)
        {
            return Vector3.Dot(projectPoint - normalPoint,
                normal);
        }

        /// <summary>
        /// Checks if point p1 is closer to the interactible than point p2.
        /// </summary>
        /// <param name="referencePoint">The main point</param>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>A boolean representing whether the point is closer or not.</returns>
        public static bool IsPointCloserThanOtherPoint(Vector3 referencePoint, Vector3 p1, Vector3 p2)
        {
            float distanceSqr1 = (referencePoint - p1).sqrMagnitude;
            float distanceSqr2 = (referencePoint - p2).sqrMagnitude;
            return distanceSqr1 < distanceSqr2;
        }
    }
}