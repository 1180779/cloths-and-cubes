using Visualisation.Core.Inputs;
using Visualization.Display.Inputs;

namespace Visualization.Display.Cameras;

public class CamerasManager
{
    private List<CameraBase> cameras = new();

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

        // update CameraMode
        if (input.IsKeyPressed(InputKey.Escape))
        {
            input.SetCursorState(CursorState.Normal);
            CameraMode = false;
        }

        // move to the next camera
        if (input.IsKeyPressed(InputKey.C))
        {
            CurrentCameraIndex++;
        }

        // process the current camera
        CurrentCamera.ProcessInput(input, dt);
    }

    public void Init(IInputProvider input)
    {
        if (CameraMode)
        {
            input.SetCursorState(CursorState.Grabbed);
        }
    }
}