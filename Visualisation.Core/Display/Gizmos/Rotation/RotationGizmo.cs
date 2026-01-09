using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos.Rotation;

/// <summary>
/// A rotation gizmo that allows rotating objects around X, Y, or Z axes.
/// Uses torus rings for each axis.
/// </summary>
public sealed class RotationGizmo : GizmoBase, IGizmo<IRotationGizmoTarget>
{
    public delegate void TargetRotatedEventHandler(Quaternion oldRotation, Quaternion newRotation);

    public event TargetRotatedEventHandler TargetRotatedEvent = delegate { };

    private readonly GizmoRing _ring = new();

    public RotationGizmo(Shader shader) : base(shader) { }

    /// <summary>
    /// Rotation sensitivity. Higher values = faster rotation. Default is 0.01f.
    /// </summary>
    public float Sensitivity = 0.01f;

    private IRotationGizmoTarget? _target;
    private Quaternion _initialRotation;
    private Vector3 _initialRotationAxis;
    private bool _wasMouseDown;

    protected override IGizmoArrow Arrow => _ring;

    public override IGizmoTarget? Target
    {
        get => _target;
        set
        {
            if (value is IRotationGizmoTarget target)
            {
                _target = target;
                return;
            }

            _target = null;
        }
    }


    public bool HandleInput(
        IInputProvider input,
        Vector2 mousePos,
        CameraBase camera,
        Vector2i screenSize)
    {
        if (_target is null) return false;

        var ray = SelectionManager.GetRayFromMouse(mousePos, camera, screenSize);
        bool isMouseDown = input.IsMouseButtonDown(MouseButton.Left);

        // Detect mouse button press (transition from up to down)
        if (isMouseDown && !_wasMouseDown)
        {
            _selectedAxis = CheckIntersection(ray, _target.AxisPosition, camera);
            if (_selectedAxis != GizmoAxis.None)
            {
                _dragStartMouse = mousePos;
                _initialRotation = _target.Orientation;

                var rotation = Space == GizmoSpace.Local ? _initialRotation : Quaternion.Identity;
                _initialRotationAxis = GetAxisDirection(_selectedAxis, rotation);

                _wasMouseDown = true;
                return true;
            }
        }
        // Handle dragging
        else if (isMouseDown && _wasMouseDown && _selectedAxis != GizmoAxis.None)
        {
            Vector3 rotationAxis = _initialRotationAxis;

            float deltaAngle = GetScreenSpaceAxisDelta(_dragStartMouse, mousePos, rotationAxis, camera, Sensitivity);

            var newRotation = GetNewRotation(rotationAxis, deltaAngle);
            _target.Orientation = newRotation;
            InvokeGizmoTargetChangedByGizmo(_target);
            TargetRotatedEvent(_initialRotation, newRotation);

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


    private Quaternion GetNewRotation(Vector3 axis, float angle)
    {
        Quaternion deltaRotation = Quaternion.FromAxisAngle(axis, angle);
        Quaternion newRotation = deltaRotation * _initialRotation;
        newRotation.Normalize();
        return newRotation;
    }

    public void Dispose()
    {
        _shader.Dispose();
        _ring.Dispose();
    }
}