namespace Engine.ParticleEngine;

public interface IParticleForceGenerator
{
    void UpdateForce(Particle particle, Real duration);
}