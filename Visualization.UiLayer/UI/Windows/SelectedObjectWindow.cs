using ImGuiNET;

using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.UI.Windows;

public sealed class SelectedObjectWindow(SelectionManager selectionManager)
{
    private SelectionManager _selectionManager = selectionManager; /* borrowed */

    public void Draw()
    {
        ImGui.Begin("Selected Object");
        ImGui.End();
    }

    private void DrawBox()
    {
        
    }

    private void DrawSphere()
    {
        
    }

    private void DrawParticle()
    {
        
    }
}