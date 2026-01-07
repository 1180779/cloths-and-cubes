using ImGuiNET;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Visualisation.Core.Inputs;

using Visualization.UiLayer.UI;

using CursorState = Visualisation.Core.Inputs.CursorState;
using MouseButton = Visualisation.Core.Inputs.MouseButton;

namespace Visualization.UiLayer.Inputs;

public class OpenTKWithImGuiInputProvider : IInputProvider
{
    private readonly ImGuiController _imGuiController;
    private readonly GameWindow _gameWindow;
    private Vector2 _mousePosition, _lastMousePosition;
    private bool _isMousePositionSet;
    private CursorState _lastCursorState = CursorState.Normal;
    private float _mouseScrollDelta;

    public OpenTKWithImGuiInputProvider(GameWindow gameWindow, ImGuiController imGuiController)
    {
        this._gameWindow = gameWindow;
        this._imGuiController = imGuiController;

        // Subscribe to the mouse wheel event to capture scroll data
        this._gameWindow.MouseWheel += OnMouseWheel;
    }

    private void OnMouseWheel(MouseWheelEventArgs e)
    {
        _mouseScrollDelta = e.OffsetY;
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
        return _gameWindow.MouseState.IsButtonPressed(mappedButton);
    }

    public bool IsMouseButtonDown(MouseButton button)
    {
        OpenTK.Windowing.GraphicsLibraryFramework.MouseButton mappedButton = button switch
        {
            MouseButton.Left => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left,
            MouseButton.Right => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right,
            MouseButton.Middle => OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
        return _gameWindow.MouseState.IsButtonDown(mappedButton);
    }

    public float GetMouseScroll()
    {
        var delta = _mouseScrollDelta;
        _mouseScrollDelta = 0;
        return delta;
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

        return _gameWindow.KeyboardState.IsKeyDown(mappedKey);
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

        return _gameWindow.KeyboardState.IsKeyPressed(mappedKey);
    }

    public Vector2 GetMouseDelta()
    {
        if (!_isMousePositionSet)
        {
            return Vector2.Zero;
        }

        return new Vector2(_gameWindow.MouseState.X, _gameWindow.MouseState.Y) - _lastMousePosition;
    }

    public void UpdateMousePosition()
    {
        _lastMousePosition = _mousePosition;
        _mousePosition = new Vector2(_gameWindow.MouseState.X, _gameWindow.MouseState.Y);

        if (!_isMousePositionSet)
        {
            _isMousePositionSet = true;
        }
    }

    public Vector2 GetMousePosition()
    {
        return new Vector2(_gameWindow.MouseState.X, _gameWindow.MouseState.Y);
    }

    public void SetCursorState(CursorState state)
    {
        if (state != _lastCursorState)
        {
            // sync imGui with cursor state
            if (state == CursorState.Normal)
            {
                _gameWindow.MouseMove += _imGuiController.MouseMove;
            }
            else
            {
                _gameWindow.MouseMove -= _imGuiController.MouseMove;
            }

            _lastCursorState = state;
        }

        OpenTK.Windowing.Common.CursorState mappedState = state switch
        {
            CursorState.Normal => OpenTK.Windowing.Common.CursorState.Normal,
            CursorState.Confined => OpenTK.Windowing.Common.CursorState.Confined,
            CursorState.Grabbed => OpenTK.Windowing.Common.CursorState.Grabbed,
            CursorState.Hidden => OpenTK.Windowing.Common.CursorState.Hidden,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        _gameWindow.CursorState = mappedState;
    }

    public CursorState GetCursorState()
    {
        OpenTK.Windowing.Common.CursorState state = _gameWindow.CursorState;
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