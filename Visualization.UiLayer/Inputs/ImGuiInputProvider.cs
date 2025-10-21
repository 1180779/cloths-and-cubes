using ImGuiNET;
using OpenTK.Windowing.Desktop;
using Visualisation.Core.Inputs;
using Visualization.UiLayer.UI;
using MouseButton = Visualisation.Core.Inputs.MouseButton;

namespace Visualization.UiLayer.Inputs;

public class ImGuiInputProvider : IInputProvider
{
    private readonly ImGuiController imGuiController;
    private readonly GameWindow gameWindow;
    private Vector2 mousePosition, lastMousePosition;
    private bool isMousePositionSet;
    private CursorState lastCursorState = CursorState.Normal;

    public ImGuiInputProvider(GameWindow gameWindow, ImGuiController imGuiController)
    {
        this.gameWindow = gameWindow;
        this.imGuiController = imGuiController;
    }

    public bool IsMouseButtonPressed(MouseButton button)
    {
        OpenTK.Windowing.GraphicsLibraryFramework.MouseButton mappedButton = button switch
        {
            MouseButton.Left => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left,
            MouseButton.Right => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right,
            MouseButton.Middle => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
        return gameWindow.MouseState.IsButtonPressed(mappedButton);
    }

    public bool IsKeyDown(InputKey key)
    {
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            return false;
        }

        if (!KeyMap.OpenTkKeysMap.TryGetValue(key, out var mappedKey))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return gameWindow.KeyboardState.IsKeyDown(mappedKey);
    }

    public bool IsKeyPressed(InputKey key)
    {
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            return false;
        }

        if (!KeyMap.OpenTkKeysMap.TryGetValue(key, out var mappedKey))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return gameWindow.KeyboardState.IsKeyPressed(mappedKey);
    }

    public Vector2 GetMouseDelta()
    {
        if (!isMousePositionSet)
        {
            return Vector2.Zero;
        }

        return new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y) - lastMousePosition;
    }

    public void UpdateMousePosition()
    {
        lastMousePosition = mousePosition;
        mousePosition = new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y);

        if (!isMousePositionSet)
        {
            isMousePositionSet = true;
        }
    }

    public Vector2 GetMousePosition()
    {
        return new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y);
    }

    public void SetCursorState(CursorState state)
    {
        if (state != lastCursorState)
        {
            // sync imGui with cursor state
            if (state == CursorState.Normal)
            {
                gameWindow.MouseMove += imGuiController.MouseMove;
            }
            else
            {
                gameWindow.MouseMove -= imGuiController.MouseMove;
            }

            lastCursorState = state;
        }

        OpenTK.Windowing.Common.CursorState mappedState = state switch
        {
            CursorState.Normal => OpenTK.Windowing.Common.CursorState.Normal,
            CursorState.Confined => OpenTK.Windowing.Common.CursorState.Confined,
            CursorState.Grabbed => OpenTK.Windowing.Common.CursorState.Grabbed,
            CursorState.Hidden => OpenTK.Windowing.Common.CursorState.Hidden,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        gameWindow.CursorState = mappedState;
    }

    public CursorState GetCursorState()
    {
        OpenTK.Windowing.Common.CursorState state = gameWindow.CursorState;
        CursorState mappedState = state switch
        {
            OpenTK.Windowing.Common.CursorState.Normal => CursorState.Normal,
            OpenTK.Windowing.Common.CursorState.Confined => CursorState.Confined,
            OpenTK.Windowing.Common.CursorState.Grabbed => CursorState.Grabbed,
            OpenTK.Windowing.Common.CursorState.Hidden => CursorState.Hidden,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        return mappedState;
    }
}