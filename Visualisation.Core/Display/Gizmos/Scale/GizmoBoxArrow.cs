using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Rays;
using Engine.RigidBodies;

using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.Gizmos.Scale;

/// <summary>
/// Represents a 3D arrow gizmo component consisting of a cylinder shaft and a box tip.
/// </summary>
public sealed class GizmoBoxArrow : IGizmoArrow, IDisposable
{
    private readonly CubeMesh _cubeMesh;
    private readonly CylinderMesh _cylinderMesh;

    public float ShaftLength;
    public float ShaftRadius;
    public float BoxSize;

    public GizmoBoxArrow(
        float shaftLength = 1.0f,
        float shaftRadius = 0.05f,
        float boxSize = 0.2f)
    {
        _cubeMesh = new CubeMesh();
        _cylinderMesh = new CylinderMesh();

        ShaftLength = shaftLength;
        ShaftRadius = shaftRadius;
        this.BoxSize = boxSize;
    }

    /// <summary>
    /// Renders the arrow at the specified position and orientation.
    ///
    /// The arrow is scaled by distance to the camera to maintain a consistent on-screen size.
    /// </summary>
    /// <param name="shader">The shader to use for rendering. It is assumed that model and color uniforms are available. </param>
    /// <param name="origin">The origin point of the arrow.</param>
    /// <param name="direction">The normalized direction the arrow points to.</param>
    /// <param name="color">The color to render the arrow.</param>
    /// <param name="scaleFactor">Scale factor for the arrow size.</param>
    public void Render(
        Shader shader,
        Vector3 origin,
        Vector3 direction,
        Vector4 color,
        float scaleFactor = 1.0f)
    {
        var rotation = GetRotationFromDirection(direction);

        // Render cylinder shaft
        var cylinderModel =
            Matrix4.CreateScale(ShaftRadius * scaleFactor, ShaftRadius * scaleFactor,
                ShaftLength * scaleFactor) *
            Matrix4.CreateFromQuaternion(rotation) *
            Matrix4.CreateTranslation(origin + direction * (ShaftLength * scaleFactor / 2));

        shader.SetMatrix4("model", cylinderModel);
        shader.SetVector3("color", color.Xyz);
        shader.SetFloat("alpha", color.W);
        _cylinderMesh.Render();

        // Render box tip
        var coneModel = Matrix4.CreateScale(BoxSize * scaleFactor, BoxSize * scaleFactor,
                BoxSize * scaleFactor) *
            Matrix4.CreateFromQuaternion(rotation) *
            Matrix4.CreateTranslation(origin +
                direction * (ShaftLength * scaleFactor + (BoxSize * scaleFactor) / 2));

        shader.SetMatrix4("model", coneModel);
        _cubeMesh.Render();
    }

    /// <summary>
    /// Tests if a ray intersects with this arrow.
    /// Uses a two-phase approach: broad phase AABB test followed by precise cylinder/cone tests.
    /// </summary>
    /// <param name="ray">The ray to test against.</param>
    /// <param name="origin">The origin point of the arrow.</param>
    /// <param name="direction">The normalized direction the arrow points to.</param>
    /// <param name="handleSize">Scale factor for the arrow size.</param>
    /// <param name="distance">The distance to the intersection point if hit.</param>
    /// <returns>True if the ray intersects the arrow, false otherwise.</returns>
    public bool CheckIntersection(Ray ray, Vector3 origin, Vector3 direction, float handleSize, out float distance)
    {
        distance = float.MaxValue;

        float scaledShaftLength = ShaftLength * handleSize;
        float scaledShaftRadius = ShaftRadius * handleSize;
        float scaledBoxSize = BoxSize * handleSize;

        float totalLength = scaledShaftLength + scaledBoxSize;

        // Broad phase: Create AABB encompassing the entire arrow
        Vector3 arrowEnd = origin + direction * totalLength;
        Vector3 bboxMin = Vector3.ComponentMin(origin, arrowEnd);
        Vector3 bboxMax = Vector3.ComponentMax(origin, arrowEnd);

        // Expand bbox by maximum radius to ensure it contains the entire box arrow
        // Technically, the scaledBoxSize would be a root at the diagonal, but this is close enough for broad-phase
        float maxRadius = Math.Max(scaledShaftRadius, scaledBoxSize);
        bboxMin -= new Vector3(maxRadius);
        bboxMax += new Vector3(maxRadius);

        var bbox = new BoundingBox(
            center: new Engine.Vector3(
                (bboxMin.X + bboxMax.X) / 2.0f,
                (bboxMin.Y + bboxMax.Y) / 2.0f,
                (bboxMin.Z + bboxMax.Z) / 2.0f
            ),
            halfSize: new Engine.Vector3(
                (bboxMax.X - bboxMin.X) / 2.0f,
                (bboxMax.Y - bboxMin.Y) / 2.0f,
                (bboxMax.Z - bboxMin.Z) / 2.0f
            )
        );

        if (!RayIntersection.IntersectRayAABB(ray, bbox, out _))
        {
            return false;
        }

        // Narrow phase: Test against the cylinder shaft
        var cylinder = new CollisionCylinder { Radius = scaledShaftRadius, Height = scaledShaftLength };

        Vector3 cylinderCenter = origin + direction * (scaledShaftLength / 2.0f);
        Quaternion rotation = GetRotationFromDirection(direction);

        cylinder.Body = new RigidBody { Position = cylinderCenter.ToEngine(), Orientation = rotation.ToEngine() };
        cylinder.Body.CalculateDerivedData();
        cylinder.CalculateInternals();

        bool hitCylinder = RayIntersection.IntersectionRayCylinder(ray, cylinder, out float cylinderDist);
        if (hitCylinder)
        {
            distance = Math.Min(distance, cylinderDist);
        }

        // Narrow phase: Test against box tip
        var scaledBoxHalfSize = scaledBoxSize / 2.0f;
        var box = new CollisionBox
        {
            HalfSize = new Engine.Vector3(scaledBoxHalfSize, scaledBoxHalfSize, scaledBoxHalfSize),
        };

        Vector3 boxCenter = origin + direction * (scaledShaftLength + scaledBoxHalfSize);

        box.Body = new RigidBody { Position = boxCenter.ToEngine(), Orientation = rotation.ToEngine() };
        box.Body.CalculateDerivedData();
        box.CalculateInternals();

        bool hitBox = RayIntersection.IntersectRayOBB(ray, box, out float coneDist);
        if (hitBox)
        {
            distance = Math.Min(distance, coneDist);
        }

        return hitCylinder || hitBox;
    }

    private static Quaternion GetRotationFromDirection(Vector3 direction)
    {
        // Handle edge cases for axis-aligned directions
        if (direction == Vector3.UnitZ)
            return Quaternion.Identity;
        if (direction == -Vector3.UnitZ)
            return Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI);

        // General case: rotate from Z-axis to the direction
        Vector3 axis = Vector3.Cross(Vector3.UnitZ, direction);
        if (axis.LengthSquared < 1e-6f)
        {
            // Direction is parallel to Z axis (shouldn't happen due to earlier checks)
            return Quaternion.Identity;
        }

        axis.Normalize();
        float angle = MathF.Acos(Vector3.Dot(Vector3.UnitZ, direction));
        return Quaternion.FromAxisAngle(axis, angle);
    }

    public void Dispose()
    {
        _cubeMesh.Dispose();
        _cylinderMesh.Dispose();
    }
}