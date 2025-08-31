using System.Diagnostics;

namespace Engine;

public class Particle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public Vector3 forceAccum;
    public Real damping;
    public Real inverseMass { get; private set; }

    public Particle(
        Vector3 position,
        Vector3 velocity,
        Vector3 acceleration,
        Vector3 forceAccum,
        Real damping,
        Real inverseMass)
    {
        this.position = position;
        this.velocity = velocity;
        this.acceleration = acceleration;
        this.damping = damping;
        this.inverseMass = inverseMass;
        this.forceAccum = forceAccum;
    }

    public Particle()
    {
        position = new Vector3();
        velocity = new Vector3();
        acceleration = new Vector3();
        forceAccum = new Vector3();
        damping = 0.99f;
        inverseMass = 1f;
    }


    public void Integrate(Real duration)
    {
        if (inverseMass < 0.0f) return;
        Debug.Assert(duration > 0.0, $"Duration has to be greater than 0.0. Duration: {duration:F3}.");
        
        position += velocity * duration;

        var acc = acceleration;
        acc += forceAccum * inverseMass;
        velocity += acc * duration;

        velocity *= Real.Pow(damping, duration);
        ClearAccumulator();
    }

    public void SetMass(Real mass)
    {
        if (mass == 0) return;
        inverseMass = 1f / mass;
    }

    public Real GetMass()
    {
        if (inverseMass == 0.0)
        {
            return Real.MaxValue;
        }

        return (Real)1.0 / inverseMass;
    }

    public bool HasFiniteMass()
    {
        return inverseMass != 0.0;
    }

    public void SetInverseMass(Real inverseMass)
    {
        this.inverseMass = inverseMass;
    }

    public void ClearAccumulator()
    {
        forceAccum.X = forceAccum.Y = forceAccum.Z = (Real)0;
    }

    public void AddForce(Vector3 force)
    {
        forceAccum += force;
        if (Core.Debug) Console.WriteLine($"[DEBUG]: Adding force {force} to particle at {this.position}");
    }
}