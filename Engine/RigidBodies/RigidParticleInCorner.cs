using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

/// <summary>
/// Represents a rigid particle that is positioned within a corner and is
/// associated with a cloth simulation.
/// </summary>
/// <remarks>
/// This class is a specialized version of the <see cref="RigidParticle"/> class
/// that includes a reference to a <see cref="Cloth"/> object. The particle's
/// bounding box is calculated based on the spring length of the attached cloth
/// instead of a fixed small size to enable easier selection.
/// </remarks>
public sealed class RigidParticleInCorner : RigidParticle
{
    public required Cloth AttachedToCloth { get; init; }
    public new float BoundingBoxHalfSize => AttachedToCloth.SpringLength / 2.0f + RigidParticle.BoundingBoxHalfSize;

    public override BoundingBox GetBoundingBox()
    {
        var halfSize = BoundingBoxHalfSize;
        return new BoundingBox(
            center: this.Body.Position,
            halfSize: new Vector3(halfSize, halfSize, halfSize) // larger AABB for easier selection
        );
    }
}