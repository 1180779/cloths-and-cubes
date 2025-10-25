using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos;

public class ConstantBoxesMaterialsDemo : BoxesMaterialsDemo
{
    private const int NFirstMaterials = 20 * NrOfRows;

    // private readonly Type materialsSource = typeof(MaterialConstant);
    protected override IMaterial[] GetMaterials()
    {
        return MaterialsHelper.GetConstMaterials(NFirstMaterials);
    }
}