using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Controls;
using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos;

public sealed class TranslationGizmo : GizmoBase, IGizmo
{
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

    public bool HandleInput(IInputProvider input, Vector2 mousePos, CameraBase camera, Vector2i screenSize)
    {
        if (_target == null) return false;

        var ray = SelectionManager.GetRayFromMouse(mousePos, camera, screenSize);
        bool isMouseDown = input.IsMouseButtonDown(MouseButton.Left);

        // Detect mouse button press (transition from up to down)
        if (isMouseDown && !_wasMouseDown)
        {
            _dragStartMouse = mousePos;
            _selectedAxis = CheckIntersection(ray, _target.Position, camera);
            if (_selectedAxis != GizmoAxis.None)
            {
                _initialPosition = _target.Position;

                var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;
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
            var rotation = Space == GizmoSpace.Local ? GetObjectRotation(_target) : Quaternion.Identity;
            Vector3 moveDir = GetAxisDirection(_selectedAxis, rotation);

            float projectedMovement = GetProjectedMovementOnAxis(ray, _dragStartMouse, mousePos,
                _initialPosition, moveDir, _dragStartPoint, camera);
            var projectedDelta = projectedMovement * moveDir;

            UpdateTargetPosition(_target, _initialPosition + projectedDelta);

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

    private void UpdateTargetPosition(GameObject target, Vector3 newPosition)
    {
        if (target is GameObjectRigidBody rb)
        {
            rb.EngineRigidBody.Position = new Engine.Vector3(newPosition.X, newPosition.Y, newPosition.Z);
            rb.EngineRigidBody.Velocity = new(); // zero the velocity to avoid it accumulating
            rb.EngineRigidBody.SetAwake();
            rb.EngineRigidBody.CalculateDerivedData();
        }

        // TODO: schedule the BVH recalculation if simulation is not running
    }

    public void Dispose()
    {
        _arrow.Dispose();
    }
}