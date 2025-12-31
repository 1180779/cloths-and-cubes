using ImGuiNET;

using Visualisation.Core.GameObjects.Scenes;

using Visualization.UiLayer.Applications.Demos;

namespace Visualization.UiLayer.UI.Windows;

public sealed class SceneManagementWindow : IWindow
{
    private readonly BoxesDemo _application;
    private readonly string _scenesDirectory;

    private string _newSceneName = "New Scene";
    private string _sceneDescription = "";
    private bool _includeParticleStates;
    private string _statusMessage = "";
    private float _statusMessageTimer;
    private bool _isError;

    private List<string> _savedScenes = new();
    private string? _selectedScene;
    private SceneData? _previewData;

    public SceneManagementWindow(BoxesDemo application, string scenesDirectory = "scenes")
    {
        _application = application;
        _scenesDirectory = scenesDirectory;

        if (!Directory.Exists(_scenesDirectory))
        {
            Directory.CreateDirectory(_scenesDirectory);
        }

        RefreshSceneList();
    }

    public string Name => "Scene Management";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            // Status Message
            if (_statusMessageTimer > 0f)
            {
                _statusMessageTimer -= ImGui.GetIO().DeltaTime;
                if (_statusMessageTimer <= 0f)
                {
                    _statusMessage = "";
                }
            }

