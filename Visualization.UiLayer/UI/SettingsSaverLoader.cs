using System.Text.Json;

using Visualization.UiLayer.Applications;

namespace Visualization.UiLayer.UI;

public sealed class SettingsSaverLoader
{
    private const string SettingsFileName = "settings.json";

    public void Save(ApplicationState state)
    {
        var options = new JsonSerializerOptions { IncludeFields = true };
        var json = JsonSerializer.Serialize(state, options);
        File.WriteAllText(SettingsFileName, json);
    }

    public ApplicationState? Load()
    {
        if (!File.Exists(SettingsFileName))
        {
            return null;
        }

        var json = File.ReadAllText(SettingsFileName);
        var options = new JsonSerializerOptions { IncludeFields = true };
        return JsonSerializer.Deserialize<ApplicationState>(json, options);
    }
}