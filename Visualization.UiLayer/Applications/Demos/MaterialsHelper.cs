using System.Reflection;

using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.Applications.Demos;

public static class MaterialsHelper
{
    private static readonly Type ConstMaterialsSource = typeof(MaterialConstant);

    public static IMaterial[] GetConstMaterials(int n)
    {
        var topLevelMaterials = ConstMaterialsSource
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(MaterialConstant))
            .Select(p => (MaterialConstant)p.GetValue(null)!)
            .ToArray();
        var nestedMaterials = ConstMaterialsSource
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(p => p.PropertyType == typeof(MaterialConstant))
            .Select(p => (MaterialConstant)p.GetValue(null)!)
            .ToArray();

        IMaterial[] materials = [..topLevelMaterials, ..nestedMaterials];
        materials = materials.Take(n).ToArray();
        return materials;
    }

    private static readonly Type TexturedMaterialsSource = typeof(MaterialTextured.Metals);

    public static IMaterial[] GetTexturedMaterials(int n)
    {
        var topLevelMaterials = TexturedMaterialsSource
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(MaterialTextured))
            .Select(p => (MaterialTextured)p.GetValue(null)!)
            .ToArray();
        var nestedMaterials = TexturedMaterialsSource
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(p => p.PropertyType == typeof(MaterialTextured))
            .Select(p => (MaterialTextured)p.GetValue(null)!)
            .ToArray();

        IMaterial[] materials = [..topLevelMaterials, ..nestedMaterials];
        materials = materials.Take(n).ToArray();
        return materials;
    }
}