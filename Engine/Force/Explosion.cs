using Engine.ParticleEngine;
using Engine.RigidBodies;

namespace Engine.Force;

public class Explosion : IForceGenerator, IParticleForceGenerator
{
    /**
 * Tracks how long the explosion has been in operation, used
 * for time-sensitive effects.
 */
    private Real timePassed;

    /// <summary>
    /// The location of the detonation of the weapon.
    /// </summary>
    readonly Vector3 detonation = new();

    /// <summary>
    /// The radius up to which objects implode in the first stage
    /// of the explosion.
    /// </summary>
    readonly float implosionMaxRadius = 100.0f;

    /// <summary>
    /// The radius within which objects don't feel the implosion
    /// force. Objects near to the detonation aren't sucked in by
    /// the air implosion.
    /// </summary>
    readonly float implosionMinRadius = 1.0f;

    /// <summary>
    /// The length of time that objects spend imploding before the
    /// concussion phase kicks in.
    /// </summary>
    readonly float implosionDuration = 0.5f;

    /// <summary>
    /// The maximal force that the implosion can apply. This should
    /// be relatively small to avoid the implosion pulling objects
    /// through the detonation point and out the other side before
    /// the concussion wave kicks in.
    /// </summary>
    readonly float implosionForce = 20000.0f;

    /// <summary>
    /// The speed that the shock wave is traveling, this is related
    /// to the thickness below in the relationship:
    ///
    /// thickness >= speed * minimum frame duration
    /// </summary>
    readonly float shockwaveSpeed = 100.0f;

    /// <summary>
    /// The shock wave applies its force over a range of distances,
    /// this controls how thick. Faster waves require larger
    /// thicknesses.
    /// </summary>
    readonly float shockwaveThickness = 5.0f;

    /// <summary>
    /// This is the force that is applied at the very centre of the
    /// concussion wave on an object that is stationary. Objects
    /// that are in front or behind of the wavefront, or that are
    /// already moving outwards, get proportionally less
    /// force. Objects moving in towards the centre get
    /// proportionally more force.
    /// </summary>
    readonly float peakConcussionForce = 50000.0f;

    /// <summary>
    /// The length of time that the concussion wave is active.
    /// As the wave nears this, the forces it applies reduces.
    /// </summary>
    readonly float concussionDuration = 0.1f;

    /// <summary>
    /// The length of time the convection chimney is active. Typically,
    /// this is the longest effect to be in operation, as the heat
    /// from the explosion outlives the shock wave and implosion
    /// itself.
    /// </summary>
    readonly float convectionDuration = 5.0f;

    public void UpdateTimePassed(float duration)
    {
        timePassed += duration;
    }

    public void Reset()
    {
        timePassed = (Real)0.0;
    }

    Vector3 GetForce(Vector3 position, Real duration)
    {
        if (timePassed <= implosionDuration)
        {
            // Implosion, force towards point of explosion
            Vector3 direction = detonation - position;
            var distance = direction.Magnitude;
            if (distance <= implosionMaxRadius && distance > implosionMinRadius)
            {
                // Apply implosion force
                var force = direction.Normalise() * (implosionForce / (distance * distance));
                return force * duration;
            }
        }
        else if (timePassed <= concussionDuration + implosionDuration)
        {
            // Shockwave away from the point of explosion
            var direction = position - detonation;
            var distance = direction.Magnitude;

            // Check if is inside a shockwave
            var shockwaveTravelTime = timePassed - implosionDuration;
            // Average shockwave front where force is strongest
            var shockwaveTravelDistance = shockwaveTravelTime * shockwaveSpeed;
            if (distance > shockwaveTravelDistance + shockwaveThickness || distance < shockwaveTravelDistance - shockwaveThickness)
            {
                // The outside of shockwave
                return new Vector3();
            }

            var distanceToShockWavePeak = Real.Max(Math.Abs(shockwaveTravelDistance - distance), (Real)1.0);
            // Gets proportionally weaker with distance
            var force = peakConcussionForce * duration / (distanceToShockWavePeak * distanceToShockWavePeak);
            return direction.Normalise() * force;
        }
        else if (timePassed <= convectionDuration + concussionDuration + implosionDuration)
        {
            // Chimney
        }

        return new Vector3();
    }
    
    /// <summary>
    /// Calculates and applies the force that the explosion
    /// has on the given rigid body.
    /// </summary>
    public void UpdateForce(RigidBody body, Real duration)
    {
        body.AddForce(GetForce(body.Position, duration));
    }

    /// <summary>
    /// Calculates and applies the force that the explosion has
    /// on the given particle.
    /// </summary>
    public void UpdateForce(Particle particle, Real duration)
    {
        particle.AddForce(GetForce(particle.position, duration));
    }
}