            var color = _isError
                ? new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f)
                : new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            ImGui.TextColored(color, _statusMessage);
            ImGui.Separator();

            // Save Scene Section
            if (ImGui.CollapsingHeader("Save Current Scene", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.InputText("Scene Name", ref _newSceneName, 256);
                ImGui.InputTextMultiline("Description", ref _sceneDescription, 1024,
                    new System.Numerics.Vector2(-1, 60));

                ImGui.Checkbox("Include Particle States", ref _includeParticleStates);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Saves exact positions/velocities of all cloth particles.\n" +
                        "Unchecked: Only saves cloth parameters.\n" +
                        "Checked: Saves full particle state.");
                }

                ImGui.Spacing();

                if (ImGui.Button("Save Scene", new System.Numerics.Vector2(120, 30)))
                {
                    SaveCurrentScene();
                }

                ImGui.SameLine();
                ImGui.TextDisabled($"Location: {_scenesDirectory}/");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Load#Scene#Collapsing#Header", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Refresh List"))
                {
                    RefreshSceneList();
                }

                ImGui.Spacing();

                if (_savedScenes.Count == 0)
                {
                    ImGui.TextDisabled("No saved scenes found.");
                }
                else
                {
                    ImGui.Text($"Found {_savedScenes.Count} scene(s):");
                    ImGui.Separator();

                    // Scene list
                    ImGui.BeginChild("SceneList", new System.Numerics.Vector2(-1, 200), ImGuiChildFlags.None);

                    foreach (var scenePath in _savedScenes)
                    {
                        var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                        bool isSelected = _selectedScene == scenePath;

                        if (ImGui.Selectable(sceneName, isSelected))
                        {
                            _selectedScene = scenePath;
                            LoadScenePreview(scenePath);
                        }

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            LoadScene(scenePath);
                        }
                    }

                    ImGui.EndChild();

                    ImGui.TextDisabled("Double-click to load, or select and use buttons below");

                    // Scene preview
                    if (_previewData != null)
                    {
                        ImGui.Spacing();
                        ImGui.SeparatorText("Scene Preview");

                        ImGui.Text($"Name: {_previewData.Metadata.Name}");
                        if (!string.IsNullOrEmpty(_previewData.Metadata.Description))
                        {
                            ImGui.TextWrapped($"Description: {_previewData.Metadata.Description}");
                        }

                        ImGui.Text($"Created: {_previewData.Metadata.CreatedDate.ToLocalTime()}");

                        ImGui.Spacing();
                        ImGui.Text("Objects:");
                        ImGui.BulletText($"Boxes: {_previewData.Boxes.Count}");
                        ImGui.BulletText($"Balls: {_previewData.Balls.Count}");
                        ImGui.BulletText($"Cloths: {_previewData.Cloths.Count}");
                    }

                    ImGui.Spacing();

                    // Action buttons
                    if (_selectedScene != null)
                    {
                        if (ImGui.Button("Load Scene", new System.Numerics.Vector2(120, 30)))
                        {
                            LoadScene(_selectedScene);
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Delete Scene", new System.Numerics.Vector2(120, 30)))
                        {
                            ImGui.OpenPopup("DeleteConfirmation");
                        }
                    }
                }

                bool deletePopupOpen = true;
                if (ImGui.BeginPopupModal("DeleteConfirmation", ref deletePopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (_selectedScene != null)
                    {
                        ImGui.Text($"Are you sure you want to delete:");
                        ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1),
                            Path.GetFileNameWithoutExtension(_selectedScene));
                        ImGui.Text("This action cannot be undone!");

                        ImGui.Spacing();

                        if (ImGui.Button("Yes, Delete", new System.Numerics.Vector2(120, 0)))
                        {
                            DeleteScene(_selectedScene);
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Cancel", new System.Numerics.Vector2(120, 0)))
                        {
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Information"))
            {
                ImGui.TextWrapped("Scene files are saved in JSON format and include:");
                ImGui.BulletText("All object positions, orientations, and velocities");
                ImGui.BulletText("Physics parameters (mass, friction, etc.)");
                ImGui.BulletText("Material assignments");
                ImGui.BulletText("Cloth grid dimensions and spring parameters");

                ImGui.Spacing();
                ImGui.TextWrapped("Notes:");
                ImGui.BulletText("Loading a scene will clear current objects");
                ImGui.BulletText("Scene files can be edited manually (JSON format)");
            }
        }

        ImGui.End();
    }

    private void SaveCurrentScene()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_newSceneName))
            {
                ShowStatus("Scene name cannot be empty!", true);
                return;
            }

            // Sanitize filename
            var sanitizedName = string.Join("_", _newSceneName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{sanitizedName}.json";
            var filePath = Path.Combine(_scenesDirectory, fileName);

            var sceneData = SceneSerializer.SerializeScene(
                _application.SceneManager.GameObjects,
                _application.CollisionData,
                _newSceneName,
                _sceneDescription,
                _includeParticleStates);
            SceneSerializer.SaveToFile(sceneData, filePath);

            ShowStatus($"Scene saved: {fileName}", false);
            RefreshSceneList();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error saving scene: {ex.Message}", true);
        }
    }

    private void LoadScene(string filePath)
    {
        try
        {
            var sceneData = SceneDeserializer.LoadFromFile(filePath);
            if (sceneData == null)
            {
                ShowStatus("Failed to load scene file!", true);
                return;
            }

            _application.ApplySceneData(sceneData);
            _application.StoreInitialSceneState(sceneData);

            ShowStatus($"Scene loaded: {Path.GetFileNameWithoutExtension(filePath)}", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error loading scene: {ex.Message}", true);
        }
    }

    private void DeleteScene(string filePath)
    {
        try
        {
            File.Delete(filePath);
            ShowStatus($"Scene deleted: {Path.GetFileNameWithoutExtension(filePath)}", false);

            _selectedScene = null;
            _previewData = null;
            RefreshSceneList();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error deleting scene: {ex.Message}", true);
        }
    }

    private void LoadScenePreview(string filePath)
    {
        try
        {
            _previewData = SceneDeserializer.LoadFromFile(filePath);
        }
        catch
        {
            _previewData = null;
        }
    }

    private void RefreshSceneList()
    {
        try
        {
            _savedScenes = Directory.GetFiles(_scenesDirectory, "*.json").ToList();
            _savedScenes.Sort();
        }
        catch
        {
            _savedScenes = new List<string>();
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        _statusMessage = message;
        _isError = isError;
        _statusMessageTimer = 5f; // Show for 5 seconds
    }

    public sealed record State
    {
        public string? NewSceneName { get; init; }
        public string? SceneDescription { get; init; }
        public bool IncludeParticleStates { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            NewSceneName = _newSceneName,
            SceneDescription = _sceneDescription,
            IncludeParticleStates = _includeParticleStates
        };
    }

    public void RestoreState(State state)
    {
        _newSceneName = state.NewSceneName ?? "New Scene";
        _sceneDescription = state.SceneDescription ?? "";
        _includeParticleStates = state.IncludeParticleStates;
    }
}