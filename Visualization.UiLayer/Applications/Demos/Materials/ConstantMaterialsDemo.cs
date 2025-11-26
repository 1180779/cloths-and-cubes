using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class ConstantMaterialsDemo : BoxesMaterialsDemo
{
    protected override IMaterial[] GetMaterials()
    {
        int nFirstMaterials = 20 * _nrOfRows;
        return MaterialsHelper.GetConstMaterials(nFirstMaterials);
    }
}