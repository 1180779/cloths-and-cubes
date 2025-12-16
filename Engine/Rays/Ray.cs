namespace Engine.Rays;

/// <summary>
/// Represents a ray in 3D space with an origin and a direction.
/// Used for ray-casting operations in collision detection and selection.
/// </summary>
public struct Ray
{
    /// <summary>
    /// The origin point of the ray.
    /// </summary>
    public Vector3 Origin;

    private Vector3 _direction;
    private Vector3 _invDirection;

    /// <summary>
    /// The direction vector of the ray (normalized).
    /// </summary>
    public Vector3 Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            _direction.Normalise();

            // Cache inverse direction for fast slab tests.
            // Division by 0 for float/double yields +/-Infinity, which is fine for slab comparisons.
            _invDirection = new Vector3(
                1.0f / _direction.X,
                1.0f / _direction.Y,
                1.0f / _direction.Z
            );
        }
    }

    /// <summary>
    /// Component-wise inverse of Direction (1 / Direction).
    /// Useful for optimized ray-box intersection tests.
    /// </summary>
    public readonly Vector3 InvDirection => _invDirection;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        _direction = default;
        _invDirection = default;
        Direction = direction;
    }
}