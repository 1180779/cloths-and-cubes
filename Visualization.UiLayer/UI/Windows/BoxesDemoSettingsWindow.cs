using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class BoxesDemoSettingsWindow(
    Func<int> getBoxesCount,
    Func<int> getSpheresCount,
    Func<int> getClothsCount
) : IWindow
{
    public Func<int> GetBoxesCount { get; set; } = getBoxesCount;
    public Func<int> GetSpheresCount { get; set; } = getSpheresCount;
    public Func<int> GetClothsCount { get; set; } = getClothsCount;

    public delegate void SetObjectCount(int count);

    public SetObjectCount? SetBoxesCount { get; set; }
    public SetObjectCount? SetSpheresCount { get; set; }
    public SetObjectCount? SetClothsCount { get; set; }

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

    private bool _lsctpm; // linear spring constant to particle mass
    private Real _lsctpmScale = (Real)1.0;
    private Real _lsctpmBias = (Real)0.0;

    private bool _lsctsl; // linear spring constant to spring length
    private Real _lsctslScale = (Real)1.0;
    private Real _lsctslBias = (Real)0.0;

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Boxes Demo Settings", ref isOpen))
        {
            DrawClothGeneral();
            DrawClothLsctpm();
            DrawClothLsctsl();

            DrawCounts();
        }

        ImGui.End();
    }

    private void DrawClothGeneral()
    {
        ImGui.SeparatorText("Cloth parameters");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        if (ImGui.SliderFloat("Spring Length", ref _springLength, 0.005f, 1.0f))
        {
            if (_lsctsl)
            {
                _springConstant = _lsctslScale * _springLength + _lsctslBias;
            }
        }

        if (ImGui.DragFloat("Spring Constant", ref _springConstant, 0.005f, 0.0f, 100.0f))
        {
            if (_lsctpm)
            {
                _particleMass = (Real)Math.Max((_springConstant - _lsctpmBias), 0.0) / _lsctpmScale;
            }
            else if (_lsctsl)
            {
                _springLength = (_springConstant - _lsctslBias) / _lsctslScale;
            }
        }

        if (ImGui.DragFloat("Particle Mass", ref _particleMass, 0.005f, 0.01f, 10.0f) && _lsctpm)
        {
            _springConstant = _particleMass * _lsctpmScale + _lsctpmBias;
        }
    }

    private void DrawClothLsctpm()
    {
        if (_lsctsl)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox("Linear Spring Constant to Particle Mass", ref _lsctpm) && _lsctpm)
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        if (_lsctsl)
        {
            ImGui.EndDisabled();
        }

        if (!_lsctpm || _lsctsl)
        {
            ImGui.BeginDisabled();
        }

        ImGui.PushID("Scale (particle mass)");
        if (ImGui.DragFloat("Scale", ref _lsctpmScale, 0.5f, 0.0f, 100.0f))
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.PopID();


        ImGui.PushID("Bias (particle mass)");
        if (ImGui.DragFloat("Bias", ref _lsctpmBias, 0.5f, 0.0f, 100.0f))
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.PopID();

        ImGui.PushID("Reset Scale (particle mass)");
        if (ImGui.Button("Reset Scale"))
        {
            _lsctpmScale = (Real)1.0;
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.PopID();

        ImGui.SameLine();
        ImGui.PushID("Reset Bias (particle mass)");
        if (ImGui.Button("Reset Bias"))
        {
            _lsctpmBias = (Real)0.0;
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.PopID();

        if (!_lsctpm || _lsctsl)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawClothLsctsl()
    {
        if (_lsctpm)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox("Linear Spring Constant to Spring Length", ref _lsctsl) && _lsctsl)
        {
            _springConstant = _lsctslScale * _springLength + _lsctpmBias;
        }

        if (_lsctpm)
        {
            ImGui.EndDisabled();
        }

        if (!_lsctsl || _lsctpm)
        {
            ImGui.BeginDisabled();
        }

        ImGui.PushID("Scale (spring length)");
        if (ImGui.DragFloat("Scale", ref _lsctslScale, 0.5f, 0.0f, 100.0f))
        {
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.PopID();


        ImGui.PushID("Bias (spring length)");
        if (ImGui.DragFloat("Bias", ref _lsctslBias, 0.5f, 0.0f, 100.0f))
        {
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.PopID();

        ImGui.PushID("Reset Scale (spring length)");
        if (ImGui.Button("Reset Scale"))
        {
            _lsctslScale = (Real)1.0;
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.PopID();

        ImGui.SameLine();
        ImGui.PushID("Reset Bias (spring length)");
        if (ImGui.Button("Reset Bias"))
        {
            _lsctslBias = (Real)0.0;
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.PopID();

        if (!_lsctsl)
        {
            ImGui.EndDisabled();
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
        if (ImGui.SliderInt("Cloths", ref clothsCount, 0, 5))
        {
            SetClothsCount?.Invoke(clothsCount);
        }
    }

    public sealed record State
    {
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public Real SpringLength { get; init; }
        public Real SpringConstant { get; init; }
        public Real ParticleMass { get; init; }
        public bool Lsctpm { get; init; }
        public Real LsctpmScale { get; init; }
        public Real LsctpmBias { get; init; }
        public bool Lsctsl { get; init; }
        public Real LsctslScale { get; init; }
        public Real LsctslBias { get; init; }
        public int BoxesCount { get; init; }
        public int SpheresCount { get; init; }
        public int ClothsCount { get; init; }
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
            Lsctpm = _lsctpm,
            LsctpmScale = _lsctpmScale,
            LsctpmBias = _lsctpmBias,
            Lsctsl = _lsctsl,
            LsctslScale = _lsctslScale,
            LsctslBias = _lsctslBias,
            BoxesCount = GetBoxesCount(),
            SpheresCount = GetSpheresCount(),
            ClothsCount = GetClothsCount(),
        };
    }

    public void RestoreState(State state)
    {
        _sizeX = state.SizeX;
        _sizeY = state.SizeY;

        _springLength = state.SpringLength;
        _springConstant = state.SpringConstant;
        _particleMass = state.ParticleMass;

        _lsctpm = state.Lsctpm;
        _lsctpmScale = state.LsctpmScale;
        _lsctpmBias = state.LsctpmBias;

        _lsctsl = state.Lsctsl;
        _lsctslScale = state.LsctslScale;
        _lsctslBias = state.LsctslBias;

        SetBoxesCount?.Invoke(state.BoxesCount);
        SetSpheresCount?.Invoke(state.SpheresCount);
        SetClothsCount?.Invoke(state.ClothsCount);
    }

    public string Name => "Boxes Demo Settings";
}