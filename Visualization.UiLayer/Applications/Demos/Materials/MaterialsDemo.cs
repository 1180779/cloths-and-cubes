using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public class MaterialsDemo : BoxesMaterialsDemo
{
    protected override IMaterial[] GetMaterials()
    {
        _nrOfRows = 10;
        var nrOfColumns = _nrOfRows;

        var materials = new List<IMaterial>(_nrOfRows * nrOfColumns);
        var albedo = new Vector3(1.0f, 0.71f, 0.29f);

        for (var row = 0; row < _nrOfRows; row++)
        {
            var roughness = float.Min((float)row / (_nrOfRows - 1), float.Floor(1.0f));

            for (var col = 0; col < nrOfColumns; col++)
            {
                var metallic = float.Min((float)col / (nrOfColumns - 1), float.Floor(1.0f));

                materials.Add(
                    new MaterialConstant
                    {
                        Albedo = albedo, Ao = 1.0f, Metallic = metallic, Roughness = roughness,
                    }
                );
            }
        }

        return materials.ToArray();
    }
}