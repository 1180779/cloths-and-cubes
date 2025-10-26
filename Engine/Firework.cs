using Engine.ParticleEngine;

namespace Engine;

public class Firework : Particle
{
    public uint type;

    public Real age;

    public bool Update(Real duration)
    {
        Integrate(duration);

        age -= duration;
        return (age < (Real)0);
    }
}

public class FireworkRule
{
    public uint type;
    public Real minAge;
    public Real maxAge;
    public Vector3 minVelocity;
    public Vector3 maxVelocity;
    public Real damping;

    public class Payload
    {
        public uint type;
        public uint count;
    }

    public uint payloadCount;
    List<Payload> payloads;


    public void Create(Firework firework, Firework? parent)
    {
        System.Random r = new();
        firework.type = type;
        firework.age = (Real)r.NextDouble() * (maxAge - minAge) + minAge;
        if (parent != null)
        {
            firework.position = parent.position;
        }

        // The velocity is the particle’s velocity.
        Vector3 vel = (parent != null) ? parent.velocity : new Vector3();
        vel += Vector3.RandomVector(r, minVelocity, maxVelocity);
        firework.velocity = vel;
        // We use a mass of 1 in all cases (no point having fireworks
        // with different masses, since they are only under the influence
        // of gravity).
        firework.SetInverseMass(1f);
        firework.damping = damping;
        firework.acceleration = Vector3.Gravity;
        firework.ClearAccumulator();
    }
}