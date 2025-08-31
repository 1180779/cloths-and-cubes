namespace Engine;
/// <summary>
/// Keeps track of one random stream: i.e., a seed and its output.
/// This is used to get random numbers. Rather than a function, this 
/// allows there to be several streams of repeatable random numbers
/// at the same time. Uses the RandRotB algorithm.
/// </summary>
public class Random
{
    // Internal mechanics
    private int p1, p2;
    private uint[] buffer = new uint[17];

    /// <summary>
    /// left bitwise rotation
    /// </summary>
    public uint Rotl(uint n, uint r)
    {
        return (n << (int)r) | (n >> (32 - (int)r));
    }

    /// <summary>
    /// right bitwise rotation
    /// </summary>
    public uint Rotr(uint n, uint r)
    {
        return (n >> (int)r) | (n << (32 - (int)r));
    }

    /// <summary>
    /// Creates a new random number stream with a seed based on
    /// timing data.
    /// </summary>
    public Random()
    {
        Seed(0);
    }

    /// <summary>
    /// Creates a new random stream with the given seed.
    /// </summary>
    public Random(uint seed)
    {
        Seed(seed);
    }

    /// <summary>
    /// Sets the seed value for the random stream.
    /// </summary>
    public void Seed(uint seed)
    {
        if (seed == 0)
        {
            seed = (uint)DateTime.Now.Ticks;
        }

        // Fill the buffer with some basic random numbers
        for (uint i = 0; i < 17; i++)
        {
            // Simple linear congruential generator
            seed = seed * 2891336453 + 1;
            buffer[i] = seed;
        }

        // Initialize pointers into the buffer
        p1 = 0;
        p2 = 10;
    }

    /// <summary>
    /// Returns the next random bitstring from the stream. This is
    /// the fastest method.
    /// </summary>
    public uint RandomBits()
    {
        var result =
            // Rotate the buffer and store it back to itself
            buffer[p1] = Rotl(buffer[p2], 13) + Rotl(buffer[p1], 9);

        // Rotate pointers
        if (--p1 < 0) p1 = 16;
        if (--p2 < 0) p2 = 16;

        return result;
    }

    /// <summary>
    /// Returns a random floating point number between 0 and 1.
    /// </summary>
    public Real RandomReal()
    {
        return (Real)System.Random.Shared.NextDouble();
    }

    /// <summary>
    /// Returns a random floating point number between 0 and scale.
    /// </summary>
    public Real RandomReal(Real scale)
    {
        return RandomReal() * scale;
    }

    /// <summary>
    /// Returns a random floating point number between min and max.
    /// </summary>
    public Real RandomReal(Real min, Real max)
    {
        return RandomReal() * (max - min) + min;
    }

    /// <summary>
    /// Returns a random integer less than the given value.
    /// </summary>
    public uint RandomInt(uint max)
    {
        return RandomBits() % max;
    }

    public uint RandomInt(uint min, uint max)
    {
        return min + RandomBits() % (max - min);
    }

    /// <summary>
    /// Returns a random binomially distributed number between - scale
    /// and +scale.
    /// </summary>
    public Real RandomBinomial(Real scale)
    {
        return (RandomReal() - RandomReal()) * scale;
    }

    /// <summary>
    /// Returns a random vector where each component is binomially
    /// distributed in the range (-scale to scale) [mean = 0.0f].
    /// </summary>
    public Vector3 RandomVector(float scale)
    {
        return new Vector3(
            RandomBinomial(scale),
            RandomBinomial(scale),
            RandomBinomial(scale)
        );
    }

    /// <summary>
    /// Returns a random vector where each component is binomially
    /// distributed in the range (-scale to scale) [mean = 0.0f],
    /// where a scale is the corresponding component of the given
    /// vector.
    /// </summary>
    public Vector3 RandomVector(Vector3 scale)
    {
        return new Vector3(
            RandomBinomial(scale.X),
            RandomBinomial(scale.Y),
            RandomBinomial(scale.Z)
        );
    }

    /// <summary>
    /// Returns a random vector in the cube defined by the given
    /// minimum and maximum vectors. The probability is uniformly
    /// distributed in this region.
    /// </summary>
    public Vector3 RandomVector(Vector3 min, Vector3 max)
    {
        return new Vector3(
            RandomReal(min.X, max.X),
            RandomReal(min.Y, max.Y),
            RandomReal(min.Z, max.Z)
        );
    }

    /// <summary>
    /// Returns a random vector where each component is binomially
    /// distributed in the range (-scale to scale) [mean = 0.0f],
    /// except the y coordinate which is zero.
    /// </summary>
    public Vector3 RandomXzVector(Real scale)
    {
        return new Vector3(
            RandomBinomial(scale),
            0,
            RandomBinomial(scale)
        );
    }

    /// <summary>
    /// Returns a random orientation (i.e. normalized) quaternion.
    /// </summary>
    public Quaternion RandomQuaternion()
    {
        Quaternion q = new Quaternion(
            RandomReal(),
            RandomReal(),
            RandomReal(),
            RandomReal()
        );
        q.Normalise();
        return q;
    }
}