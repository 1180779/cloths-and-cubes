using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

/// <summary>
/// Provides functionality for managing and interacting with multiple user interface windows.
/// The WindowsManager allows for registering, rendering, and handling input for all managed windows,
/// as well as state management for saving and restoring specific configurations of the windows.
/// </summary>
public sealed class WindowsManager
{
    private sealed record WindowEntry(IWindow Window)
    {
        public IWindow Window = Window;
        public bool IsOpen = true;
    }

    private readonly List<WindowEntry> _windows;

    public WindowsManager()
    {
        _windows = new();
    }

    public void Add(IWindow window)
    {
        _windows.Add(new WindowEntry(window));
    }

    /// <summary>
    /// Renders the user interface for all windows managed by the WindowsManager.
    /// Iterates through the registered windows and invokes their individual Draw methods to display and update their content.
    /// </summary>
    public void Draw()
    {
        _windows.ForEach(window => window.Window.Draw(ref window.IsOpen));
    }

    /// <summary>
    /// Processes user input for all windows managed by the WindowsManager.
    /// Iterates through the registered windows and invokes their individual HandleInput methods
    /// to detect and handle user interactions such as key presses or mouse clicks.
    /// </summary>
    public void HandleInput()
    {
        _windows.ForEach(window => window.Window.HandleInput());
    }

    /// <summary>
    /// Renders the main menu bar with a "Windows" menu containing a list of registered windows.
    /// Displays each window's name as a menu item and allows toggling the visibility
    /// of individual windows.
    /// </summary>
    public void DrawMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Windows"))
            {
                _windows.ForEach(w => ImGui.MenuItem(w.Window.Name, "", ref w.IsOpen));
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    public IWindow GetWindow(string name)
    {
        return _windows.First(e => e.Window.Name == name).Window;
    }

    public sealed record State
    {
        public Dictionary<string, bool> AreOpen { get; init; } = [];
    }

    public State SaveState()
    {
        var dict = new Dictionary<string, bool>();
        _windows.ForEach(e => dict.Add(e.Window.Name, e.IsOpen));

        return new State { AreOpen = dict };
    }

    public void RestoreState(State state)
    {
        foreach (var window in _windows)
        {
            if (state.AreOpen.TryGetValue(window.Window.Name, out var isOpen))
            {
                window.IsOpen = isOpen;
            }
        }
    }
}