using System.Diagnostics;

using Engine.Rays;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Controls;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos;

public abstract class GizmoBase(Shader shader)
{
    public event IGizmo.TargetChangedEventHandler TargetChangedEvent = delegate { };

    protected void InvokeGizmoTargetChangedByGizmo(GameObjectCollisionPrimitive collisionPrimitive)
    {
        TargetChangedEvent.Invoke(collisionPrimitive);
    }

    protected GizmoAxis _selectedAxis = GizmoAxis.None;
    protected GizmoAxis _hoveredAxis = GizmoAxis.None;

    protected abstract IGizmoArrow Arrow { get; }
    protected GameObjectCollisionPrimitive? _target;
    protected readonly Shader _shader = shader;

    public GameObjectCollisionPrimitive? Target
    {
        get => _target;
        set => _target = value;
    }

    public float DefaultTransparency { get; set; } = 0.5f;
    public Vector4 SelectionColor { get; set; } = new(1.0f, 1.0f, 0, 0.5f);
    public Vector4 HoverColor { get; set; } = new(0.5f, 0.5f, 0, 0.5f);

    public GizmoSpace Space { get; set; } = GizmoSpace.Global;
    public float HandleSize { get; set; } = 1.0f;
    public bool ConstantScreenSize { get; set; }

    protected Vector2 _dragStartMouse;
    protected bool _useScreenSpaceFallback;

    protected virtual void BeforeCheckIntersection() { }

    protected virtual void CheckArrowIntersection(
        Ray ray,
        Vector3 position,
        Quaternion rotation,
        GizmoAxis axis,
        float handleScale,
        ref GizmoAxis hitAxis,
        ref float closestDist)
    {
        if (Arrow.CheckIntersection(ray, position, GetAxisDirection(axis, rotation), handleScale,
            out float dist))
        {
            if (dist < closestDist)
            {
                closestDist = dist;
                hitAxis = axis;
            }
        }
    }

    protected GizmoAxis CheckIntersection(Ray ray, Vector3 position, CameraBase camera)
    {
        BeforeCheckIntersection();

        float closestDist = float.MaxValue;
        GizmoAxis hitAxis = GizmoAxis.None;

        var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;

        float handleScale = ConstantScreenSize ? GetGizmoScale(position, camera) : HandleSize;

        CheckArrowIntersection(ray, position, rotation, GizmoAxis.X, handleScale, ref hitAxis, ref closestDist);
        CheckArrowIntersection(ray, position, rotation, GizmoAxis.Y, handleScale, ref hitAxis, ref closestDist);
        CheckArrowIntersection(ray, position, rotation, GizmoAxis.Z, handleScale, ref hitAxis, ref closestDist);

        return hitAxis;
    }

    public void Render(CameraBase camera)
    {
        if (_target == null) return;
        BeforeRender();

        _shader.Use();
        camera.SetForSimpleShader(_shader);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var position = _target.Position;
        var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;

        float finalHandleSize = ConstantScreenSize ? GetGizmoScale(position, camera) : HandleSize;

        // X Axis Ring (Red) - rotates around X axis
        RenderAxis(GizmoAxis.X, position, rotation, new Vector4(1, 0, 0, DefaultTransparency), finalHandleSize);

        // Y Axis Ring (Green) - rotates around Y axis
        RenderAxis(GizmoAxis.Y, position, rotation, new Vector4(0, 1, 0, DefaultTransparency), finalHandleSize);

        // Z Axis Ring (Blue) - rotates around Z axis
        RenderAxis(GizmoAxis.Z, position, rotation, new Vector4(0, 0, 1, DefaultTransparency), finalHandleSize);

        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
    }

    protected virtual void BeforeRender()
    {
    }

    protected virtual void RenderAxis(
        GizmoAxis axis,
        Vector3 position,
        Quaternion rotation,
        Vector4 defaultColor,
        float handleSize)
    {
        Arrow.Render(_shader, position, GetAxisDirection(axis, rotation),
            _hoveredAxis == axis ? HoverColor :
            _selectedAxis == axis ? SelectionColor : defaultColor,
            handleSize);
    }

