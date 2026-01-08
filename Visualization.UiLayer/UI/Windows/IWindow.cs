using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public interface IWindow
{
    /// <summary>
    /// Gets the display name of the window.
    /// This property provides a user-friendly identifier used for distinguishing windows
    /// in the user interface and menu structures. The value is expected to be unique across
    /// all registered windows within the application.
    /// </summary>
    string Name { get; }


    /// <summary>
    /// Draws the content of the window. This method is invoked to render the UI components of a window.
    /// </summary>
    /// <param name="isOpen">A reference to a boolean indicating whether the window is open.
    /// The value should be passed to the ImGui.Begin(...) function. </param>
    void Draw(ref bool isOpen);

    /// <summary>
    /// Handles user input for the window. This method processes key events and updates
    /// relevant properties or states based on the user's interactions.
    /// </summary>
    /// <note>
    /// Should use ImGui key and mouse functions to check for input events.
    /// If the input is to be handled only when the window is focused, the implementation should save the focus state
    /// with <see cref="ImGui.IsItemHovered()"/> or <see cref="ImGui.IsItemFocused"/> in the Draw method and check it here. 
    /// </note>
    public void HandleInput() { }
}