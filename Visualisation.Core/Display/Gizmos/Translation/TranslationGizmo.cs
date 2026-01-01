using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos.Translation;

public sealed class TranslationGizmo : GizmoBase, IGizmo<ITranslationGizmoTarget>
{
    public delegate void TargetMovedEventHandler(Vector3 oldPosition, Vector3 newPosition);

    public event TargetMovedEventHandler TargetMovedEvent = delegate { };

    private readonly GizmoArrow _arrow;

    private Vector3 _dragStartPoint;
    private Vector3 _initialPosition;
    private bool _wasMouseDown;

    public TranslationGizmo(Shader shader) : base(shader)
    {
        _arrow = new GizmoArrow(shaftLength: 1.0f, shaftRadius: 0.025f, tipHeight: 0.5f,
            tipRadius: 0.15f);
    }

    protected override IGizmoArrow Arrow => _arrow;
    private ITranslationGizmoTarget? _target;

    public override IGizmoTarget? Target
    {
        get => _target;
        set
        {
            if (value is ITranslationGizmoTarget target)
            {
                _target = target;
                return;
            }

            _target = null;
        }
    }

    public bool HandleInput(IInputProvider input, Vector2 mousePos, CameraBase camera, Vector2i screenSize)
    {
        if (_target is null) return false;

        var ray = SelectionManager.GetRayFromMouse(mousePos, camera, screenSize);
        bool isMouseDown = input.IsMouseButtonDown(MouseButton.Left);

        // Detect mouse button press (transition from up to down)
        if (isMouseDown && !_wasMouseDown)
        {
            _dragStartMouse = mousePos;
            _selectedAxis = CheckIntersection(ray, _target.AxisPosition, camera);
            if (_selectedAxis != GizmoAxis.None)
            {
                _initialPosition = _target.AxisPosition;

                var rotation = AxisRotation;
                Vector3 axisDir = GetAxisDirection(_selectedAxis, rotation);
                _useScreenSpaceFallback = ShouldUseScreenSpaceFallback(ray, _initialPosition, axisDir);

                // Calculate the closest point on the axis to the ray as the starting reference
                _dragStartPoint = axisDir * GetProjectedMovementOnAxisSkewLine(ray, _initialPosition, axisDir, new());

                _wasMouseDown = true;
                return true;
            }
        }
        // Handle dragging
        else if (isMouseDown && _wasMouseDown && _selectedAxis != GizmoAxis.None)
        {
            var rotation = AxisRotation;
            Vector3 moveDir = GetAxisDirection(_selectedAxis, rotation);

            float projectedMovement = GetProjectedMovementOnAxis(ray, _dragStartMouse, mousePos,
                _initialPosition, moveDir, _dragStartPoint, camera);
            var projectedDelta = projectedMovement * moveDir;

            var newPosition = _initialPosition + projectedDelta;
            _target.Position = newPosition;
            InvokeGizmoTargetChangedByGizmo(_target);
            TargetMovedEvent.Invoke(_initialPosition, newPosition);

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

    public void Dispose()
    {
        _arrow.Dispose();
    }
}