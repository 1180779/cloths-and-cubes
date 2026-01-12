using System.Text.Json;
using System.Text.Json.Serialization;

using Engine.Collision;
using Engine.ContactGenerators;

namespace Visualisation.Core.GameObjects.Scenes;

/// <summary>
/// Serializes scene state to SceneData
/// </summary>
public static class SceneSerializer
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
        GlobalJointsList globalJoints,
        string sceneName = "Untitled Scene",
        string description = "")
    {
        return gameObjects.ToSceneData(collisionData, globalJoints, sceneName, description);
    }

    /// <summary>
    /// Serialize scene to JSON string
    /// </summary>
    public static string SerializeToJson(SceneData sceneData)
    {
        return JsonSerializer.Serialize(sceneData, JsonOptions);
    }

    /// <summary>
    /// Save the scene to file
    /// </summary>
    public static void SaveToFile(SceneData sceneData, string filePath)
    {
        var json = SerializeToJson(sceneData);
        File.WriteAllText(filePath, json);
    }
}