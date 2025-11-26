using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class PbrMaterialsDemo : BoxesMaterialsDemo
{
    protected override IMaterial[] GetMaterials()
    {
        return MaterialsHelper.GetTexturedMaterials(50 * _nrOfRows);
    }
}