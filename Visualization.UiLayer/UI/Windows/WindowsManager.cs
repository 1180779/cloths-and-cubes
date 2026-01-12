using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

/// <summary>
/// Provides functionality for managing and interacting with multiple user interface windows.
/// The WindowsManager allows for registering, rendering, and handling input for all managed windows,
/// as well as state management for saving and restoring specific configurations of the windows.
/// </summary>
public sealed class WindowsManager : IDisposable
{
    private sealed record WindowEntry(IWindow Window)
    {
        public IWindow Window = Window;
        public bool IsOpen = true;
    }

    private readonly List<WindowEntry> _windows;
    private readonly List<WindowEntry> _manualWindows = [];

    public WindowsManager()
    {
        _windows = new();
    }

    public void Add(IWindow window)
    {
        _windows.Add(new WindowEntry(window));
    }

    public void AddManuallyDrawn(IWindow window)
    {
        _manualWindows.Add(new WindowEntry(window));
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

    public IWindow GetManualWindow(string name)
    {
        return _manualWindows.First(e => e.Window.Name == name).Window;
    }

    public void DrawManualWindow(string name)
    {
        var entry = _manualWindows.First(e => e.Window.Name == name);
        entry.Window.Draw(ref entry.IsOpen);
    }

    public sealed record State
    {
        public Dictionary<string, bool> WindowsOpenState { get; init; } = [];
        public Dictionary<string, bool> ManualWindowsOpenState { get; init; } = [];
    }

    public void Dispose()
    {
        foreach (var window in _windows)
        {
            if (window.Window is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        foreach (var manualWindow in _manualWindows)
        {
            if (manualWindow.Window is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public State SaveState()
    {
        var dict = new Dictionary<string, bool>();
        _windows.ForEach(e => dict.Add(e.Window.Name, e.IsOpen));

        var manDict = new Dictionary<string, bool>();
        _manualWindows.ForEach(e => manDict.Add(e.Window.Name, e.IsOpen));
        return new State { WindowsOpenState = dict, ManualWindowsOpenState = manDict };
    }

    public void RestoreState(State state)
    {
        foreach (var window in _windows)
        {
            if (state.WindowsOpenState.TryGetValue(window.Window.Name, out var isOpen))
            {
                window.IsOpen = isOpen;
            }
        }

        foreach (var manualWindow in _manualWindows)
        {
            if (state.ManualWindowsOpenState.TryGetValue(manualWindow.Window.Name, out var isOpen))
            {
                manualWindow.IsOpen = isOpen;
            }
        }
    }
}