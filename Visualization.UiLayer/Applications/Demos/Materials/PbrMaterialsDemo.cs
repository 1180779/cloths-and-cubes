using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class PbrMaterialsDemo : SpheresMaterialsDemo
{
    // private readonly Type materialsSource = typeof(MaterialTextured.Metals);
    protected override IMaterial[] GetMaterials()
    {
        return MaterialsHelper.GetTexturedMaterials(20 * NrOfRows);
    }
}