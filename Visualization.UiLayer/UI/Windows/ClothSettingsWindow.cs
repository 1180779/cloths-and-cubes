using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class ClothSettingsWindow
{
    private int _sizeX = 21;
    public int SizeX => _sizeX;
    private int _sizeY = 21;
    public int SizeY => _sizeY;

    private Real _springLength = 0.25f;
    public Real SpringLength => _springLength;

    private Real _springConstant = 1.0f;
    public Real SpringConstant => _springConstant;

    private Real _particleMass = 0.1f;
    public Real ParticleMass => _particleMass;
    
    public void Draw()
    {
        ImGui.Begin("Cloth Settings");

        ImGui.SliderInt("Size X", ref _sizeX, 1, 100);
        ImGui.SliderInt("Size Y", ref _sizeY, 1, 100);

        ImGui.SliderFloat("Spring Length", ref _springLength, 0.01f, 1.0f);
        ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.01f, 10.0f);
        ImGui.SliderFloat("Particle Mass", ref _particleMass, 0.01f, 10.0f);
        
        ImGui.End();
    }

    public record State(int SizeX, int SizeY, Real SpringLength, Real SpringConstant, Real ParticleMass);

    public State SaveState()
    {
        return new State(_sizeX, _sizeY, _springLength, _springConstant, ParticleMass);
    }

    public void RestoreState(State state)
    {
        _sizeX = state.SizeX;
        _sizeY = state.SizeY;

        _springLength = state.SpringLength;
        _springConstant = state.SpringConstant;
        _particleMass = state.ParticleMass;
    }
}