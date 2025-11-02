using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class CamerasManager
{
    public CamerasManager(IInputProvider inputProvider)
    {
        if (CameraMode)
        {
            inputProvider.SetCursorState(CursorState.Grabbed);
        }
    }

    private readonly List<CameraBase> cameras = [];

    private int currentCameraIndex = 0;

    public int CurrentCameraIndex
    {
        get => currentCameraIndex;
        set => currentCameraIndex = value % cameras.Count;
    }

    public void AddCamera(CameraBase camera)
    {
        cameras.Add(camera);
    }

    public void RemoveCamera(CameraBase camera)
    {
        cameras.Remove(camera);
    }

    public void RemoveCurrentCamera()
    {
        cameras.RemoveAt(CurrentCameraIndex);
        CurrentCameraIndex--;
    }

    public CameraBase CurrentCamera => cameras[CurrentCameraIndex];

    public bool CameraMode { get; set; }

    public void ProcessInputOutOfFocus(IInputProvider input, float dt)
    {
        if (CameraMode)
        {
            CameraMode = false;
        }
    }

    public void ProcessInput(IInputProvider input, float dt)
    {
        if (!CameraMode)
        {
            if (input.IsMouseButtonPressed(MouseButton.Left))
            {
                input.SetCursorState(CursorState.Grabbed);
                CameraMode = true;
            }

            return;
        }

        if (input.IsKeyPressed(InputKey.Escape))
        {
            input.SetCursorState(CursorState.Normal);
            CameraMode = false;
        }

        if (input.IsKeyPressed(InputKey.C))
        {
            CurrentCameraIndex++;
        }

        CurrentCamera.ProcessInput(input, dt);
    }
}