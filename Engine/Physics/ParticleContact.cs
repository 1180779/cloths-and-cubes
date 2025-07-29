using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Physics
{
    class ParticleContact
    {
        public Particle?[] particles;
        public Real restitution;
        public Vector3 contactNormal;
        public Real interpenetration;

        public ParticleContact(Particle[] particles, float restitution, Vector3 contactNormal)
        {
            this.particles = particles;
            this.restitution = restitution;
            this.contactNormal = contactNormal;
        }

        public ParticleContact(Particle a, Particle? b, Real restitution, Vector3 contactNormal)
        {
            particles = [a, b];
            this.restitution = restitution;
            this.contactNormal = contactNormal;
        }

        protected void Resolve(Real duration) { ResolveVelocity(duration); }
        protected Real CalculateSeparatingVelocity()
        {
            var relVelocity = particles[0].velocity;
            if (particles[1] != null) relVelocity -= particles[1].velocity;

            return relVelocity * contactNormal;
        }
        private void ResolveVelocity(Real duration)
        {
            Real sepVelocity = CalculateSeparatingVelocity();
            if (sepVelocity > 0.0) return;
            Real newSepVelocity = -sepVelocity * restitution;
            Real deltaVelocity = newSepVelocity - sepVelocity;

            Real totalInverseMass = particles[0].inverseMass;
            if (particles[1] != null) totalInverseMass += particles[1].inverseMass;

            if (totalInverseMass <= (Real)0) return;

            Real impulse = deltaVelocity / totalInverseMass;
            Vector3 impulsePerIMass = contactNormal * impulse;

            particles[0].velocity += impulsePerIMass * particles[0].inverseMass;

            if(particles[1] != null)
            {
                particles[1].velocity -= impulsePerIMass * particles[1].inverseMass;

            }
        }
    }
}
