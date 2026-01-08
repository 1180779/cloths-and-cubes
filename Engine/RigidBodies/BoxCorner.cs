using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

/// <summary>
/// Represents a box corner for pinning cloth particles.
/// This is a lightweight helper class that wraps a corner position
/// and provides a bounding box for collision detection.
/// </summary>
public sealed record BoxCorner : IBoxable
{
    /// <summary>
    /// The position of this corner in world space.
    /// </summary>
    public Vector3 Position { get; init; }

    /// <summary>
    /// The box this corner belongs to.
    /// </summary>
    public Box Box { get; init; }

    /// <summary>
    /// The index of this corner (0-7).
    /// </summary>
    public int CornerIndex { get; init; }

    /// <summary>
    /// The half-size of the bounding box around the corner for collision detection.
    /// This determines how close a particle needs to be to snap to the corner.
    /// </summary>
    public const float BoundingBoxHalfSize = 0.55f;

    public BoxCorner(Vector3 position, Box box, int cornerIndex)
    {
        Position = position;
        Box = box;
        CornerIndex = cornerIndex;
    }

    public BoundingBox GetBoundingBox()
    {
        return new BoundingBox(
            center: Position,
            halfSize: new Vector3(BoundingBoxHalfSize, BoundingBoxHalfSize, BoundingBoxHalfSize)
        );
    }
}