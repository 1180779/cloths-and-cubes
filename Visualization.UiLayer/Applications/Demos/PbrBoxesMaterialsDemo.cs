using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos;

public class PbrBoxesMaterialsDemo : SpheresMaterialsDemo
{
    private const int NFirstMaterials = 20 * NrOfRows;

    // private readonly Type materialsSource = typeof(MaterialTextured.Metals);
    protected override IMaterial[] GetMaterials()
    {
        return MaterialsHelper.GetTexturedMaterials(NFirstMaterials);
    }
}