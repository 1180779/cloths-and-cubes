namespace Engine.ParticleEngine
{
    public interface IParticleForceGenerator
    {
        public void UpdateForce(Particle particle, Real duration);
    }

    public class ParticleGravity : IParticleForceGenerator
    {
        private Vector3 _gravity;

        public ParticleGravity(Vector3 gravity)
        {
            _gravity = gravity;
        }

        public void UpdateForce(Particle particle, Real duration)
        {
            if (!particle.HasFiniteMass()) return;

            particle.AddForce(_gravity * particle.GetMass());
        }
    }

    class ParticleDrag : IParticleForceGenerator
    {
        private Real k1;
        private Real k2;

        public ParticleDrag(float k1, float k2)
        {
            this.k1 = k1;
            this.k2 = k2;
        }

        public void UpdateForce(Particle particle, Real duration)
        {
            var force = particle.velocity;

            var mag = force.Magnitude;
            mag = k1 * mag + k2 * mag * mag;
            force.Normalize();
            particle.AddForce(force * -mag);
        }
    }

    class ParticleSpring : IParticleForceGenerator
    {
        private Particle _other;
        private Real springConstant;
        private Real restLength;

        public ParticleSpring(Particle other, Real springConstant, Real restLength)
        {
            _other = other;
            this.springConstant = springConstant;
            this.restLength = restLength;
        }

        public void UpdateForce(Particle particle, Real duration)
        {
            Vector3 force = particle.position;
            force -= _other.position;
            var mag = Real.Abs(force.Magnitude - restLength);
            mag *= springConstant;

            force.Normalize();
            particle.AddForce(force * -mag);
        }
    }

    public class ParticleAnchoredSpring : IParticleForceGenerator
    {
        private Vector3 _anchor;
        private Real springConstant;
        private Real restLength;

        public ParticleAnchoredSpring(Vector3 anchor, Real springConstant, Real restLength)
        {
            _anchor = anchor;
            this.springConstant = springConstant;
            this.restLength = restLength;
        }

        public void UpdateForce(Particle particle, Real duration)
        {
            Vector3 force = particle.position;
            force -= _anchor;
            var mag = Real.Abs(force.Magnitude - restLength);
            mag *= springConstant;

            force.Normalize();
            particle.AddForce(force * -mag);
        }
    }

    class ParticleBungee : IParticleForceGenerator
    {
        private Particle _other;
        private Real springConstant;
        private Real restLength;

        public ParticleBungee(Particle other, Real springConstant, Real restLength)
        {
            _other = other;
            this.springConstant = springConstant;
            this.restLength = restLength;
        }

        public void UpdateForce(Particle particle, Real duration)
        {
            Vector3 force = particle.position;
            force -= _other.position;
            var mag = force.Magnitude;
            if (mag <= restLength) return;

            mag *= springConstant * (restLength - mag);

            force.Normalize();
            particle.AddForce(force * -mag);
        }
    }

    class ParticleBuoyancy : IParticleForceGenerator
    {
        private Real _maxDepth;
        private Real _volume;
        private Real _waterHeight;
        private Real _liquidDensity;

        public ParticleBuoyancy(Real maxDepth, Real volume, Real waterHeight, Real liquidDensity = 1000)
        {
            _maxDepth = maxDepth;
            _volume = volume;
            _waterHeight = waterHeight;
            _liquidDensity = liquidDensity;
        }


        public void UpdateForce(Particle particle, Real duration)
        {
            Real depth = particle.position.Y;
            if (depth >= _waterHeight + _maxDepth) return;

            Vector3 force = new Vector3();

            if (depth <= _waterHeight - _maxDepth)
            {
                force.Y = _liquidDensity * _volume;
                particle.AddForce(force);
                return;
            }

            force.Y = _liquidDensity * _volume * (depth - _maxDepth - _waterHeight) / (Real)(2 * _maxDepth);

            particle.AddForce(force);
        }
    }

    public class ParticleForceRegistry
    {
        protected List<(Particle, IParticleForceGenerator)> registry = new();

        public void Add(Particle particle, IParticleForceGenerator pfg)
        {
            registry.Add((particle, pfg));
        }

        public void Remove(Particle particle, IParticleForceGenerator pfg)
        {
            registry.Remove((particle, pfg));
        }

        public void Clear()
        {
            registry.Clear();
        }

        public void UpdateForces(Real duration)
        {
            foreach (var (particle, pfg) in registry)
            {
                pfg.UpdateForce(particle, duration);
            }
        }
    }
}