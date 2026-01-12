using Engine.Collision;

using ImGuiNET;

using Visualisation.Core.Display.Materials;

namespace Visualization.UiLayer.UI.Windows;

public sealed class BoxesDemoSettingsWindow(
    Func<int> getBoxesCount,
    Func<int> getSpheresCount,
    Func<int> getClothsCount,
    Func<int> getJointsCount,
    Func<EnvMapFileDescription> getCurrentEnvironmentMap,
    Action<EnvMapFileDescription> setCurrentEnvironmentMap,
    Func<EnvMapFileDescription> getDefaultEnvironmentMap,
    CollisionData collisionData
) : IWindow
{
    private CollisionData _collisionData = collisionData; /* borrowed */

    public Func<EnvMapFileDescription> GetCurrentEnvironmentMap { get; init; } = getCurrentEnvironmentMap;
    public Action<EnvMapFileDescription> SetCurrentEnvironmentMap { get; init; } = setCurrentEnvironmentMap;
    public Func<EnvMapFileDescription> GetDefaultEnvironmentMap { get; init; } = getDefaultEnvironmentMap;
    public Func<int> GetBoxesCount { get; init; } = getBoxesCount;
    public Func<int> GetSpheresCount { get; init; } = getSpheresCount;
    public Func<int> GetClothsCount { get; init; } = getClothsCount;
    public Func<int> JointsCount { get; init; } = getJointsCount;

    public delegate void SetObjectCount(int count);

    public delegate void SetClothCount(
        int count,
        int sizeX,
        int sizeY,
        Real springLength,
        Real springConstant,
        Real particleMass);

    public SetObjectCount? SetBoxesCount { get; init; }
    public SetObjectCount? SetSpheresCount { get; init; }
    public SetClothCount? SetClothsCount { get; init; }

    private int _sizeX = 21;
    public int SizeX => _sizeX;
    private int _sizeY = 21;
    public int SizeY => _sizeY;

    private Real _springLength = (Real)0.25;
    public Real SpringLength => _springLength;

    private Real _springConstant = (Real)1.0;
    public Real SpringConstant => _springConstant;

    private Real _particleMass = (Real)0.1;
    public Real ParticleMass => _particleMass;


    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Boxes Demo Settings", ref isOpen))
        {
            DrawClothGeneral();
            DrawCounts();
            DrawCollision();
            SelectEnvironmentMap();
        }

        ImGui.End();
    }

    private void SelectEnvironmentMap()
    {
        if (ImGui.CollapsingHeader("Environment Map"))
        {
            ImGui.Indent();
            const string resetToDefaultText = "Reset to Default";
            if (ImGui.Button(resetToDefaultText, UiControls.Style.ButtonSizes.Medium(resetToDefaultText)))
            {
                SetCurrentEnvironmentMap(GetDefaultEnvironmentMap());
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();


            var environmentMapFiles = MaterialsAndEnvironmentMapsHelper.AllEnvironmentMapFiles;
            foreach (var envMap in environmentMapFiles)
            {
                var fileName = envMap.FileName;
                if (ImGui.Button(fileName, UiControls.Style.ButtonSizes.SmallFillX(fileName)))
                {
                    SetCurrentEnvironmentMap(envMap);
                }
            }

            ImGui.Unindent();
        }
    }

    private void DrawClothGeneral()
    {
        ImGui.SeparatorText("New cloth parameters");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        ImGui.SliderFloat("Spring Length", ref _springLength, 0.005f, 1.0f);
        ImGui.DragFloat("Spring Constant", ref _springConstant, 0.005f, 0.0f, 10_000.0f);
        ImGui.DragFloat("Particle Mass", ref _particleMass, 0.005f, 0.01f, 10.0f);
    }

    private void DrawCounts()
    {
        ImGui.SeparatorText("Object counts");
        int boxesCount = GetBoxesCount();
        if (ImGui.DragInt("Boxes", ref boxesCount, 0.1f, 0, 200))
        {
            SetBoxesCount?.Invoke(boxesCount);
        }

        int spheresCount = GetSpheresCount();
        if (ImGui.DragInt("Spheres", ref spheresCount, 0.1f, 0, 200))
        {
            SetSpheresCount?.Invoke(spheresCount);
        }

        int clothsCount = GetClothsCount();
        if (ImGui.SliderInt("Cloths", ref clothsCount, 0, 5))
        {
            SetClothsCount?.Invoke(clothsCount, SizeX, SizeY, SpringLength, SpringConstant, ParticleMass);
        }

        int jointsCount = JointsCount();
        ImGui.Text($"Joints: {jointsCount}");
    }

    private void DrawCollision()
    {
        ImGui.SeparatorText("Collision parameters");
        ImGui.SliderFloat("Friction", ref _collisionData.Friction, 0.0f, 1.0f);
        ImGui.SliderFloat("Restitution", ref _collisionData.Restitution, 0.0f, 1.0f);
        ImGui.SliderFloat("Tolerance", ref _collisionData.Tolerance, 0.0f, 1.0f);
    }

    public sealed record CollisionState
    {
        public Real Friction { get; init; }
        public Real Restitution { get; init; }
        public Real Tolerance { get; init; }
    }

    public sealed record State
    {
        public CollisionState? CollisionState { get; init; }
        public EnvMapFileDescription? EnvironmentMapFileDescription { get; init; }
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public Real SpringLength { get; init; }
        public Real SpringConstant { get; init; }
        public Real ParticleMass { get; init; }
        public int BoxesCount { get; init; }
        public int SpheresCount { get; init; }
        public int ClothsCount { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            CollisionState =
                new CollisionState
                {
                    Friction = _collisionData.Friction,
                    Restitution = _collisionData.Restitution,
                    Tolerance = _collisionData.Tolerance
                },
            EnvironmentMapFileDescription = GetCurrentEnvironmentMap(),
            SizeX = SizeX,
            SizeY = SizeY,
            SpringLength = SpringLength,
            SpringConstant = SpringConstant,
            ParticleMass = ParticleMass,
            BoxesCount = GetBoxesCount(),
            SpheresCount = GetSpheresCount(),
            ClothsCount = GetClothsCount(),
        };
    }

    public void RestoreState(State state)
    {
        if (state.EnvironmentMapFileDescription is not null)
        {
            // Setting the loaded one to the renderers default one will not work as the renderer constructor has already run. 
            // Would need to refactor the renderer to call some Init() method to initialize the environment map, 
            // which would be skipped in the constructor in that case. This, however, creates more complexity. 
            SetCurrentEnvironmentMap(state.EnvironmentMapFileDescription);
        }

        if (state.CollisionState is not null)
        {
            _collisionData.Friction = state.CollisionState.Friction;
            _collisionData.Restitution = state.CollisionState.Restitution;
            _collisionData.Tolerance = state.CollisionState.Tolerance;
        }

        _sizeX = state.SizeX;
        _sizeY = state.SizeY;

        _springLength = state.SpringLength;
        _springConstant = state.SpringConstant;
        _particleMass = state.ParticleMass;

        SetBoxesCount?.Invoke(state.BoxesCount);
        SetSpheresCount?.Invoke(state.SpheresCount);
        SetClothsCount?.Invoke(state.ClothsCount, SizeX, SizeY, SpringLength, SpringConstant, ParticleMass);
    }

    public string Name => "Boxes Demo Settings";
}