using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class ClothParticleWrapperGizmoAdapter : ITranslationGizmoTarget
{
    private readonly ClothParticleWrapper _wrapper;

    public ClothParticleWrapperGizmoAdapter(ClothParticleWrapper wrapper)
    {
        _wrapper = wrapper;
    }

    public ClothParticleWrapper Wrapper => _wrapper;

    public Vector3 AxisPosition => _wrapper.Particle.Body.Position.ToOpenTK();
    public Quaternion AxisOrientation => Quaternion.Identity;

    public Vector3 Position
    {
        get => _wrapper.Particle.Body.Position.ToOpenTK();
        set
        {
            _wrapper.Particle.Body.Position = value.ToEngine();
            _wrapper.Particle.Body.ClearAccumulators();
        }
    }
}