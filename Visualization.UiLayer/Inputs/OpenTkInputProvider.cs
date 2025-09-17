using OpenTK.Windowing.Desktop;
using Visualisation.Core.Inputs;
using MouseButton = Visualisation.Core.Inputs.MouseButton;

namespace Visualization.UiLayer.Inputs;

public class OpenTkInputProvider : IInputProvider
{
    private readonly GameWindow gameWindow;
    private Vector2 lastMousePosition;
    private bool isMousePositionSet = true;

    public OpenTkInputProvider(GameWindow gameWindow)
    {
        this.gameWindow = gameWindow;
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
        if (!KeyMap.OpenTkKeysMap.TryGetValue(key, out var mappedKey))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return gameWindow.KeyboardState.IsKeyDown(mappedKey);
    }

    public bool IsKeyPressed(InputKey key)
    {
        if (!KeyMap.OpenTkKeysMap.TryGetValue(key, out var mappedKey))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return gameWindow.KeyboardState.IsKeyPressed(mappedKey);
    }

    public Vector2 GetMouseDelta()
    {
        if (isMousePositionSet)
        {
            return Vector2.Zero;
        }

        return new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y) - lastMousePosition;
    }

    public void UpdateMousePosition()
    {
        if (isMousePositionSet)
        {
            isMousePositionSet = false;
        }

        lastMousePosition = new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y);
    }

    public Vector2 GetMousePosition()
    {
        return new Vector2(gameWindow.MouseState.X, gameWindow.MouseState.Y);
    }

    public void SetCursorState(CursorState state)
    {
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