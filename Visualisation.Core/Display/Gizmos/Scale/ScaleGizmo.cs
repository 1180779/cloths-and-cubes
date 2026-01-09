using System.Runtime.CompilerServices;

using Engine.Rays;

using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos.Scale;

public sealed class ScaleGizmo : GizmoBase, IGizmo<IScaleGizmoTarget>
{
    public delegate void TargetScaledEventHandler(Vector3 oldScale, Vector3 newScale);

    public event TargetScaledEventHandler TargetScaledEvent = delegate { };

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
        if (_target is not null)
            _offset = _target.Offset;
    }

    private Vector3 _offset;


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
            _selectedAxis = CheckIntersection(ray, _target.AxisPosition, camera);
            if (_selectedAxis != GizmoAxis.None)
            {
                _dragStartMouse = mousePos;
                _initialScale = _target.Scale;

                var rotation = AxisRotation;
                Vector3 axisDir = GetAxisDirection(_selectedAxis, rotation);
                _useScreenSpaceFallback = ShouldUseScreenSpaceFallback(ray, _target.AxisPosition, axisDir);

                // Calculate the closest point on the axis to the ray as the starting reference
                _dragStartPoint =
                    axisDir * GetProjectedMovementOnAxisSkewLine(ray, _target.AxisPosition, axisDir, new());

                _wasMouseDown = true;
                return true;
            }
        }
        // Handle dragging
        else if (isMouseDown && _wasMouseDown && _selectedAxis != GizmoAxis.None)
        {
            var rotation = AxisRotation;
            Vector3 axisDir = GetAxisDirection(_selectedAxis, rotation);

            float scaleChange = GetProjectedMovementOnAxis(ray, _dragStartMouse, mousePos,
                _target.AxisPosition, axisDir, _dragStartPoint, camera);

            float factor = 1.0f + scaleChange;
            if (factor < 0.1f) factor = 0.1f;

            var newScale = _target.GetTargetScale(_initialScale, factor, _selectedAxis);
            _target.Scale = newScale;
            InvokeGizmoTargetChangedByGizmo(_target);
            TargetScaledEvent.Invoke(_initialScale, newScale);

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
            _hoveredAxis = CheckIntersection(ray, _target.AxisPosition, camera);
            if (_hoveredAxis != GizmoAxis.None)
                return true;
        }

        return false;
    }


    private IScaleGizmoTarget? _target;

    public override IGizmoTarget? Target
    {
        get => _target;
        set
        {
            if (value is IScaleGizmoTarget target)
            {
                _target = target;
                return;
            }

            _target = null;
        }
    }

    protected override void BeforeCheckIntersection()
    {
        if (_target is not null)
            _offset = _target.Offset;
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