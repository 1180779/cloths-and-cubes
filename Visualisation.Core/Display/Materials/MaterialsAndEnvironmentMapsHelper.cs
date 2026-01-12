using System.Reflection;

namespace Visualisation.Core.Display.Materials;

public sealed record EnvMapFileDescription(string FilePath)
{
    public string FilePath { get; init; } = FilePath;
    public string FileName => Path.GetFileName(FilePath);
}

public static class MaterialsAndEnvironmentMapsHelper
{
    static MaterialsAndEnvironmentMapsHelper()
    {
        ScanTexturedMaterials();
        AllConstMaterials = GetConstMaterials();
        ScanEnvironmentMaps();
    }

    public const string AssetRootFolder = "resources";
    public static string MaterialsFolder = Path.Join(AssetRootFolder, "materials");
    public static string EnvironmentMapsFolder = Path.Join(AssetRootFolder, "environments");

    private static readonly Type ConstMaterialsSource = typeof(MaterialConstant);

    public static EnvMapFileDescription[] AllEnvironmentMapFiles = [];
    public static IMaterial[] AllConstMaterials;
    public static IMaterial[] AllTexturedMaterials = [];

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

    public static void ScanEnvironmentMaps()
    {
        var materialsDirectoryInfo = new DirectoryInfo(EnvironmentMapsFolder);
        if (!materialsDirectoryInfo.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Environment maps directory not found at '{materialsDirectoryInfo.FullName}'");
            Console.ResetColor();
            return;
        }

        var envMapFiles = materialsDirectoryInfo.GetFiles()
            .Where(f => f.Name.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith(".exr", StringComparison.OrdinalIgnoreCase))
            .Select(f => new EnvMapFileDescription($"{AssetRootFolder}/environments/{f.Name}"))
            .ToArray();
        AllEnvironmentMapFiles = envMapFiles;
    }

    public static void ScanTexturedMaterials()
    {
        var materialsDirectoryInfo = new DirectoryInfo(MaterialsFolder);
        if (!materialsDirectoryInfo.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Materials directory not found at '{materialsDirectoryInfo.FullName}'");
            Console.ResetColor();
            return;
        }

        var materials = ProcessMaterials(materialsDirectoryInfo, AssetRootFolder);
        AllTexturedMaterials = materials;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Successfully loaded {materials.Length} materials'");
        Console.ResetColor();
    }

    /// <summary>
    /// Defines the filename suffixes to identify PBR material maps.
    /// The program will look for files ending with these strings (case-insensitive).
    /// </summary>
    private static readonly Dictionary<string, string[]> MaterialMapIdentifiers = new()
    {
        { "albedo", ["diff"] }, { "normal", ["nor_gl"] }, { "arm", ["arm"] }, // combined metallic, roughness, ao map
    };

    /// <summary>
    /// Recursively processes directories, generating material properties and nested static classes.
    /// </summary>
    private static IMaterial[] ProcessMaterials(
        DirectoryInfo directoryInfo,
        string rootPath)
    {
        var materials = new List<IMaterial>();
        var materialDirectories = directoryInfo.GetDirectories();

        var materialDirs = new List<DirectoryInfo>();
        foreach (var directory in materialDirectories)
        {
            if (IsMaterialDirectory(directory))
            {
                materialDirs.Add(directory);
            }
        }

        foreach (var materialDir in materialDirs)
        {
            var materialMaps = GetMaterialPaths(materialDir, rootPath);

            materials.Add(new MaterialTextured
            {
                AlbedoMap = materialMaps["albedo"],
                NormalMap = materialMaps["normal"],
                ArmMap = materialMaps["arm"],
                Name = materialDir.Name,
            });
        }

        return materials.ToArray();
    }

    /// <summary>
    /// Checks if a directory contains a full set of PBR textures.
    /// </summary>
    private static bool IsMaterialDirectory(DirectoryInfo directory)
    {
        var files = directory.GetFiles();
        foreach (var mapType in MaterialMapIdentifiers)
        {
            bool found = mapType.Value.Any(identifier =>
                files.Any(f => FileContainsIdentifier(f, identifier))
            );
            if (!found) return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a file contains the specified identifier in its name (excluding extension).
    /// </summary>
    /// <param name="file">The file to check</param>
    /// <param name="identifier">The identifier to look for in the filename</param>
    /// <returns>True if the file contains the identifier, false otherwise</returns>
    private static bool FileContainsIdentifier(FileInfo file, string identifier)
    {
        return !Path.GetFileName(file.Name).EndsWith(".meta") &&
            Path.GetFileNameWithoutExtension(file.Name).Contains(identifier, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the paths for a known material directory.
    /// </summary>
    private static Dictionary<string, string> GetMaterialPaths(DirectoryInfo directory, string rootPath)
    {
        var files = directory.GetFiles();
        var foundMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapType in MaterialMapIdentifiers)
        {
            FileInfo? foundFile = null;
            foreach (var identifier in mapType.Value)
            {
                foundFile = files.FirstOrDefault(f => FileContainsIdentifier(f, identifier));
                if (foundFile != null) break;
            }

            var relativePath = Path.GetRelativePath(rootPath, foundFile!.FullName); // .Replace('\\', '/');
            foundMaps[mapType.Key.ToLowerInvariant()] = $"{AssetRootFolder}/{relativePath}";
        }

        return foundMaps;
    }
}