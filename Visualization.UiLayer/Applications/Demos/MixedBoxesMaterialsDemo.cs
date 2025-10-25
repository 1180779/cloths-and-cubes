using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos;

public class MixedBoxesMaterialsDemo : SpheresMaterialsDemo
{
    private const int NFirstMaterials = 1 * NrOfRows;

    protected override IMaterial[] GetMaterials()
    {
        return
        [
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
            ..MaterialsHelper.GetTexturedMaterials(NFirstMaterials),
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
            ..MaterialsHelper.GetTexturedMaterials(NFirstMaterials),
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
            ..MaterialsHelper.GetTexturedMaterials(NFirstMaterials),
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
            ..MaterialsHelper.GetTexturedMaterials(NFirstMaterials),
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
            ..MaterialsHelper.GetTexturedMaterials(NFirstMaterials),
            ..MaterialsHelper.GetConstMaterials(NFirstMaterials),
        ];
    }
}