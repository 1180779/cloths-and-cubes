using Engine.Collision;
using Engine.Rays;

using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.Gizmos.Rotation;

public sealed class GizmoRing : IGizmoArrow, IDisposable
{
    private readonly TorusMesh _torusMesh;

    public float MajorRadius;
    public float MinorRadius;

    public GizmoRing(float majorRadius = 1.0f, float minorRadius = 0.05f)
    {
        _torusMesh = new TorusMesh();
        MajorRadius = majorRadius;
        MinorRadius = minorRadius;
    }

    public void Render(
        Shader shader,
        Vector3 origin,
        Vector3 axis,
        Vector4 color,
        float scaleFactor = 1.0f)
    {
        var rotation = GetRotationFromAxis(axis);

        var model =
            Matrix4.CreateScale(MajorRadius * scaleFactor, MajorRadius * scaleFactor, MinorRadius * scaleFactor) *
            Matrix4.CreateFromQuaternion(rotation) *
            Matrix4.CreateTranslation(origin);

        shader.SetMatrix4("model", model);
        shader.SetVector3("color", color.Xyz);
        shader.SetFloat("alpha", color.W);
        _torusMesh.Render();
    }

    /// <summary>
    /// Checks if the specified ray intersects with the gizmo ring and calculates the intersection distance.
    /// This method uses a simplified plane-based intersection test rather than a full ray-torus intersection.
    /// </summary>
    /// <param name="ray">The ray to test for intersection with the gizmo ring.</param>
    /// <param name="origin">The origin point of the gizmo ring.</param>
    /// <param name="axis">The axis vector defining the orientation of the gizmo ring.</param>
    /// <param name="handleSize">The scaling factor applied to the ring's size.</param>
    /// <param name="distance">The distance from the ray's origin to the plane intersection point (not the actual torus surface)
    /// if the intersection falls within the ring's annular region. Set to <c>float.MaxValue</c> if no intersection is found.</param>
    /// <returns><c>true</c> if the ray intersects the ring plane within the annular region defined by the major and minor radii;
    /// otherwise, <c>false</c>.</returns>
    public bool CheckIntersection(Ray ray, Vector3 origin, Vector3 axis, float handleSize, out float distance)
    {
        distance = float.MaxValue;

        float scaledMajorRadius = MajorRadius * handleSize;
        float scaledMinorRadius = MinorRadius * handleSize;

        // Create a plane perpendicular to the axis passing through the origin
        var plane = new CollisionPlane { Direction = axis.ToEngine(), Offset = axis.ToEngine() * origin.ToEngine() };

        if (!RayIntersection.IntersectRayPlane(ray, plane, out float planeDist))
        {
            return false;
        }

        var intersectionPoint = ray.Origin + ray.Direction * planeDist;

        // Check if the point is within ring radius range
        var vectorFromCenter = intersectionPoint.ToOpenTK() - origin;
        float distanceFromCenter = vectorFromCenter.Length;

        float ringInnerRadius = scaledMajorRadius - scaledMinorRadius;
        float ringOuterRadius = scaledMajorRadius + scaledMinorRadius;

        if (distanceFromCenter >= ringInnerRadius && distanceFromCenter <= ringOuterRadius)
        {
            distance = planeDist;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the rotation needed to align a ring (default in XY plane around Z axis) with the given axis.
    /// </summary>
    /// <param name="axis">The target axis direction (perpendicular to the desired ring plane).</param>
    /// <returns>Quaternion representing the rotation.</returns>
    private static Quaternion GetRotationFromAxis(Vector3 axis)
    {
        Vector3 rotAxis = Vector3.Cross(Vector3.UnitZ, axis);
        if (rotAxis.LengthSquared < 1e-6f)
        {
            // Parallel or anti-parallel
            return axis.Z > 0 ? Quaternion.Identity : Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI);
        }

        rotAxis.Normalize();
        float angle = MathF.Acos(Vector3.Dot(Vector3.UnitZ, axis));
        return Quaternion.FromAxisAngle(rotAxis, angle);
    }

    public void Dispose()
    {
        _torusMesh.Dispose();
    }
}