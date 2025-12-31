using System.Runtime.CompilerServices;

using Engine.Rays;

using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Controls;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

using Box = Visualisation.Core.GameObjects.Box;
using Cone = Visualisation.Core.GameObjects.Cone;
using Cylinder = Visualisation.Core.GameObjects.Cylinder;

namespace Visualisation.Core.Display.Gizmos;

public sealed class ScaleGizmo : GizmoBase, IGizmo
{
    private readonly GizmoBoxArrow _boxArrow;

    private Vector3 _initialScale;
    private Vector3 _dragStartPoint;
    private bool _wasMouseDown;

    public ScaleGizmo(Shader shader) : base(shader)
    {
        _boxArrow = new(shaftLength: 1.0f, shaftRadius: 0.025f, boxSize: 0.5f);

        // Reasonable defaults
        ConstantScreenSize = false;
        Space = GizmoSpace.Local;
    }

    protected override IGizmoArrow Arrow => _boxArrow;

    protected override void BeforeRender()
    {
        CaptureOffset();
    }

    private Vector3 _offset;

    private void CaptureOffset()
    {
        if (_target is Box box)
        {
            _offset = box.EngineBox.HalfSize.ToOpenTK();
        }
        else if (_target is Ball ball)
        {
            _offset = new Vector3(ball.EngineBall.Radius);
        }
        else if (_target is Cylinder cyl)
        {
            _offset = new Vector3(cyl.EngineCylinder.Radius, cyl.EngineCylinder.Radius,
                cyl.EngineCylinder.Height / 2.0f);
        }
        else if (_target is Cone cone)
        {
            _offset = new Vector3(cone.EngineCone.Radius, cone.EngineCone.Radius, cone.EngineCone.Height / 2.0f);
        }
    }

    protected override void RenderAxis(
        GizmoAxis axis,
        Vector3 position,
        Quaternion rotation,
        Vector4 defaultColor,
        float handleSize)
    {
        AdjustArrowShaftLength(axis);
        base.RenderAxis(axis, position, rotation, defaultColor, handleSize);
    }

    public bool HandleInput(
        IInputProvider input,
        Vector2 mousePos,
        CameraBase camera,
        Vector2i screenSize)
    {
        if (_target == null) return false;

        var ray = SelectionManager.GetRayFromMouse(mousePos, camera, screenSize);
        bool isMouseDown = input.IsMouseButtonDown(MouseButton.Left);

        // Detect mouse button press (transition from up to down)
        if (isMouseDown && !_wasMouseDown)
        {
            _selectedAxis = CheckIntersection(ray, _target.Position, camera);
            if (_selectedAxis != GizmoAxis.None)
            {
                _dragStartMouse = mousePos;
                CaptureInitialScale(_target);

                var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;
                Vector3 axisDir = GetAxisDirection(_selectedAxis, rotation);
                _useScreenSpaceFallback = ShouldUseScreenSpaceFallback(ray, _target.Position, axisDir);

                // Calculate the closest point on the axis to the ray as the starting reference
                _dragStartPoint = axisDir * GetProjectedMovementOnAxisSkewLine(ray, _target.Position, axisDir, new());

                _wasMouseDown = true;
                return true;
            }
        }
        // Handle dragging
        else if (isMouseDown && _wasMouseDown && _selectedAxis != GizmoAxis.None)
        {
            var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;
            Vector3 axisDir = GetAxisDirection(_selectedAxis, rotation);

            float scaleChange = GetProjectedMovementOnAxis(ray, _dragStartMouse, mousePos,
                _target.Position, axisDir, _dragStartPoint, camera);

            float factor = 1.0f + scaleChange;
            if (factor < 0.1f) factor = 0.1f;

            UpdateTargetScale(_target, factor, _selectedAxis);

            return true;
        }
        // Detect mouse button release
        else if (!isMouseDown && _wasMouseDown)
        {
            _selectedAxis = GizmoAxis.None;
            _wasMouseDown = false;
        }
        // No selection happening; handle hover
        else
        {
            _hoveredAxis = CheckIntersection(ray, _target.Position, camera);
            if (_hoveredAxis != GizmoAxis.None)
                return true;
        }

        return false;
    }

    private void CaptureInitialScale(GameObject target)
    {
        if (target is Box box)
        {
            _initialScale = box.EngineBox.HalfSize.ToOpenTK();
        }
        else if (target is Ball ball)
        {
            _initialScale = new Vector3(ball.EngineBall.Radius);
        }
        else if (target is Cylinder cyl)
        {
            _initialScale = new Vector3(cyl.EngineCylinder.Radius, cyl.EngineCylinder.Radius,
                cyl.EngineCylinder.Height);
        }
        else if (target is Cone cone)
        {
            _initialScale = new Vector3(cone.EngineCone.Radius, cone.EngineCone.Radius,
                cone.EngineCone.Height);
        }
    }

    private void UpdateTargetScale(GameObject target, float factor, GizmoAxis axis)
    {
        if (target is Box box)
        {
            var newHalfSize = _initialScale;
            if (axis == GizmoAxis.X) newHalfSize.X *= factor;
            if (axis == GizmoAxis.Y) newHalfSize.Y *= factor;
            if (axis == GizmoAxis.Z) newHalfSize.Z *= factor;

            box.EngineBox.HalfSize = newHalfSize.ToEngine();
            box.EngineBox.Body.SetAwake();
            box.EngineBox.Body.CalculateDerivedData();
        }
        else if (target is Ball ball)
        {
            ball.EngineBall.Radius = _initialScale.X * factor;
            ball.EngineBall.Body.SetAwake();
            ball.EngineBall.Body.CalculateDerivedData();
        }
        else if (target is Cylinder cylinder)
        {
            if (axis == GizmoAxis.Z)
            {
                cylinder.EngineCylinder.Height = _initialScale.Z * factor;
            }
            else
            {
                cylinder.EngineCylinder.Radius = _initialScale.X * factor;
            }

            cylinder.EngineCylinder.Body.SetAwake();
            cylinder.EngineCylinder.Body.CalculateDerivedData();
        }
        else if (target is Cone cone)
        {
            if (axis == GizmoAxis.Z)
            {
                cone.EngineCone.Height = _initialScale.Z * factor;
            }
            else
            {
                cone.EngineCone.Radius = _initialScale.X * factor;
            }

            cone.EngineCone.Body.SetAwake();
            cone.EngineCone.Body.CalculateDerivedData();
        }
    }

    protected override void BeforeCheckIntersection()
    {
        CaptureOffset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdjustArrowShaftLength(GizmoAxis axis)
    {
        _boxArrow.ShaftLength = axis switch
        {
            GizmoAxis.X => _offset.X,
            GizmoAxis.Y => _offset.Y,
            GizmoAxis.Z => _offset.Z,
            _ => _boxArrow.ShaftLength
        };
    }

    protected override void CheckArrowIntersection(
        Ray ray,
        Vector3 position,
        Quaternion rotation,
        GizmoAxis axis,
        float handleScale,
        ref GizmoAxis hitAxis,
        ref float closestDist)
    {
        AdjustArrowShaftLength(axis);
        base.CheckArrowIntersection(ray, position, rotation, axis, handleScale, ref hitAxis, ref closestDist);
    }

    public void Dispose()
    {
        _boxArrow.Dispose();
    }
}