    // 
    // From Unity's discussion thread. Seems to work reasonably well.
    // https://discussions.unity.com/t/constant-screen-size-gizmos/425916/3
    //
    protected float GetGizmoScale(Vector3 position, CameraBase camera)
    {
        float distance = Vector3.Dot(position - camera.Position, camera.Front);
        if (distance < camera.NearPlane) distance = camera.NearPlane;

        // Project a unit vector at the object's depth
        Vector3 p1 = camera.Position + camera.Front * distance;
        Vector3 p2 = p1 + camera.Right; // 1 unit right

        Vector4 p1Clip = new Vector4(p1, 1.0f) * camera.ViewMatrix * camera.ProjectionMatrix;
        Vector4 p2Clip = new Vector4(p2, 1.0f) * camera.ViewMatrix * camera.ProjectionMatrix;

        // To NDC
        Vector2 p1NDC = p1Clip.Xy / p1Clip.W;
        Vector2 p2NDC = p2Clip.Xy / p2Clip.W;

        float lengthNDC = (p1NDC - p2NDC).Length;

        // 1 unit world size should correspond to some constant NDC size.
        return HandleSize * 0.15f / lengthNDC;
    }

    /// <summary>
    /// Calculates the projected movement along an axis skew line based on a drag operation.
    /// This computes the difference between the given drag start point and the closest point
    /// on the axis line to the drag ray, projected onto the specified axis.
    /// </summary>
    /// <param name="dragRay">The ray representing the drag operation. The ray direction must be normalized. </param>
    /// <param name="origin">The origin point of the axis line.</param>
    /// <param name="axis">The normalized direction vector of the axis line.</param>
    /// <param name="dragStartPoint">The starting point of the drag operation.</param>
    /// <returns>
    /// A <c>float</c> representing the projected movement along the axis. The result is the scalar projection
    /// of the distance vector between the drag start point and the closest point on the axis onto the axis direction.
    /// </returns>
    protected float GetProjectedMovementOnAxisSkewLine(
        Ray dragRay,
        Vector3 origin,
        Vector3 axis,
        Vector3 dragStartPoint)
    {
        // 
        // The axis line is given by:
        // P_a = origin_a + t_a * axisDirection_a
        // 
        // The ray line is given by:
        // P_r = origin_r + t_r * rayDirection_r
        // 
        // We want to find the point on the axis that is the closes to the ray line. 
        // Then the distance from the drag start point to that point projected onto the axis is the wanted movement.
        // 
        // We want to find the value of the t_a from which the point P_a can be found.
        // 

        // 
        // The points will have the least distance between them when the vector between them is perpendicular to both lines.
        // 
        // Let
        //   P be the vector between the two points:
        //   o_r be the origin of the ray
        //   d_r be the direction of the ray
        //   o_a be the origin of the axis
        //   d_a be the direction of the axis
        // P = P_r - P_a = (o_r + t_r * d_r) - (o_a + t_a * d_a)
        // 
        // then we have the two conditions:
        // P · d_a = 0
        // P · d_r = 0
        //

        // 
        // Expanding:
        // ((o_r + t_r * d_r) - (o_a + t_a * d_a)) · d_a = 0
        // ((o_r + t_r * d_r) - (o_a + t_a * d_a)) · d_r = 0
        // we use distributive property of dot product:
        // o_r · d_a + t_r * d_r · d_a - o_a · d_a - t_a * d_a · d_a = 0
        // o_r · d_r + t_r * d_r · d_r - o_a · d_r - t_a * d_a · d_r = 0
        // 
        // Assuming that d_a and d_r are normalized:
        // o_r · d_a + t_r * d_r · d_a - o_a · d_a - t_a * 1 = 0
        // o_r · d_r + t_r * 1 - o_a · d_r - t_a * d_a · d_r = 0
        // 
        // Rearranging:
        // o_r · d_a + t_r * d_r · d_a = o_a · d_a + t_a * 1
        // o_r · d_r + t_r * 1 = o_a · d_r + t_a * d_a · d_r
        // 
        // We want to remove the t_r term, so we multiply the second equation by d_r · d_a and subtract it from the first:
        // o_r · d_a + t_r * d_r · d_a = o_a · d_a + t_a * 1
        // (o_r · d_r) * (d_r · d_a) + t_r * (d_r · d_a) = (o_a · d_r) * (d_r · d_a) + t_a * (d_a · d_r) * (d_r · d_a)
        // 
        // Subtracting:
        // o_r · d_a - (o_r · d_r) * (d_r · d_a) = o_a · d_a + t_a - (o_a · d_r) * (d_r · d_a) - t_a * (d_a · d_r)^2
        // 
        // Grouping t_a terms:
        // o_r · d_a - (o_r · d_r) * (d_r · d_a) - o_a · d_a + (o_a · d_r) * (d_r · d_a) = t_a * (1 - (d_a · d_r)^2)
        // 
        // Factoring:
        // (o_r - o_a) · d_a - ((o_r - o_a) · d_r) * (d_r · d_a) = t_a * (1 - (d_a · d_r)^2)
        // 
        // Finally isolating t_a:
        // t_a = ((o_r - o_a) · d_a - ((o_r - o_a) · d_r) * (d_r · d_a)) / (1 - (d_a · d_r)^2)
        //

        var rayOrigin = dragRay.Origin.ToOpenTK();
        var rayDirection = dragRay.Direction.ToOpenTK();

        // check assumptions in debug mode: 
        // axis and ray direction are normalized
        Debug.Assert(Math.Abs(axis.LengthSquared - 1.0f) < 1e-2f, "Axis direction not normalized");
        Debug.Assert(Math.Abs(rayDirection.LengthSquared - 1.0f) < 1e-2f, "Ray direction not normalized");

        var diff = rayOrigin - origin;
        var rayDirDotAxis = Vector3.Dot(rayDirection, axis);

        var numerator = Vector3.Dot(diff, axis) - Vector3.Dot(diff, rayDirection) * rayDirDotAxis;
        var denominator = 1.0f - rayDirDotAxis * rayDirDotAxis;

        if (Math.Abs(denominator) < 1e-6f)
        {
            return 0.0f;
        }

        var tA = numerator / denominator;

        var closestPointOnAxis = origin + tA * axis;
        var dragVector = closestPointOnAxis - dragStartPoint;

        return Vector3.Dot(dragVector, axis);
    }


