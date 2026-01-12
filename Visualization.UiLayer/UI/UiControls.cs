using ImGuiNET;

namespace Visualization.UiLayer.UI;

public static class UiControls
{
    public static void SetTooltip(string text)
    {
        if (ImGui.IsItemHovered(Style.Tooltip.HoveredFlags))
        {
            ImGui.SetTooltip(text);
        }
    }

    public static bool DragFloatPropertyPositive(
        Func<float> get,
        Action<float> set,
        String label,
        float step = 0.1f,
        float minValue = 0.0f,
        float maxValue = float.PositiveInfinity)
    {
        var value = get();
        if (ImGui.DragFloat(label, ref value, step, minValue, maxValue))
        {
            set(value);
            return true;
        }

        return false;
    }

    public static class Style
    {
        public static class Tooltip
        {
            public static ImGuiHoveredFlags HoveredFlags =>
                ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.Stationary;
        }

        public static class ButtonSizes
        {
            public static System.Numerics.Vector2 Small(string text) =>
                CalculateButtonSize(text, 10f, 6f);

            public static System.Numerics.Vector2 SmallFillX(string text) =>
                new(-1f, CalculateButtonSize(text, 10f, 6f).Y);

            public static System.Numerics.Vector2 Medium(string text) =>
                CalculateButtonSize(text, 20f, 10f);

            public static System.Numerics.Vector2 MediumFillX(string text) =>
                new(-1f, CalculateButtonSize(text, 20f, 10f).Y);

            public static System.Numerics.Vector2 Large(string text) =>
                CalculateButtonSize(text, 40f, 20f);

            private static System.Numerics.Vector2 CalculateButtonSize(string text, float paddingX, float paddingY)
            {
                // TODO: Cache sizes if performance becomes an issue
                var textSize = ImGui.CalcTextSize(text);
                return new System.Numerics.Vector2(textSize.X + paddingX, textSize.Y + paddingY);
            }
        }
    }
}