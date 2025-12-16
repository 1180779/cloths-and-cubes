using System.Reflection;

namespace Visualisation.Core.Display.Materials;

public static class MaterialsHelper
{
    static MaterialsHelper()
    {
        AllConstMaterials = GetConstMaterials();
        AllTexturedMaterials = GetTexturedMaterials();
    }
    
    private static readonly Type ConstMaterialsSource = typeof(MaterialConstant);

    public static IMaterial[] AllConstMaterials;
    public static IMaterial[] AllTexturedMaterials;
    
    public static IMaterial[] GetConstMaterials(int? n = null)
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
        if (n is not null)
        {
            materials = materials.Take(n.Value).ToArray();
        }
        return materials;
    }

    private static readonly Type TexturedMaterialsSource = typeof(MaterialTextured.Metals);

    public static IMaterial[] GetTexturedMaterials(int? n = null)
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
        if (n is not null)
        {
            materials = materials.Take(n.Value).ToArray();
        }
        return materials;
    }
}