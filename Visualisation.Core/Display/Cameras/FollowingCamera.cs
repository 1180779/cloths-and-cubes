using Visualisation.Core.GameObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class FollowingCamera : CameraBase
{
    private int _currentTargetIndex;
    public Func<GameObject[]> GetTargetObjects { get; set; }

    public GameObject? TargetObject
    {
        get
        {
            var targetObjects = GetTargetObjects();
            return targetObjects.Length != 0 ? GetTargetObjects()[_currentTargetIndex] : null;
        }
    }

    public int CurrentTargetIndex
    {
        get => _currentTargetIndex;
        set
        {
            var targetObjectsLength = GetTargetObjects().Length;
            _currentTargetIndex = value % targetObjectsLength;
            if (_currentTargetIndex < 0)
                _currentTargetIndex += targetObjectsLength;
        }
    }

    // default target if no target objects are attached
    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Distance { get; set; } = 5f;

    public FollowingCamera(Func<GameObject[]> getTargetObjects) : base()
    {
        GetTargetObjects = getTargetObjects;
        UpdatePositionFromTarget();
    }

    public FollowingCamera(Func<GameObject[]> getTargetObjects, Vector3 position) : base(position)
    {
        GetTargetObjects = getTargetObjects;
        UpdatePositionFromTarget();
    }

    private Vector3 CurrentTarget
    {
        get
        {
            var targetObjects = GetTargetObjects();
            return targetObjects.Length != 0 ? targetObjects[CurrentTargetIndex].Position : Target;
        }
    }


    private void UpdatePositionFromTarget()
    {
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