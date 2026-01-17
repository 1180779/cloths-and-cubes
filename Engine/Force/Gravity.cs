using Engine.RigidBodies;

namespace Engine.Force;

/// <summary>
/// Applies a gravitational force to a rigid body.
/// </summary>
/// <note>
/// <para>This is used rather sparingly if at all since the acceleration from gravity 
/// is always the same (a = g).
/// It is more efficient to just set it directly on the bodies as acceleration. </para>
///
/// <para>This class is provided for completeness. </para>
/// </note>
public class Gravity : IForceGenerator
{
    /** Holds the acceleration due to gravity. */
    Vector3 gravity;

    public Gravity(Vector3 g)
    {
        gravity = g;
    }

    public void UpdateForce(RigidBody body, Real duration)
    {
        // Check that we do not have infinite mass
        if (body.InverseMass == 0) return;

        // Apply the mass-scaled force to the body
        body.AddForce(gravity * body.Mass);
    }
}