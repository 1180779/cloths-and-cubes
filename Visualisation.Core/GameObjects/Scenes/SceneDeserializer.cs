using System.Text.Json;

namespace Visualisation.Core.GameObjects.Scenes;

/// <summary>
/// Deserializes SceneData back into GameObjects
/// </summary>
public static class SceneDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserialize a scene from a JSON string
    /// </summary>
    public static SceneData? DeserializeFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SceneData>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to deserialize scene: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a scene from a file
    /// </summary>
    public static SceneData? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Scene file not found: {filePath}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return DeserializeFromJson(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load scene file: {ex.Message}");
            return null;
        }
    }
}