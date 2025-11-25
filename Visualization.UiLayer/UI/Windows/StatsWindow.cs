using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows
{
	public static class StatsWindow
	{
		public static void Draw()
		{
			ImGui.Begin("Stats");
			ImGui.Text($"Application FPS: {ImGui.GetIO().Framerate:0.0}");
			ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:0.0} ms");
			ImGui.End();
		}
	}
}