using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class FreeMovingCamera : CameraBase
{
    public FreeMovingCamera(float aspectRatio) : base(aspectRatio)
    {
    }

    public FreeMovingCamera(Vector3 position, float aspectRatio) : base(position, aspectRatio)
    {
    }

    public override void ProcessInput(IInputProvider input, float dt)
    {
        if (input.IsKeyDown(InputKey.W))
        {
            Position += Front * CameraSpeed * dt; // Forward
        }

        if (input.IsKeyDown(InputKey.S))
        {
            Position -= Front * CameraSpeed * dt; // Backwards
        }

        if (input.IsKeyDown(InputKey.A))
        {
            Position -= Right * CameraSpeed * dt; // Left
        }

        if (input.IsKeyDown(InputKey.D))
        {
            Position += Right * CameraSpeed * dt; // Right
        }

        if (input.IsKeyDown(InputKey.Space))
        {
            Position += Up * CameraSpeed * dt; // Up
        }

        if (input.IsKeyDown(InputKey.LeftShift))
        {
            Position -= Up * CameraSpeed * dt; // Down
        }

        var mouseDelta = input.GetMouseDelta();
        YawDegrees += mouseDelta.X * Sensitivity;
        PitchDegrees -= mouseDelta.Y * Sensitivity; // Reversed since y-coordinates range from bottom to top
    }

    private Int64 i = 0;
}