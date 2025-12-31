using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public class WindowsManager
{
    private readonly List<IWindow> _windows;
    private readonly List<bool> _areOpen;

    public WindowsManager()
    {
        _windows = new List<IWindow>();
        _areOpen = new List<bool>();
    }

    public void Add(IWindow window)
    {
        _windows.Add(window);
        _areOpen.Add(true);
    }

    public void Draw()
    {
        for (var i = 0; i < _windows.Count; i++)
        {
            var isOpen = _areOpen[i];
            if (isOpen)
            {
                _windows[i].Draw(ref isOpen);
                _areOpen[i] = isOpen;
            }
        }
    }

    public void DrawMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Windows"))
            {
                for (var i = 0; i < _windows.Count; i++)
                {
                    var isOpen = _areOpen[i];
                    if (ImGui.MenuItem(_windows[i].Name, "", ref isOpen))
                    {
                        _areOpen[i] = isOpen;
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    public IWindow GetWindow(string name)
    {
        return _windows.First(w => w.Name == name);
    }

    public sealed record State
    {
        public Dictionary<string, bool> AreOpen { get; init; } = [];
    }

    public State SaveState()
    {
        var dict = new Dictionary<string, bool>();
        for (var i = 0; i < _windows.Count; i++)
        {
            dict.Add(_windows[i].Name, _areOpen[i]);
        }

        return new State { AreOpen = dict };
    }

    public void RestoreState(State state)
    {
        for (var i = 0; i < _windows.Count; i++)
        {
            if (state.AreOpen.TryGetValue(_windows[i].Name, out var isOpen))
            {
                _areOpen[i] = isOpen;
            }
        }
    }
}

public interface IWindow
{
    string Name { get; }
    void Draw(ref bool isOpen);
}