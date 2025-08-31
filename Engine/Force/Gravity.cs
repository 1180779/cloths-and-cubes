using Engine.RigidBodies;

namespace Engine.Force;

public class Gravity : IForceGenerator
{
    /** Holds the acceleration due to gravity. */
    Vector3 gravity;

    Gravity(Vector3 g)
    {
        gravity = g;
    }

    public void UpdateForce(RigidBody body, Real duration)
    {
        // Check that we do not have infinite mass
        if (body.InverseMass > 0) return;

        // Apply the mass-scaled force to the body
        body.AddForce(gravity * body.Mass);
    }
}