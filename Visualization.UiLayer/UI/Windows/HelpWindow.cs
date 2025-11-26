using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public static class HelpWindow
{
    public const int ActionDescriptionWidth = 30;

    public static readonly Dictionary<string, Dictionary<string, string>> ActionKeyDictionary =
        new()
        {
            {
                "Camera",
                new()
                {
                    { "Enter camera mode", "Mouse click on the scene" },
                    { "Disable camera mode", "Esc" },
                    { "Next camera", "C" },
                    { "Move camera up", "Space" },
                    { "Move camera down", "Left Shift" },
                    { "Move camera forward", "W" },
                    { "Move camera backward", "S" },
                    { "Move Camera left", "A" },
                    { "Move camera right", "D" }
                }
            },
            {
                "Frame mode", new()
                {
                    { "Enable frame after frame mode", "Left Bracket ([)" },
                    { "Disable frame after frame mode", "Right Bracket (])" },
                    { "Add 1 frame (frame mode)", "1" },
                    { "Add 2 frames (frame mode)", "2" },
                    { "Add 3 frames (frame mode)", "3" },
                    { "Add 4 frames (frame mode)", "4" },
                    { "Add 5 frames (frame mode)", "5" },
                    { "Add 6 frames (frame mode)", "6" },
                    { "Add 7 frames (frame mode)", "7" },
                    { "Add 8 frames (frame mode)", "8" },
                    { "Add 9 frames (frame mode)", "9" },
                    { "Add frames (frame mode)", "0" },
                }
            },
#if DEBUG
            {
                " Objects in time",
                new()
                {
                    { "Go back", "Left" },
                    { "Go forward", "Right" },
                    {
                        "Using this feature while not being in (single) frame mode can lead to unexpected results. Use with caution. ",
                        ""
                    }
                }
            },
#endif
            { "Scene", new() { { "Reset demo", "R" }, } }
        };

    public static void Draw()
    {
        ImGui.Begin("Help");
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
        foreach (KeyValuePair<string, Dictionary<string, string>> pair in ActionKeyDictionary)
        {
            ImGui.Text(pair.Key);
            ImGui.Spacing();
            foreach (KeyValuePair<string, string> action in pair.Value)
            {
                if (action.Value.Length > 0)
                {
                    ImGui.Text($"{action.Key,ActionDescriptionWidth}: {action.Value}");
                }
                else
                {
                    // treat as an additional description instead
                    ImGui.Spacing();
                    ImGui.Text($"{action.Key}");
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
        }

        ImGui.PopTextWrapPos();
        ImGui.End();
    }
}