    protected const float AngleDegreesFallback = 10.0f;

    /// <summary>
    /// Determines whether to use screen-space fallback based on the angle between the axis and the initial ray.
    /// This should be called once at the start of a drag operation and the result cached.
    /// </summary>
    /// <param name="initialRay">The ray from the mouse position at the start of the drag.</param>
    /// <param name="origin">The origin point of the axis line.</param>
    /// <param name="axis">The normalized direction vector of the axis line.</param>
    /// <returns>True if screen-space fallback should be used, false otherwise.</returns>
    protected bool ShouldUseScreenSpaceFallback(Ray initialRay, Vector3 origin, Vector3 axis)
    {
        Debug.Assert(MathF.Abs(axis.LengthSquared - 1.0f) < 1e-3f, "Axis direction not normalized");

        var rayDirection = initialRay.Origin.ToOpenTK() + initialRay.Direction.ToOpenTK() - origin;
        var axisDotRayDir = Vector3.Dot(axis, rayDirection.Normalized());

        return MathF.Abs(axisDotRayDir) > MathF.Cos(AngleDegreesFallback / 360.0f * 2.0f * MathF.PI);
    }

    protected float GetProjectedMovementOnAxis(
        Ray dragRay,
        Vector2 dragMouseStart,
        Vector2 dragMouseCurrent,
        Vector3 origin,
        Vector3 axis,
        Vector3 dragStartPoint,
        CameraBase camera)
    {
        Debug.Assert(MathF.Abs(axis.LengthSquared - 1.0f) < 1e-3f, "Axis direction not normalized");

        if (_useScreenSpaceFallback)
        {
            float screenDelta =
                GetScreenSpaceAxisDelta(dragMouseStart, dragMouseCurrent, axis, camera, sensitivity: 0.01f);
            return screenDelta;
        }

        return GetProjectedMovementOnAxisSkewLine(dragRay, origin, axis, dragStartPoint);
    }

