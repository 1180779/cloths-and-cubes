using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class BoxesDemoSettingsWindow(int boxesCount, int spheresCount)
{
    public delegate void SetObjectCount(int count); // set boxes or spheres count

    private int _boxesCount = boxesCount;
    public SetObjectCount? SetBoxesCount { get; set; }
    private int _spheresCount = spheresCount;
    public SetObjectCount? SetSpheresCount { get; set; }

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

    public void Draw()
    {
        ImGui.Begin("Boxes Demo Settings");
        DrawClothGeneral();
        DrawClothLsctpm();
        DrawClothLsctsl();

        DrawCounts();
        ImGui.End();
    }

    private void DrawClothGeneral()
    {
        ImGui.SeparatorText("Cloth parameters");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        if (ImGui.SliderFloat("Spring Length", ref _springLength, 0.01f, 1.0f))
        {
            if (_lsctsl)
            {
                _springConstant = _lsctslScale * _springLength + _lsctslBias;
            }
        }

        if (ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.0f, 10.0f))
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

        if (ImGui.SliderFloat("Particle Mass", ref _particleMass, 0.01f, 10.0f) && _lsctpm)
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
        if (ImGui.SliderFloat("Scale", ref _lsctpmScale, 0.0f, 10.0f))
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.PopID();


        ImGui.PushID("Bias (particle mass)");
        if (ImGui.SliderFloat("Bias", ref _lsctpmBias, 0.0f, 10.0f))
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
        if (ImGui.SliderFloat("Scale", ref _lsctslScale, 0.0f, 10.0f))
        {
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.PopID();


        ImGui.PushID("Bias (spring length)");
        if (ImGui.SliderFloat("Bias", ref _lsctslBias, 0.0f, 10.0f))
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
        if (ImGui.SliderInt("Boxes", ref _boxesCount, 0, 20))
        {
            SetBoxesCount?.Invoke(_boxesCount);
        }

        if (ImGui.SliderInt("Spheres", ref _spheresCount, 0, 20))
        {
            SetSpheresCount?.Invoke(_boxesCount);
        }
    }

    public record State(
        int SizeX,
        int SizeY,
        Real SpringLength,
        Real SpringConstant,
        Real ParticleMass,
        bool Lsctpm,
        Real LsctpmScale,
        Real LsctpmBias,
        bool Lsctsl,
        Real LsctslScale,
        Real LsctslBias,
        int BoxesCount,
        int SpheresCount
    );

    public State SaveState()
    {
        return new State(_sizeX, _sizeY, _springLength, _springConstant, ParticleMass, _lsctpm, _lsctpmScale,
            _lsctpmBias, _lsctsl, _lsctslScale, _lsctslBias, _boxesCount, _spheresCount);
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

        _boxesCount = state.BoxesCount;
        _spheresCount = state.SpheresCount;
        SetBoxesCount?.Invoke(_boxesCount);
        SetSpheresCount?.Invoke(_spheresCount);
    }
}