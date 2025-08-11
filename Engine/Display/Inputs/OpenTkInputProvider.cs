using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Display.Inputs;

public class OpenTkInputProvider : IInputProvider
{
    private readonly GameWindow _window;
    private Vector2 _lastMousePos;
    private bool _firstMove = true;

    public OpenTkInputProvider(GameWindow window)
    {
        _window = window;
    }

    public bool IsKeyDown(InputKey key)
    {
        Keys mappedKey = key switch
        {
            InputKey.Escape => Keys.Escape,
            InputKey.W => Keys.W,
            InputKey.A => Keys.A,
            InputKey.S => Keys.S,
            InputKey.D => Keys.D,
            InputKey.Space => Keys.Space,
            InputKey.LeftShift => Keys.LeftShift,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        return _window.KeyboardState.IsKeyDown(mappedKey);
    }

    public Vector2 GetMouseDelta()
    {
        if (_firstMove)
        {
            return Vector2.Zero;
        }
        
        return new Vector2(_window.MouseState.X, _window.MouseState.Y) - _lastMousePos;
    }

    public void UpdateMousePosition()
    {
        if (_firstMove)
        {
            _firstMove = false;
        }
        _lastMousePos = new Vector2(_window.MouseState.X, _window.MouseState.Y);
    }

    public Vector2 GetMousePosition()
    {
        return new Vector2(_window.MouseState.X, _window.MouseState.Y);
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
        _window.CursorState = mappedState;
    }

    public CursorState GetCursorState()
    {
        OpenTK.Windowing.Common.CursorState state = _window.CursorState;
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