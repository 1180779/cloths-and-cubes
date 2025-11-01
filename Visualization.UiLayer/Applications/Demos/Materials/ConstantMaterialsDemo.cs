using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class ConstantMaterialsDemo : BoxesMaterialsDemo
{
    // private readonly Type materialsSource = typeof(MaterialConstant);
    protected override IMaterial[] GetMaterials()
    {
        int nFirstMaterials = 20 * NrOfRows;
        return MaterialsHelper.GetConstMaterials(nFirstMaterials);
    }
}