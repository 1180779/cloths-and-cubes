using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class FollowingCamera : CameraBase
{
    private GameObject[] _targetObjects = [];
    private int _currentTargetIndex;

    public GameObject? TargetObject => _targetObjects.Length != 0 ? _targetObjects[_currentTargetIndex] : null;

    public int CurrentTargetIndex
    {
        get => _currentTargetIndex;
        set
        {
            _currentTargetIndex = value % _targetObjects.Length;
            if (_currentTargetIndex < 0)
                _currentTargetIndex += _targetObjects.Length;
        }
    }

    // default target if no target objects are attached
    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Distance { get; set; } = 5f;

    public FollowingCamera(float aspectRatio) : base(aspectRatio)
    {
    }

    public FollowingCamera(Vector3 position, float aspectRatio) : base(position, aspectRatio)
    {
    }

    public void AttachTo(GameObject target)
    {
        _targetObjects = [target];
        UpdatePositionFromTarget();
    }

    public void AttachTo(GameObject[] targets)
    {
        _targetObjects = targets;
    }

    private Vector3 CurrentTarget => _targetObjects.Length != 0 ? _targetObjects[CurrentTargetIndex].Position : Target;

    private void UpdatePositionFromTarget()
    {
        // Keep looking at the target: Position is target - front * distance
        Position = CurrentTarget - Front * Distance;
    }

    public override void ProcessInput(IInputProvider input, float dt)
    {
        var mouseDelta = input.GetMouseDelta();
        YawDegrees += mouseDelta.X * Sensitivity;
        PitchDegrees -= mouseDelta.Y * Sensitivity;

        if (input.IsKeyPressed(InputKey.N))
        {
            CurrentTargetIndex++;
        }

        if (input.IsKeyPressed(InputKey.M))
        {
            CurrentTargetIndex--;
        }

        Distance -= input.GetMouseScroll() * 0.5f;
        input.ResetMouseScroll();
        UpdatePositionFromTarget();
    }
}