using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Controls;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos;

/// <summary>
/// A rotation gizmo that allows rotating objects around X, Y, or Z axes.
/// Uses torus rings for each axis.
/// </summary>
public sealed class RotationGizmo : GizmoBase, IGizmo
{
    public delegate void TargetRotatedEventHandler(Quaternion oldRotation, Quaternion newRotation);

    public event TargetRotatedEventHandler TargetRotatedEvent = delegate { };

    private readonly GizmoRing _ring = new();

    public RotationGizmo(Shader shader) : base(shader) { }

    /// <summary>
    /// Rotation sensitivity. Higher values = faster rotation. Default is 0.01f.
    /// </summary>
    public float Sensitivity = 0.01f;

    private Quaternion _initialRotation;
    private Vector3 _initialRotationAxis;
    private bool _wasMouseDown;

    protected override IGizmoArrow Arrow => _ring;

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
                _initialRotation = GetObjectRotation(_target);

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
            ApplyNewRotation(_target, newRotation);
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
            _hoveredAxis = CheckIntersection(ray, _target.Position, camera);
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

    public void ApplyNewRotation(GameObjectCollisionPrimitive target, Quaternion newRotation)
    {
        target.EngineCollisionPrimitive.Body.Orientation = newRotation.ToEngine();
        target.EngineCollisionPrimitive.Body.SetAwake();
        target.EngineCollisionPrimitive.Body.CalculateDerivedData();
        target.EngineCollisionPrimitive.CalculateInternals();
    }

    public void Dispose()
    {
        _shader.Dispose();
        _ring.Dispose();
    }
}