using Engine.Force;
using Engine.RigidBodies;

namespace Engine
{
    public class Cloth
    {
        public RigidParticle[,] particles;
        public ForceRegistry registry;
        public int sizeX;
        public int sizeY;
        public Vector3 particle0pos;
        public float springLength;
        public float springConstant;
        public float ParticleMass;

        public Cloth(
            ForceRegistry _registry,
            int sizeX = 10,
            int sizeY = 3,
            float springLength = 1f,
            float springConstant = 1f,
            float particleMass = 1f,
            Vector3 particle0pos = null)
        {
            if (particle0pos == null)
            {
                particle0pos = new Vector3(0f, 10f, 0f);
            }

            registry = _registry;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.springLength = springLength;
            this.springConstant = springConstant;
            this.ParticleMass = particleMass;
            this.particle0pos = particle0pos;
            particles = new RigidParticle[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    particles[i, j] = new RigidParticle();
                    particles[i, j].SetState(particle0pos + new Vector3(springLength * i, 0, springLength * j), 0,
                        new Vector3());
                }
            }

            Rotate(new Vector3(1, 1, 0));
            float diagonalLength = springLength * (float)Math.Sqrt(2.0);
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    if (i != sizeX - 1)
                    {
                        // Horizontal spring - add for both directions
                        registry.Add(particles[i, j].Body,
                            new Spring(new Vector3(), particles[i + 1, j].Body,
                                new Vector3(), springConstant, springLength));

                    }

                    if (j != sizeY - 1)
                    {
                        // Vertical spring - add for both directions
                        registry.Add(particles[i, j].Body,
                            new Spring(new Vector3(), particles[i, j + 1].Body,
                                new Vector3(), springConstant, springLength));

                    }

                    if (i != sizeX - 1 && j != sizeY - 1)
                    {
                        // Diagonal spring has longer rest length: sqrt(2) * springLength
                        
                        registry.Add(particles[i, j].Body,
                            new Spring(new Vector3(), particles[i + 1, j + 1].Body,
                                new Vector3(), springConstant, diagonalLength));

                    }
                    if (i != 0 && j != sizeY - 1)
                    {
                        
                        registry.Add(particles[i, j].Body,
                            new Spring(particles[i - 1, j + 1].Body.Position, particles[i, j].Body,
                            particles[i, j].Body.Position, springConstant, diagonalLength));
                    }
                }
            }
        }

        public Vector3[,] Points()
        {
            Vector3[,] points = new Vector3[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    points[i, j] = particles[i, j].Body.Position;
                }
            }

            return points;
        }

        public void Pin(uint x, uint y, Vector3 pos)
        {
            if (x >= sizeX | y >= sizeY)
            {
                return;
            }
            //TODO
        }

        public void Update(float duration)
        {
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    var box = particles[i, j];

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
            var pivot = particle0pos ?? new Vector3();

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

            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    var body = particles[i, j].Body;
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