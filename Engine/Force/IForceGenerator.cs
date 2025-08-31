using Engine.RigidBodies;

namespace Engine.Force;

public interface IForceGenerator
{
    public void UpdateForce(RigidBody body, Real duration);
}