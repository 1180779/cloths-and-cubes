using Engine.Force;
using Engine.RigidBodies;

namespace Engine
{
    public class Cloth
    {
        public RigidParticle[,] Particles;
        public ForceRegistry Registry;
        public int SizeX;
        public int SizeY;
        public Vector3 Particle0Pos;
        public float SpringLength;
        public float SpringConstant;
        public float ParticleMass;

        private record ParticleSpringAssociation(RigidBody P, Spring S);
        private readonly List<ParticleSpringAssociation> _particleSpringAssociations = [];

        public Cloth(
            ForceRegistry registry,
            int sizeX = 21,
            int sizeY = 21,
            float springLength = 0.25f,
            float springConstant = 1.0f,
            float particleMass = 0.1f,
            Vector3? particle0Pos = null)
        {
            if (particle0Pos == null)
            {
                particle0Pos = new Vector3(-2f, 10f, -2f);
            }

            Registry = registry;
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.SpringLength = springLength;
            this.SpringConstant = springConstant;
            this.ParticleMass = particleMass;
            this.Particle0Pos = particle0Pos.Value;
            Particles = new RigidParticle[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    Particles[i, j] = new RigidParticle();
                    Particles[i, j].SetState(particle0Pos.Value + new Vector3(springLength * i, 0, springLength * j), 0,
                        new Vector3(), ParticleMass);
                }
            }

            // Rotate(new Vector3(1, 1, 0));
            float diagonalLength = springLength * (float)Math.Sqrt(2.0);
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    if (i != sizeX - 1)
                    {
                        // Horizontal spring - add for both directions
                        var spring = new Spring(new Vector3(), Particles[i + 1, j].Body,
                            new Vector3(), springConstant, springLength);
                        _particleSpringAssociations.Add(new (Particles[i, j].Body, spring));
                        Registry.Add(Particles[i, j].Body, spring);
                    }

                    if (j != sizeY - 1)
                    {
                        // Vertical spring - add for both directions
                        var spring = new Spring(new Vector3(), Particles[i, j + 1].Body,
                            new Vector3(), springConstant, springLength);
                        _particleSpringAssociations.Add(new (Particles[i, j].Body, spring));
                        Registry.Add(Particles[i, j].Body, spring);
                    }

                    if (i != sizeX - 1 && j != sizeY - 1)
                    {
                        // Diagonal spring has longer rest length: sqrt(2) * springLength
                        var spring = new Spring(new Vector3(), Particles[i + 1, j + 1].Body,
                            new Vector3(), springConstant, diagonalLength);
                        _particleSpringAssociations.Add(new (Particles[i, j].Body, spring));
                        Registry.Add(Particles[i, j].Body, spring);
                    }

                    if (i != 0 && j != sizeY - 1)
                    {
                        var spring = new Spring(Particles[i - 1, j + 1].Body.Position, Particles[i, j].Body,
                            Particles[i, j].Body.Position, springConstant, diagonalLength);
                        _particleSpringAssociations.Add(new (Particles[i, j].Body, spring));
                        Registry.Add(Particles[i, j].Body, spring);
                    }
                }
            }
        }

        public void RemoveSpringsFromForceRegistry()
        {
            foreach (var particleSpringAssociation in _particleSpringAssociations)   
            {
                Registry.Remove(particleSpringAssociation.P, particleSpringAssociation.S);
            }
        }
        
        public Vector3[,] Points()
        {
            Vector3[,] points = new Vector3[SizeX, SizeY];
            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    points[i, j] = Particles[i, j].Body.Position;
                }
            }

            return points;
        }

        public void Pin(uint x, uint y, Vector3 pos)
        {
            if (x >= SizeX | y >= SizeY)
            {
                return;
            }
            // TODO: Implement
        }

        public void Update(float duration)
        {
            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    var box = Particles[i, j];

                    // Run the physics
                    box.Body.Integrate(duration);
                    box.CalculateInternals();
                }
            }
        }

        public void Move(Vector3 move)
        {
            //TODO
        }

        public void Rotate(Vector3 rot)
        {
            var pivot = Particle0Pos;

            // Precompute sines and cosines (rot components are in radians).
            double cx = Math.Cos(rot.X);
            double sx = Math.Sin(rot.X);
            double cy = Math.Cos(rot.Y);
            double sy = Math.Sin(rot.Y);
            double cz = Math.Cos(rot.Z);
            double sz = Math.Sin(rot.Z);


            Real r00 = (Real)(cz * cy);
            Real r01 = (Real)(cz * sy * sx - sz * cx);
            Real r02 = (Real)(cz * sy * cx + sz * sx);

            Real r10 = (Real)(sz * cy);
            Real r11 = (Real)(sz * sy * sx + cz * cx);
            Real r12 = (Real)(sz * sy * cx - cz * sx);

            Real r20 = (Real)(-sy);
            Real r21 = (Real)(cy * sx);
            Real r22 = (Real)(cy * cx);
            Matrix3 rotMat = new Matrix3(r00, r01, r02, r10, r11, r12, r20, r21, r22);

            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    var body = Particles[i, j].Body;
                    // relative vector to pivot
                    Vector3 rel = body.Position - pivot;

                    Vector3 rotated = rotMat.Transform(rel);

                    body.Position = rotated + pivot;

                    // ensure derived data and wake body
                    body.CalculateDerivedData();
                    body.SetAwake();
                }
            }
        }
    }
}