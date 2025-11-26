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

    private readonly List<CameraBase> _cameras = [];

    private int _currentCameraIndex = 0;

    public int CurrentCameraIndex
    {
        get => _currentCameraIndex;
        set => _currentCameraIndex = value % _cameras.Count;
    }

    public void AddCamera(CameraBase camera)
    {
        _cameras.Add(camera);
    }

    public void RemoveCamera(CameraBase camera)
    {
        _cameras.Remove(camera);
    }

    public void RemoveCurrentCamera()
    {
        _cameras.RemoveAt(CurrentCameraIndex);
        CurrentCameraIndex--;
    }

    public CameraBase CurrentCamera => _cameras[CurrentCameraIndex];

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