using System.Text.Json;
using System.Text.Json.Serialization;

using Engine.Collision;

namespace Visualisation.Core.GameObjects.Scenes;

/// <summary>
/// Serializes SceneManager state to SceneData for saving to disk
/// </summary>
public class SceneSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serialize a scene to SceneData
    /// </summary>
    public static SceneData SerializeScene(
        IEnumerable<GameObject> gameObjects,
        CollisionData collisionData,
        string sceneName = "Untitled Scene",
        string description = "",
        bool includeParticleStates = false)
    {
        return gameObjects.ToSceneData(collisionData, sceneName, description, includeParticleStates);
    }

    /// <summary>
    /// Serialize scene to JSON string
    /// </summary>
    public static string SerializeToJson(SceneData sceneData)
    {
        return JsonSerializer.Serialize(sceneData, JsonOptions);
    }

    /// <summary>
    /// Save scene to file
    /// </summary>
    public static void SaveToFile(SceneData sceneData, string filePath)
    {
        var json = SerializeToJson(sceneData);
        File.WriteAllText(filePath, json);
    }
}