    /// <summary>
    /// Calculates the angular or linear delta based on screen-space mouse movement.
    /// This method projects the axis direction onto screen space and weights the mouse delta
    /// by how aligned the axis is with the horizontal and vertical screen directions.
    /// </summary>
    /// <param name="dragStartMouse">The mouse position where the drag started (in screen space).</param>
    /// <param name="currentMouse">The current mouse position (in screen space).</param>
    /// <param name="axis">The normalized axis direction in world space along which to measure the delta.</param>
    /// <param name="camera">The camera used to determine screen-space projections.</param>
    /// <param name="sensitivity">Multiplier for the delta calculation. Higher values = faster movement/rotation.</param>
    /// <returns>
    /// A <c>float</c> representing the delta value. The interpretation depends on usage:
    /// for rotation gizmos this is an angle in radians, for translation/scale it could be a distance.
    /// </returns>
    protected float GetScreenSpaceAxisDelta(
        Vector2 dragStartMouse,
        Vector2 currentMouse,
        Vector3 axis,
        CameraBase camera,
        float sensitivity = 1.0f)
    {
        Vector3 cameraDir = camera.Front;
        Vector2 mouseDelta = currentMouse - dragStartMouse;

        Vector3 camRight = camera.Right;
        Vector3 camUp = camera.Up;

        // Find which screen direction corresponds to movement along the axis
        // The movement should be perpendicular to both the axis and the view direction
        Vector3 axisScreenDir = Vector3.Cross(axis, cameraDir);

        // In case the axis is parallel to the camera, use camera up as fallback
        if (axisScreenDir.LengthSquared < 1e-6f)
        {
            axisScreenDir = camUp;
        }
        else
        {
            axisScreenDir.Normalize();
        }

        // Project the axis screen direction onto screen axes
        float rightComponent = Vector3.Dot(axisScreenDir, camRight);
        float upComponent = Vector3.Dot(axisScreenDir, camUp);

        // Calculate delta by weighting mouse movement with screen projections
        float delta = (mouseDelta.X * rightComponent - mouseDelta.Y * upComponent) * sensitivity;

        return delta;
    }

    protected static Quaternion GetObjectRotation(GameObject? target)
    {
        if (target is GameObjectCollisionPrimitive rb)
            return rb.EngineCollisionPrimitive.Body.Orientation.ToOpenTK();

        return Quaternion.Identity;
    }

    /// <summary>
    /// Calculates the direction of the specified axis based on the gizmo's space mode (Global or Local)
    /// and the provided object rotation in local space.
    /// </summary>
    /// <param name="axis">The axis for which the direction is to be calculated (X, Y, Z, or None).</param>
    /// <param name="objectRotation">The rotation of the object in local space, used when the gizmo's space mode is set to Local.</param>
    /// <returns>
    /// A <c>Vector3</c> representing the direction of the specified axis.
    /// If the axis is None, it returns a zero vector.
    /// </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the provided axis is not a valid <c>GizmoAxis</c> value.
    /// </exception>
    protected Vector3 GetAxisDirection(GizmoAxis axis, Quaternion objectRotation)
    {
        if (Space == GizmoSpace.Global)
        {
            return axis switch
            {
                GizmoAxis.X => Vector3.UnitX,
                GizmoAxis.Y => Vector3.UnitY,
                GizmoAxis.Z => Vector3.UnitZ,
                GizmoAxis.None => Vector3.Zero,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }

        return axis switch
        {
            GizmoAxis.X => Vector3.Transform(Vector3.UnitX, objectRotation),
            GizmoAxis.Y => Vector3.Transform(Vector3.UnitY, objectRotation),
            GizmoAxis.Z => Vector3.Transform(Vector3.UnitZ, objectRotation),
            GizmoAxis.None => Vector3.Zero,
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };
    }
}