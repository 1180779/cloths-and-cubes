using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class BoxesDemoSettingsWindow(
    Func<int> getBoxesCount,
    Func<int> getSpheresCount,
    Func<int> getClothsCount,
    Func<int> getJointsCount
) : IWindow
{
    public Func<int> GetBoxesCount { get; set; } = getBoxesCount;
    public Func<int> GetSpheresCount { get; set; } = getSpheresCount;
    public Func<int> GetClothsCount { get; set; } = getClothsCount;
    public Func<int> JointsCount { get; set; } = getJointsCount;
    public Func<List<ClothParams>>? GetClothsData { get; set; }

    public delegate void SetObjectCount(int count);

    public SetObjectCount? SetBoxesCount { get; set; }
    public SetObjectCount? SetSpheresCount { get; set; }
    public Action<int, List<ClothParams>>? SetClothsCount { get; set; }

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
        }

        ImGui.End();
    }

    private void DrawClothGeneral()
    {
        ImGui.SeparatorText("Cloth parameters");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        ImGui.SliderFloat("Spring Length", ref _springLength, 0.005f, 1.0f);
        ImGui.DragFloat("Spring Constant", ref _springConstant, 0.005f, 0.0f, 10_000.0f);
        ImGui.DragFloat("Particle Mass", ref _particleMass, 0.005f, 0.01f, 10.0f);

        if(ImGui.Button("Add Cloth"))
        {
            SetClothsCount?.Invoke(GetClothsCount() + 1, GetClothsData!());
        } 
        if(ImGui.Button("Clear Cloths"))
        {
            SetClothsCount?.Invoke(0, []);
        }
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
        ImGui.Text($"Cloths: {clothsCount}");

        int jointsCount = JointsCount();
        ImGui.Text($"Joints: {jointsCount}");
    }

    public sealed record ClothParams
    {
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public Real SpringLength { get; init; }
        public Real SpringConstant { get; init; }
        public Real ParticleMass { get; init; }
    }

    public sealed record State
    {
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public Real SpringLength { get; init; }
        public Real SpringConstant { get; init; }
        public Real ParticleMass { get; init; }
        public int BoxesCount { get; init; }
        public int SpheresCount { get; init; }
        public int ClothsCount { get; init; }
        public List<ClothParams> ClothsData { get; init; } = [];
    }

    public State SaveState()
    {
        return new State
        {
            SizeX = SizeX,
            SizeY = SizeY,
            SpringLength = SpringLength,
            SpringConstant = SpringConstant,
            ParticleMass = ParticleMass,
            BoxesCount = GetBoxesCount(),
            SpheresCount = GetSpheresCount(),
            ClothsCount = GetClothsCount(),
            ClothsData = GetClothsData!=null ? GetClothsData() : [],
        };
    }

    public void RestoreState(State state)
    {
        _sizeX = state.SizeX;
        _sizeY = state.SizeY;

        _springLength = state.SpringLength;
        _springConstant = state.SpringConstant;
        _particleMass = state.ParticleMass;


        SetBoxesCount?.Invoke(state.BoxesCount);
        SetSpheresCount?.Invoke(state.SpheresCount);
        SetClothsCount?.Invoke(state.ClothsCount, state.ClothsData);
    }

    public string Name => "Boxes Demo Settings";
}