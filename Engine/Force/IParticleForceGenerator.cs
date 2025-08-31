namespace Engine.Force;

public interface IParticleForceGenerator
{
    void UpdateForce(Particle particle, Real duration);
}