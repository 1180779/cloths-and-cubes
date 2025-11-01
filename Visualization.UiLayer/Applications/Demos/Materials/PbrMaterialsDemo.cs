using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class PbrMaterialsDemo : BoxesMaterialsDemo
{
    // private readonly Type materialsSource = typeof(MaterialTextured.Metals);
    protected override IMaterial[] GetMaterials()
    {
        return MaterialsHelper.GetTexturedMaterials(50 * NrOfRows);
    }
}