using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class FollowingCamera : CameraBase
{
    private AbstractVisualObject[] targetObjects = [];
    private int currentTargetIndex;

    public AbstractVisualObject? TargetObject => targetObjects.Length != 0 ? targetObjects[currentTargetIndex] : null;

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

    public void AttachTo(AbstractVisualObject target)
    {
        targetObjects = [target];
        UpdatePositionFromTarget();
    }

    public void AttachTo(AbstractVisualObject[] targets)
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

        Distance += input.GetMouseScroll() * 0.5f;
        input.ResetMouseScroll();
        UpdatePositionFromTarget();
    }
}