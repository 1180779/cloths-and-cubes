using Visualisation.Core.Inputs;
using Visualization.Display.Cameras;
using Visualization.Display.Inputs;
using Visualization.Display.VisualObjects;

namespace Visualisation.Core.Display.Cameras;

public class FollowingCamera : CameraBase
{
    private VisualObjectBase[] targetObjects = [];
    private int currentTargetIndex;

    public VisualObjectBase? TargetObject => targetObjects.Length != 0 ? targetObjects[currentTargetIndex] : null;

    public int CurrentTargetIndex
    {
        get => currentTargetIndex;
        set
        {
            currentTargetIndex = value % targetObjects.Length;
            if (currentTargetIndex < 0)
                currentTargetIndex += targetObjects.Length;
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

    public void AttachTo(VisualObjectBase target)
    {
        targetObjects = [target];
        UpdatePositionFromTarget();
    }

    public void AttachTo(VisualObjectBase[] targets)
    {
        targetObjects = targets;
    }

    private Vector3 CurrentTarget => targetObjects.Length != 0 ? targetObjects[CurrentTargetIndex].Position : Target;

    private void UpdatePositionFromTarget()
    {
        // Keep looking at the target: Position is target - front * distance
        Position = CurrentTarget - Front * Distance;
    }

    public override void ProcessInput(IInputProvider input, float dt)
    {
        var mouseDelta = input.GetMouseDelta();
        Yaw += mouseDelta.X * Sensitivity;
        Pitch -= mouseDelta.Y * Sensitivity;

        if (input.IsKeyPressed(InputKey.N))
        {
            CurrentTargetIndex++;
        }

        if (input.IsKeyPressed(InputKey.M))
        {
            CurrentTargetIndex--;
        }

        UpdatePositionFromTarget();
    }
}