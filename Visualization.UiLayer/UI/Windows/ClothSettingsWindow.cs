using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class ClothSettingsWindow
{
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
        ImGui.Begin("Cloth Settings");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        if (ImGui.SliderFloat("Spring Length", ref _springLength, 0.01f, 1.0f))
        {
            if (_lsctsl)
            {
                _springConstant = _lsctpmScale * _particleMass + _lsctslBias;
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

        // _lsctpm
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

        if (ImGui.SliderFloat("Scale (particle mass)", ref _lsctpmScale, 0.0f, 10.0f))
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        if (ImGui.SliderFloat("Bias (particle mass)", ref _lsctpmBias, 0.0f, 10.0f))
        {
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        if (ImGui.Button("Reset Scale (particle mass)"))
        {
            _lsctpmScale = (Real)1.0;
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Bias (particle mass)"))
        {
            _lsctpmBias = (Real)0.0;
            _springConstant = _lsctpmScale * _particleMass + _lsctpmBias;
        }

        if (!_lsctpm || _lsctsl)
        {
            ImGui.EndDisabled();
        }

        // _lsctsl
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

        if (ImGui.SliderFloat("Scale (spring length)", ref _lsctslScale, 0.0f, 10.0f))
        {
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        if (ImGui.SliderFloat("Bias (spring length)", ref _lsctslBias, 0.0f, 10.0f))
        {
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        if (ImGui.Button("Reset Scale (spring length)"))
        {
            _lsctslScale = (Real)1.0;
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Bias (spring length)"))
        {
            _lsctslBias = (Real)0.0;
            _springConstant = _lsctslScale * _springLength + _lsctslBias;
        }

        if (!_lsctsl)
        {
            ImGui.EndDisabled();
        }

        ImGui.End();
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
        Real LsctslBias
    );

    public State SaveState()
    {
        return new State(_sizeX, _sizeY, _springLength, _springConstant, ParticleMass, _lsctpm, _lsctpmScale,
            _lsctpmBias, _lsctsl, _lsctslScale, _lsctslBias);
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
    }
}