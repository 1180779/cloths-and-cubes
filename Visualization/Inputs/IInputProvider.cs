namespace Visualization.Display.Inputs;

/// <summary>
/// Represents an interface for handling user input, including keyboard, mouse, and cursor control,
/// in a graphical application. Provides methods to query the state of inputs and manage cursor behavior.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Checks if a specified key is currently being pressed down.
    /// </summary>
    /// <param name="key">The key to check, represented as an <see cref="InputKey"/>.</param>
    /// <returns>
    /// A <see cref="bool"/> indicating whether the specified key is currently pressed.
    /// Returns true if the key is pressed, otherwise false.
    /// </returns>
    bool IsKeyDown(InputKey key);


    /// <summary>
    /// Determines if a specific mouse button is currently being pressed.
    /// </summary>
    /// <param name="button">The mouse button to check, represented as a <see cref="MouseButton"/>.</param>
    /// <returns>
    /// A <see cref="bool"/> indicating whether the specified mouse button is currently pressed.
    /// Returns true if the button is pressed, otherwise false.
    /// </returns>
    public bool IsMouseButtonPressed(MouseButton button);

    /// <summary>
    /// Calculates and retrieves the difference between the current mouse position and the last recorded position.
    /// This allows tracking of mouse movement (delta) across frames. If this is the first call of the method after
    /// initialization, it will return zero to ensure no unintended deltas are reported.
    /// </summary>
    /// <returns>
    /// A <see cref="Vector2"/> representing the change in mouse position since the last recorded state.
    /// </returns>
    Vector2 GetMouseDelta();

    /// <summary>
    /// Updates the current state of the mouse position by capturing the latest mouse coordinates
    /// from the associated input system.
    /// </summary>
    void UpdateMousePosition();

    /// <summary>
    /// Retrieves the current position of the mouse within the game window.
    /// The coordinates are based on the input system associated with the window.
    /// </summary>
    /// <returns>
    /// A <see cref="Vector2"/> representing the current mouse position in screen space.
    /// </returns>
    Vector2 GetMousePosition();

    /// <summary>
    /// Sets the cursor state to the specified mode within the application.
    /// </summary>
    /// <param name="state">The desired cursor state, defined by the <see cref="CursorState"/> enumeration.</param>
    void SetCursorState(CursorState state);

    /// <summary>
    /// Retrieves the current state of the cursor.
    /// </summary>
    /// <returns>
    /// A <see cref="CursorState"/> indicating the current state of the cursor.
    /// </returns>
    CursorState GetCursorState();
}