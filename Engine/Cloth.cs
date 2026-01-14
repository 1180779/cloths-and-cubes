using System.Runtime.CompilerServices;

using Engine.Force;
using Engine.RigidBodies;

namespace Engine;

public class Cloth
{
    public ClothRigidParticle[,] Particles;
    public ForceRegistry Registry;
    public int SizeX;
    public int SizeY;
    public Vector3 Particle0Pos;
    public float SpringLength;
    public float SpringConstant;
    public float ParticleMass;

    /// <summary>
    /// Gets or sets the center position of the cloth.
    /// The center is calculated as the average position of all particles.
    /// </summary>
    public Vector3 Center
    {
        get
        {
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    sum += Particles[i, j].Body.Position;
                }
            }

            return sum * (1.0f / (SizeX * SizeY));
        }
        set
        {
            var currentCenter = Center;
            var offset = value - currentCenter;
            Move(offset);
        }
    }

    private record ParticleSpringAssociation(RigidBody P, Spring S);

    private readonly List<ParticleSpringAssociation> _particleSpringAssociations = [];

    public Cloth(
        ForceRegistry registry,
        int sizeX = 6,
        int sizeY = 6,
        float springLength = 0.25f,
        float springConstant = 1.0f,
        float particleMass = 0.1f,
        Vector3? particle0Pos = null)
    {
        particle0Pos ??= new Vector3(-2f, 4f, -2f);

        Registry = registry;
        this.SizeX = sizeX;
        this.SizeY = sizeY;
        this.SpringLength = springLength;
        this.SpringConstant = springConstant;
        this.ParticleMass = particleMass;
        this.Particle0Pos = particle0Pos.Value;
        Particles = new ClothRigidParticle[sizeX, sizeY];
        ResetToInitialPosition();

        CreateSprings();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsCorner(int i, int j) =>
        (i == 0 && j == 0) ||
        (i == 0 && j == SizeY - 1) ||
        (i == SizeX - 1 && j == 0) ||
        (i == SizeX - 1 && j == SizeY - 1);

    private void ResetToInitialPosition()
    {
        for (int i = 0; i < SizeX; i++)
        {
            for (int j = 0; j < SizeY; j++)
            {
                // Create a new particle each time as the indices might not have
                // valid indices in the old particles, which are init only.
                if (IsCorner(i, j))
                {
                    Particles[i, j] = new ClothRigidParticleInCorner { Cloth = this, XIndex = i, YIndex = j };
                }
                else
                {
                    Particles[i, j] = new ClothRigidParticle { Cloth = this, XIndex = i, YIndex = j };
                }

                Particles[i, j].SetState(
                    Particle0Pos + new Vector3(SpringLength * i, 0, SpringLength * j),
                    0,
                    new Vector3(),
                    ParticleMass);
            }
        }
    }

    /// <summary>
    /// Regenerates the grid for the cloth object after updating its dimensions, spring parameters,
    /// and particle mass.
    /// </summary>
    /// <param name="newSizeX">The new number of columns for the grid.</param>
    /// <param name="newSizeY">The new number of rows for the grid.</param>
    /// <param name="newSpringLength">The new length of the springs between particles.</param>
    /// <param name="newSpringConstant">The new spring constant defining the stiffness of the springs.</param>
    /// <param name="newParticleMass">The new mass assigned to each particle in the cloth.</param>
    public void RegenerateGrid(
        int newSizeX,
        int newSizeY,
        float newSpringLength,
        float newSpringConstant,
        float newParticleMass)
    {
        RemoveSpringsFromForceRegistry();
        _particleSpringAssociations.Clear();

        SizeX = newSizeX;
        SizeY = newSizeY;
        SpringLength = newSpringLength;
        SpringConstant = newSpringConstant;
        ParticleMass = newParticleMass;

        Particles = Utilities.ResizeArray(Particles, SizeX, SizeY);
        ResetToInitialPosition();
        CreateSprings();
    }

    /// <summary>
    /// Regenerates the grid for the cloth object while preserving the original center point of the grid.
    /// </summary>
    /// <param name="newSizeX">The new number of columns for the grid.</param>
    /// <param name="newSizeY">The new number of rows for the grid.</param>
    /// <param name="newSpringLength">The new length of the springs between particles.</param>
    /// <param name="newSpringConstant">The new spring constant defining the stiffness of the springs.</param>
    /// <param name="newParticleMass">The new mass assigned to each particle in the cloth.</param>
    public void RegenerateGridPreservingTheCenter(
        int newSizeX,
        int newSizeY,
        float newSpringLength,
        float newSpringConstant,
        float newParticleMass)
    {
        var oldCenter = Center;
        RegenerateGrid(newSizeX, newSizeY, newSpringLength, newSpringConstant, newParticleMass);
        var newCenter = Center;

        var offset = oldCenter - newCenter;
        Move(offset);
    }

    /// <summary>
    /// Creates springs between particles in the grid.
    /// This includes horizontal, vertical, and diagonal springs.
    /// </summary>
    private void CreateSprings()
    {
        float diagonalLength = SpringLength * (float)Math.Sqrt(2.0);
        // Structural springs (horizontal and vertical)
        // Shear springs (diagonal)
        // Bend springs (every second particle)

        for (int i = 0; i < SizeX; i++)
        {
            for (int j = 0; j < SizeY; j++)
            {
                // Structural Springs
                if (i != SizeX - 1)
                {
                    // Horizontal spring - add for both directions
                    var spring = new Spring(new Vector3(), Particles[i + 1, j].Body,
                        new Vector3(), SpringConstant, SpringLength);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
                    Registry.Add(Particles[i, j].Body, spring);
                }

                if (j != SizeY - 1)
                {
                    // Vertical spring - add for both directions
                    var spring = new Spring(new Vector3(), Particles[i, j + 1].Body,
                        new Vector3(), SpringConstant, SpringLength);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
                    Registry.Add(Particles[i, j].Body, spring);
                }

                // Shear Springs
                if (i != SizeX - 1 && j != SizeY - 1)
                {
                    // Diagonal spring has longer rest length: sqrt(2) * springLength
                    var spring = new Spring(new Vector3(), Particles[i + 1, j + 1].Body,
                        new Vector3(), SpringConstant, diagonalLength);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
                    Registry.Add(Particles[i, j].Body, spring);
                }

                if (i != 0 && j != SizeY - 1)
                {
                    // Diagonal spring (down-left)
                    var spring = new Spring(new Vector3(), Particles[i - 1, j + 1].Body,
                        new Vector3(), SpringConstant, diagonalLength);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
                    Registry.Add(Particles[i, j].Body, spring);
                }

                // Bend Springs (skip one particle)
                // These help resist bending and add rigidity
                if (i < SizeX - 2)
                {
                    var spring = new Spring(new Vector3(), Particles[i + 2, j].Body,
                        new Vector3(), SpringConstant, SpringLength * 2);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
                    Registry.Add(Particles[i, j].Body, spring);
                }

                if (j < SizeY - 2)
                {
                    var spring = new Spring(new Vector3(), Particles[i, j + 2].Body,
                        new Vector3(), SpringConstant, SpringLength * 2);
                    _particleSpringAssociations.Add(new(Particles[i, j].Body, spring));
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

    public Vector3[,] PointsVelocityAdjusted(float positionEpsilon)
    {
        Vector3[,] points = new Vector3[SizeX, SizeY];
        for (int i = 0; i < SizeX; i++)
        {
            for (int j = 0; j < SizeY; j++)
            {
                points[i, j] = Particles[i, j].Body.Position -
                    Particles[i, j].Body.Velocity.Normalized() * positionEpsilon;
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
        // Substepping
        int subSteps = 8; // Increased substeps for stability
        float subDt = duration / subSteps;

        for (int step = 0; step < subSteps; step++)
        {
            // Update forces for this substep
            Registry.updateForces(subDt);

            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    var box = Particles[i, j];

                    // Run the physics
                    box.Body.Integrate(subDt);
                    box.CalculateInternals();
                }
            }
        }
    }

    public void Move(Vector3 move)
    {
        for (int i = 0; i < SizeX; i++)
        {
            for (int j = 0; j < SizeY; j++)
            {
                var body = Particles[i, j].Body;
                body.Position += move;
                body.CalculateDerivedData();
                body.SetAwake();
            }
        }

        Particle0Pos += move;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RotateAroundCenter(Vector3 rot)
    {
        Rotate(rot, Center);
    }

    public void Rotate(Vector3 rot, Vector3? pivot = null)
    {
        pivot ??= Particle0Pos;

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
                Vector3 rel = body.Position - pivot.Value;

                Vector3 rotated = rotMat.Transform(rel);

                body.Position = rotated + pivot.Value;

                // ensure derived data and wake body
                body.CalculateDerivedData();
                body.SetAwake();
            }
        }

        // Update Particle0Pos
        Particle0Pos = Particles[0, 0].Body.Position;
    }

    public void ClearAccumulators()
    {
        for (int i = 0; i < SizeX; i++)
        {
            for (int j = 0; j < SizeY; j++)
            {
                Particles[i, j].Body.ClearAccumulators();
                Particles[i, j].Body.Velocity = Vector3.Zero;
                Particles[i, j].Body.Rotation = Vector3.Zero;
            }
        }
    }
}